using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Weapon : NetworkBehaviour, IShootable
    {
        /// <summary>
        /// Meaning that the local client has dealed damage to an enemy on the server.
        /// </summary>
        public event Action<float> OnDealDamage;

        /// <summary>
        /// Meaning that the local client has hit an enemy on his screen,
        /// but this has not yet been confirmed by the server.
        /// </summary>
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private int playerLayer = default;
        [SerializeField] private int intangibleLayer = default;
        [SerializeField] private float range = default;
        [SerializeField] private float damage = default;

        private List<ShootMessage> pendingShootMessages;
        private ICameraInterpolation interpolation;
        private LagCompensation lagCompensation;
        private ShootMessage shootMessage;
        private PlayerLook playerLook;
        private Rigidbody rb;
        private Transform eyes;
        private Transform cam;
        private Vector3 pos;
        private Vector3 prevPos;
        private Vector3 vel;

        public void Setup(Transform cam, Transform eyes, ICameraInterpolation interpolation, LagCompensation lagCompensation)
        {
            this.cam = cam;
            this.eyes = eyes;
            this.interpolation = interpolation;
            this.lagCompensation = lagCompensation;

            pendingShootMessages = new List<ShootMessage>();
            playerLook = GetComponent<PlayerLook>();
            rb = GetComponent<Rigidbody>();
            pos = transform.position;
            prevPos = pos;
            gameObject.layer = playerLayer;
        }

        public void DoClientUpdate(int movementTick, int rollbackTick)
        {
            if (!primaryFire.WasPressed)
                return;

            shootMessage.SetRotation(playerLook.RotationX, playerLook.RotationY);
            shootMessage.SetTicks(movementTick, rollbackTick);
            shootMessage.SetPosition(cam.position, interpolation.LerpValue);
            CmdShoot(shootMessage);
            // CmdShoot(new ShootMessage(cameraInterpolation.lerpValue, movement.id - 1,
            //    playerLook.RotationY, playerLook.RotationY, movement.currentTick - 1));

            if (!Physics.Raycast(cam.position, cam.forward,
                out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
                return;

            if (!hit.transform.root.TryGetComponent(out IDamagable damagable))
                return;

            OnPredictDamage?.Invoke(damage);
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
            playerLook.SetAsRotation(shootMessage.xRotation, shootMessage.yRotation);
            cam.rotation = eyes.rotation;
            interpolation.Interject(pos, prevPos, vel);
            interpolation.SetValue(shootMessage.lerpValue);
            gameObject.layer = intangibleLayer;

            if (cam.position != shootMessage.origin)
            {
                Debug.LogWarning($"if (cam.position != shootMessage.origin) | if ({cam.position} != {shootMessage.origin})");
                cam.position = shootMessage.origin;
            }

            if (Physics.Raycast(cam.position, cam.forward,  out RaycastHit hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.root.TryGetComponent(out IDamagable damagable))
                {
                    damagable.Damage(damage);
                    TargetDisplayHitPip(connectionToClient, damage);
                    hit.transform.root.GetComponent<PlayerHealth>().Damage(damage);
                }
            }

            gameObject.layer = playerLayer;
        }

        /// <summary>
        /// You could also look in the server's buffer
        /// for the client movement state but this leaves 
        /// door open for cheats... See if this works first.
        /// </summary>
        [Server]
        public void DoShootTick(int movementTick)
        {
            // TODO: MAKE SURE THIS IS CORRECT.
            prevPos = pos;
            pos = transform.position;
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