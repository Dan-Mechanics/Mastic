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
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;

        private List<ReliableShootMessage> pendingShootMessages;
        private ICameraInterpolation cameraInterpolation;
        private ReliableShootMessage shootMessage;
        private LagCompensation lagCompensation;
        private MouseLook mouseLook;
        private Rigidbody rb;
        private Transform eyes;
        private Transform cam;
        private Vector3 origin;
        private Vector3 prevOrigin;
        private Vector3 velocity;

        private void Awake()
        {
            eyes = transform.Find("eyes");
            cam = GameObject.FindWithTag("MainCamera").transform;
            cameraInterpolation = cam.GetComponent<ICameraInterpolation>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            pendingShootMessages = new List<ReliableShootMessage>();
            mouseLook = GetComponent<MouseLook>();
            rb = GetComponent<Rigidbody>();
            prevOrigin = eyes.position;
        }

        public void DoLocalUpdate(int inputTick, int rollbackTick)
        {
            if (!secondaryFire.WasPressed)
                return;

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
                    shootMessage.rollbackTick = playerEntity.Shared.rollbackTick;
            }

            CmdShoot(shootMessage);
        }

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) => OnAuthoritativeDamage?.Invoke(damage);

        [Command]
        private void CmdShoot(ReliableShootMessage shootMessage) => pendingShootMessages.Add(shootMessage);

        [Server]
        private void Shoot(ReliableShootMessage shootMessage) 
        {
            // RECREATE THE SHOT CONDITIONS.
            lagCompensation.SetAsTick(shootMessage.rollbackTick);
            
            mouseLook.SetAsRotation(shootMessage.xRotation, shootMessage.yRotation);
            cameraInterpolation.Interject(origin, prevOrigin, velocity);
            cameraInterpolation.SetValue(shootMessage.lerpValue);

            Debug.Log("shoot message recieved");

            // DEBUG.
            if (cam.position != shootMessage.origin)
            {
                Debug.LogWarning($"if (cam.position != shootMessage.origin) | if ({cam.position} != {shootMessage.origin})");

                Vector3 diff = (cam.position - shootMessage.origin) / Time.fixedDeltaTime;
                Debug.LogWarning($"diff {diff.magnitude}");
                //cam.position = shootMessage.eyesPos;

                // FUTURE: ALLOW LENIENCY IF WAS RECENTLY BOOPED AND ALSO RAYCAST LOS.
            }

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
        public void DoServerTick(int inputTick)
        {
            origin = eyes.position;
            velocity = rb.linearVelocity;
            for (int i = 0; i < pendingShootMessages.Count; i++)
            {
                if (inputTick < pendingShootMessages[i].inputTick || !cooldownHandler.CanCast(cooldownIndex))
                    continue;

                Shoot(pendingShootMessages[i]);
                pendingShootMessages.RemoveAt(i);
                break;
            }

            prevOrigin = origin;
        }
    }
}