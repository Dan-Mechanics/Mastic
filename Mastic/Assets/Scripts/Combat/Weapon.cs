using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Weapon : NetworkBehaviour, IAttack
    {
        public event Action<float> OnDealDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;

        private List<ShootMessage> pendingShootMessages;
        private ICameraInterpolation interpolation;
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
            interpolation = cam.GetComponent<ICameraInterpolation>();
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

            shootMessage.SetValues(mouseLook.RotationX, mouseLook.RotationY, interpolation.LerpValue, movementTick, rollbackTick);
            shootMessage.SetDebugValues(cam.position, Vector3.zero);
            // CmdShoot(new ShootMessage(cameraInterpolation.lerpValue, movement.id - 1,
            //    playerLook.RotationY, playerLook.RotationY, movement.currentTick - 1));

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root.TryGetComponent(out IDamagable damagable))
                {
                    OnPredictDamage?.Invoke(damage);
                    shootMessage.SetDebugValues(cam.position, hit.transform.root.position);
                }
            }

            CmdShoot(shootMessage);
        }

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) => OnDealDamage?.Invoke(damage);

        [Command]
        private void CmdShoot(ShootMessage shootMessage) => pendingShootMessages.Add(shootMessage);

        [Server]
        private void Shoot(ShootMessage shootMessage) 
        {
            lagCompensation.SetAsTick(shootMessage.rollbackTick);
            
            // WE DON'T HAVE TO MOVE THE PLAYER HERE
            // BECAUSE RAYCASTS OF A 
            mouseLook.SetAsRotation(shootMessage.xRotation, shootMessage.yRotation);
            cam.rotation = eyes.rotation;
            interpolation.Interject(pos, prevPos, vel);
            interpolation.SetValue(shootMessage.lerpValue);

            // FOR DEBUG.
            if (cam.position != shootMessage.debugEyesPos)
            {
                Debug.LogWarning($"if (cam.position != shootMessage.origin) | if ({cam.position} != {shootMessage.debugEyesPos})");
                Debug.LogWarning($"{(cam.position - shootMessage.debugEyesPos) / Time.fixedDeltaTime}");
                //cam.position = shootMessage.eyesPos;

                // FUTURE: ALLOW LENIENCY IF WAS RECENTLY BOOPED AND ALSO RAYCAST LOS.
            }

            if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root.TryGetComponent(out IDamagable damagable))
                {
                    damagable.Damage(damage);
                    TargetDisplayHitPip(connectionToClient, damage);
                    hit.transform.root.GetComponent<PlayerHealth>().Damage(damage);

                    // FOR DEBUG.
                    if (shootMessage.debugEnemyPos != Vector3.zero && shootMessage.debugEnemyPos != hit.transform.root.position)
                    {
                        Debug.LogWarning($"Recreated enemy pos was not the same as on the client. correct: {shootMessage.debugEnemyPos}, recreated: {hit.transform.root.position}");
                        Debug.LogWarning($"{(shootMessage.debugEnemyPos - hit.transform.root.position) / Time.fixedDeltaTime}");
                    }
                }
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