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
        private ICameraInterpolation cameraInterpolation;
        private ShootMessage shootMessage;
        private LagCompensation lagCompensation;
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
            cameraInterpolation = cam.GetComponent<ICameraInterpolation>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
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

        public void DoLocalUpdate(int inputTick, int rollbackTick)
        {
            if (!secondaryFire.WasPressed || !cooldownHandler.CanCast(cooldownIndex))
                return;

            cooldownHandler.Cast(cooldownIndex);
            shootMessage.debugEnemyPos = Vector3.zero;
            shootMessage.SetValues(cam.position, mouseLook.RotationX, mouseLook.RotationY, cameraInterpolation.LerpValue, inputTick, rollbackTick);
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                Transform target = hit.transform.root;
                if (target.TryGetComponent(out IDamagable damagable))
                {
                    OnPredictDamage?.Invoke(damage);
                    shootMessage.debugEnemyPos = target.position;
                }
                
                // REDUCE NO-REGS.
                if (target.TryGetComponent(out PlayerEntity playerEntity))
                    shootMessage.rollbackTick = playerEntity.RollbackTick;
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
            lagCompensation.SetAsTick(shootMessage.rollbackTick);
            mouseLook.SetRotationDirectly(shootMessage.xRotation, shootMessage.yRotation);
            cameraInterpolation.Interject(origin, prevOrigin, velocity);
            cameraInterpolation.SetValue(shootMessage.lerpValue);
            IReliableAttackAbility.AllowNoregLenience(cam, shootMessage, tolerance, noregMask);
            //AllowNoregLenience(shootMessage);

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