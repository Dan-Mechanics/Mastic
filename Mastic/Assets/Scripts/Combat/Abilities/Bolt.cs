using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Bolt : NetworkBehaviour, IReliableAttackAbility
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding secondaryFire = default;
        [SerializeField] private CooldownHandler cooldownHandler = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private LayerMask noregMask = default;
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;

        private List<ShootMessage> pendingShootMessages;
        private ICameraInterpolation interpolation;
        private ShootMessage shootMessage;
        private EntityManager entityManager;
        private MouseLook mouseLook;
        private int cooldownIndex;
        private Rigidbody rb;
        private Transform eyes;
        private Transform cam;
        private Vector3 origin;
        private Vector3 prevOrigin;
        private Vector3 velocity;
        private float tolerance;
        private int previousTick;
        private int maxPendingRequests;

        private void Awake()
        {
            eyes = transform.Find("eyes");
            cam = GameObject.FindWithTag("MainCamera").transform;
            interpolation = cam.GetComponent<ICameraInterpolation>();
            entityManager = FindAnyObjectByType<EntityManager>();
            pendingShootMessages = new List<ShootMessage>();
            mouseLook = GetComponent<MouseLook>();
            rb = GetComponent<Rigidbody>();
            prevOrigin = eyes.position;
            previousTick = -1;
            cooldownIndex = cooldownHandler.GetIndexFromName(nameof(Bolt));

            EasySettings easySettings = EasySettings.Current;
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            tolerance = easySettings.Get<float>(nameof(tolerance));
        }

        public void DoLocalUpdate(int inputTick, int rollbackTick, float unlocalLerpValue)
        {
            if (!secondaryFire.WasPressed || !cooldownHandler.CanCast(cooldownIndex))
                return;

            cooldownHandler.Cast(cooldownIndex);
            shootMessage.debugEnemyPos = Vector3.zero;
            shootMessage.SetValues(cam.position, mouseLook.RotationX, mouseLook.RotationY, interpolation.LerpValue, unlocalLerpValue, inputTick, rollbackTick);
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                Transform target = hit.transform.root;
                if (target.TryGetComponent(out IDamagable damagable))
                {
                    OnPredictDamage?.Invoke(damage);
                    shootMessage.debugEnemyPos = target.position;
                }
            }

            CmdShoot(shootMessage);
        }

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) => OnAuthoritativeDamage?.Invoke(damage);

        [Command]
        private void CmdShoot(ShootMessage shootMessage) 
        {
            if (pendingShootMessages.Count >= maxPendingRequests || shootMessage.inputTick <= previousTick)
                return;

            pendingShootMessages.Add(shootMessage);
            previousTick = shootMessage.inputTick;
        }

        [Server]
        private void Shoot(ShootMessage shootMessage)
        {
            // RECREATE THE SHOT CONDITIONS.
            entityManager.DoRollback(shootMessage.rollbackTick, shootMessage.unlocalLerpValue);
            mouseLook.SetRotationDirectly(shootMessage.xRotation, shootMessage.yRotation);
            interpolation.Interject(origin, prevOrigin, velocity);
            interpolation.SetValue(shootMessage.localLerpValue);
            NetcodeUtils.AllowNoregLenience(cam, shootMessage, tolerance, noregMask);

            if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                return;

            Transform target = hit.transform.root;
            if (!target.TryGetComponent(out IDamagable damagable))
                return;

            damagable.Damage(damage);
            TargetDisplayHitPip(connectionToClient, damage);

            // DEBUG.
            if (shootMessage.debugEnemyPos != Vector3.zero && shootMessage.debugEnemyPos != target.position)
            {
                Debug.LogWarning($"Recreated enemy pos was not the same as on the client. correct: {shootMessage.debugEnemyPos}, recreated: {hit.transform.root.position}");
                Debug.LogWarning($"{(shootMessage.debugEnemyPos - hit.transform.root.position) / Time.fixedDeltaTime}");
            }
        }

        [Server]
        public void DoServerTick(int processedTick)
        {
            origin = eyes.position;
            velocity = rb.linearVelocity;
            for (int i = 0; i < pendingShootMessages.Count; i++)
            {
                if (processedTick < pendingShootMessages[i].inputTick || !cooldownHandler.CanCast(cooldownIndex))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Shoot(pendingShootMessages[i]);
                pendingShootMessages.RemoveAt(i);
                break;
            }

            prevOrigin = origin;
        }
    }
}