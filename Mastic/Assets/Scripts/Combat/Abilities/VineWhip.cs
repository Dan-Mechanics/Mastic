using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class VineWhip : NetworkBehaviour, IReliableAttackAbility
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding binding = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private LayerMask noregMask = default;
        [SerializeField] private GameObject prefab = default;
        [SerializeField] private GameObject particleEffect = default;
        [SerializeField] private float range = default;
        [SerializeField] private float speed = default;
        [SerializeField] private float damage = default;

        private CooldownHandler cooldownHandler;
        private List<ShootMessage> pendingShootMessages;
        private ICameraInterpolation interpolation;
        private ShootMessage shootMessage;
        private LagCompensation lagCompensation;
        private MouseLook mouseLook;
        private int cooldownIndex;
        private Rigidbody rb;
        private Transform eyes;
        private float boltEffectLifetime;
        private float boltEffectSize;
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
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            pendingShootMessages = new List<ShootMessage>();
            mouseLook = GetComponent<MouseLook>();
            rb = GetComponent<Rigidbody>();
            prevOrigin = eyes.position;
            previousTick = -1;
            cooldownHandler = GetComponent<CooldownHandler>();
            cooldownIndex = cooldownHandler.GetIndexFromName(GetType().Name);

            EasySettings easySettings = EasySettings.Current;
            boltEffectLifetime = easySettings.Get<float>(nameof(boltEffectLifetime));
            boltEffectSize = easySettings.Get<float>(nameof(boltEffectSize));
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            tolerance = easySettings.Get<float>(nameof(tolerance));
        }

        public void DoLocalUpdate(int inputTick, int rollbackTick)
        {
            if (!binding.WasPressed || !cooldownHandler.CanCast(cooldownIndex))
                return;

            Vector3 endPosition = cam.position + (cam.forward * range);
            cooldownHandler.Cast(cooldownIndex);
            shootMessage.debugEnemyPos = Vector3.zero;
            shootMessage.SetValues(cam.position, mouseLook.RotationX, mouseLook.RotationY, interpolation.LerpValue, inputTick, rollbackTick);
            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                endPosition = hit.point;
                Transform target = hit.transform.root;
                if (target.TryGetComponent(out IDamagable damagable))
                {
                    OnPredictDamage?.Invoke(damage);
                    shootMessage.debugEnemyPos = target.position;
                }
                
                // REDUCE NO-REGS.
                if (target.TryGetComponent(out PlayerEntity playerEntity))
                    shootMessage.rollbackTick = playerEntity.GetDisplayedTick();
            }

            SpawnBoltBeam(cam.position, endPosition);
            CmdShoot(shootMessage);
        }

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) 
            => OnAuthoritativeDamage?.Invoke(damage);

        [Command]
        private void CmdShoot(ShootMessage shootMessage) 
        {
            if (pendingShootMessages.Count >= maxPendingRequests || shootMessage.inputTick <= previousTick)
                return;

            pendingShootMessages.Add(shootMessage);
            previousTick = shootMessage.inputTick;
        }

        private void SpawnBoltBeam(Vector3 start, Vector3 end)
        {
            Transform effect = Instantiate(prefab).transform;
            effect.position = (start + end) * 0.5f;
            effect.Translate(Vector3.down * 0.5f);
            effect.localScale = new Vector3(boltEffectSize, boltEffectSize, Vector3.Distance(start, end));
            effect.forward = (end - start).normalized;
            Destroy(effect.gameObject, boltEffectLifetime);

            Instantiate(particleEffect, transform.position, particleEffect.transform.rotation);
        }

        [Server]
        private void Shoot(ShootMessage shootMessage)
        {
            // RECREATE THE SHOT CONDITIONS.
            lagCompensation.SetAsTick(shootMessage.rollbackTick);
            mouseLook.SetRotationDirectly(shootMessage.xRotation, shootMessage.yRotation);
            interpolation.Interject(origin, prevOrigin, velocity);
            interpolation.SetValue(shootMessage.lerpValue);
            NetcodeUtils.AllowNoregLenience(cam, shootMessage, tolerance, noregMask);

            Vector3 endPosition = cam.position + (cam.forward * range);
            SpawnBoltBeam(cam.position, endPosition);
            if (!Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                return;

            Transform target = hit.transform.root;
            if (target.TryGetComponent(out NetworkMovement networkMovement))
                networkMovement.AddForce((cam.position - target.position).normalized * speed);

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