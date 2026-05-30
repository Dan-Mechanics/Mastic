using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Bolt : NetworkBehaviour, IAttackAbility
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding secondaryFire = default;
        [SerializeField] private CooldownHandler cooldownHandler = default;
        [SerializeField] private int cooldownIndex = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private LayerMask noregMask = default;
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;

        private List<ShootMessage> pendingShootMessages;
        private ICameraInterpolation cameraInterpolation;
        private ShootMessage shootMessage;
        private LagCompensation lagCompensation;
        private MouseLook mouseLook;
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

            EasySettings easySettings = EasySettings.Current;
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            tolerance = easySettings.Get<float>(nameof(tolerance));
        }

        public void DoLocalUpdate(int inputTick, int rollbackTick)
        {
            if (!secondaryFire.WasPressed || !cooldownHandler.CanCast(cooldownIndex))
                return;

            cooldownHandler.Cast(cooldownIndex);
            shootMessage.SetValues(cam.position, mouseLook.RotationX, mouseLook.RotationY, cameraInterpolation.LerpValue, inputTick, rollbackTick);
            shootMessage.debugEnemyPos = Vector3.zero;
            // CmdShoot(new ShootMessage(cameraInterpolation.lerpValue, movement.id - 1,
            //    playerLook.RotationY, playerLook.RotationY, movement.currentTick - 1));

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
                    shootMessage.rollbackTick = playerEntity.PlayerTicks.rollbackTick;
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
            AllowNoregLenience(shootMessage);

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

        private void AllowNoregLenience(ShootMessage shootMessage)
        {
            float dist = Vector3.Distance(cam.position, shootMessage.origin);
            if (dist <= tolerance)
                return;

            // DEBUG.
            Vector3 debugDiff = (cam.position - shootMessage.origin) / Time.fixedDeltaTime;
            Debug.LogWarning($"if (cam.position != shootMessage.origin) | if ({cam.position} != {shootMessage.origin})");
            Debug.LogWarning($"diff {debugDiff.magnitude}");

            // ALLOW LENIENCY IF WAS RECENTLY BOOPED AND ALSO RAYCAST LOS.
            Vector3 dir = shootMessage.origin - cam.position;
            dir.Normalize();
            if (!Physics.Raycast(cam.position, dir, out RaycastHit hit, dist, noregMask, QueryTriggerInteraction.Ignore))
            {
                cam.position = shootMessage.origin;
            }
            else
            {
                Vector3 difference = cam.position - hit.point;
                dist = difference.magnitude - 0.1f;
                if (dist > tolerance)
                {
                    difference = Vector3.ClampMagnitude(difference, dist);
                    cam.position += difference;
                }
            }
        }

        [Server]
        public void DoServerTick(int receivedInputTick)
        {
            origin = eyes.position;
            velocity = rb.linearVelocity;
            for (int i = 0; i < pendingShootMessages.Count; i++)
            {
                if (receivedInputTick < pendingShootMessages[i].inputTick || !cooldownHandler.CanCast(cooldownIndex))
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