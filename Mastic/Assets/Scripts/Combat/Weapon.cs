using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Weapon : NetworkBehaviour, IAttackAbility
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;

        private List<ShootMessage> pendingShootMessages;
        private ICameraInterpolation cameraInterpolation;
        private LagCompensation lagCompensation;
        private ShootMessage shootMessage;
        private MouseLook mouseLook;
        private Rigidbody rb;
        private Transform eyes;
        private Transform cam;
        private Vector3 pos;
        private Vector3 prevPos;
        private Vector3 vel;

        private void Awake()
        {
            eyes = transform.Find("eyes");
            cam = GameObject.FindWithTag("MainCamera").transform;
            cameraInterpolation = cam.GetComponent<ICameraInterpolation>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            pendingShootMessages = new List<ShootMessage>();
            mouseLook = GetComponent<MouseLook>();
            rb = GetComponent<Rigidbody>();
            pos = transform.position;
            prevPos = pos;
        }

        public void DoLocalUpdate(int movementTick, int rollbackTick)
        {
            if (!primaryFire.WasPressed)
                return;

            shootMessage.SetValues(cam.position, mouseLook.RotationX, mouseLook.RotationY, cameraInterpolation.LerpValue, movementTick, rollbackTick);
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
                    shootMessage.rollbackTick = playerEntity.RollbackTick;
            }

            CmdShoot(shootMessage);
        }

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) => OnAuthoritativeDamage?.Invoke(damage);

        [Command]
        private void CmdShoot(ShootMessage shootMessage) => pendingShootMessages.Add(shootMessage);

        [Server]
        private void Shoot(ShootMessage shootMessage) 
        {
            // RECREATE THE SHOT CONDITIONS.
            lagCompensation.SetAsTick(shootMessage.rollbackTick);
            
            mouseLook.SetAsRotation(shootMessage.xRotation, shootMessage.yRotation);
            cameraInterpolation.Interject(pos, prevPos, vel);
            cameraInterpolation.SetValue(shootMessage.lerpValue);

            // DEBUG.
            if (cam.position != shootMessage.origin)
            {
                Debug.LogWarning($"if (cam.position != shootMessage.origin) | if ({cam.position} != {shootMessage.origin})");
                Debug.LogWarning($"{(cam.position - shootMessage.origin) / Time.fixedDeltaTime}");
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

        /// <summary>
        /// You could also look in the server's buffer
        /// for the client movement state but this leaves 
        /// door open for cheats... See if this works first.
        /// </summary>
        [Server]
        public void DoServerTick(int movementTick)
        {
            prevPos = pos;
            pos = eyes.position;
            vel = rb.linearVelocity;
            for (int i = pendingShootMessages.Count - 1; i >= 0; i--)
            {
                if (movementTick >= pendingShootMessages[i].movementTick)
                {
                    Shoot(pendingShootMessages[i]);
                    pendingShootMessages.RemoveAt(i);
                }
            }
        }
    }
}