using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class PiercingIcicle : NetworkBehaviour, IReliableAttackAbility
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private CooldownHandler cooldownHandler = default;
        [SerializeField] private GameObject projectile = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private LayerMask noregMask = default;

        private float lifetime;
        private float damage;
        private int maxPendingRequests;
        private float tolerance;
        private float speed;
        private float standardInterval;
        private float radius;
        private bool hasGravity;

        private List<ShootMessage> pendingShootMessages;
        private ShootMessage shootMessage;

        private ICameraInterpolation cameraInterpolation;
        private LagCompensation lagCompensation;
        private string cooldownName;
        private MouseLook mouseLook;
        private Collider coll;
        private Rigidbody rb;
        private Transform eyes;
        private Transform cam;
        private Vector3 vel;
        private Vector3 origin;
        private Vector3 prevOrigin;
        private int previousTick;

        private void Awake()
        {
            eyes = transform.Find("eyes");
            cam = GameObject.FindWithTag("MainCamera").transform;
            cameraInterpolation = cam.GetComponent<ICameraInterpolation>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            pendingShootMessages = new List<ShootMessage>();
            mouseLook = GetComponent<MouseLook>();
            rb = GetComponent<Rigidbody>();
            coll = GetComponentInChildren<Collider>();
            prevOrigin = eyes.position;
            previousTick = -1;

            EasySettings easySettings = EasySettings.Current;
            int standardTickrate = easySettings.Get<int>(nameof(standardTickrate));
            standardInterval = 1f / standardTickrate;
            cooldownName = nameof(PiercingIcicle);
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            tolerance = easySettings.Get<float>(nameof(tolerance));
            damage = easySettings.Get<float>(cooldownName + nameof(damage));
            lifetime = easySettings.Get<float>(cooldownName + nameof(lifetime));
            speed = easySettings.Get<float>(cooldownName + nameof(speed));
            radius = easySettings.Get<float>(cooldownName + nameof(radius));
            hasGravity = easySettings.Get<bool>(cooldownName + nameof(hasGravity));
        }

        public void DoLocalUpdate(int inputTick, int rollbackTick)
        {
            if (!primaryFire.WasPressed || !cooldownHandler.CanCast(cooldownName))
                return;

            OnPredictDamage?.Invoke(damage);
            cooldownHandler.Cast(cooldownName);
            shootMessage.SetValues(cam.position, mouseLook.RotationX, mouseLook.RotationY, cameraInterpolation.LerpValue, inputTick, rollbackTick);
            CmdShoot(shootMessage);

            // CONSIDER STORING EACH PROJECTILE WITH AN
            // ID SO YOU CAN RECONSILE IT.
            SpawnProjectile(cam.position, cam.forward * speed);
        }

        private void SpawnProjectile(Vector3 origin, Vector3 velocity)
        {
            GameObject proj = Instantiate(projectile, origin, Quaternion.identity);
            proj.GetComponent<Projectile>().Initialize(velocity, radius, hasGravity, coll, isServer, damage, lifetime);
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
            mouseLook.SetRotationDirectly(shootMessage.xRotation, shootMessage.yRotation);
            cameraInterpolation.Interject(origin, prevOrigin, vel);
            cameraInterpolation.SetValue(shootMessage.lerpValue);
            //AllowNoregLenience(shootMessage);
            IReliableAttackAbility.AllowNoregLenience(cam, shootMessage, tolerance, noregMask);

            // ACCOUNT FOR TRAVEL TIME OF PACKET.
            Vector3 projectileOrigin = cam.position;
            Vector3 projectileVelocity = cam.forward * speed;
            int tickCount = lagCompensation.GetProjectileRollbackTickCount(ref shootMessage.rollbackTick);
            for (int i = 0; i < tickCount; i++)
            {
                lagCompensation.SetAsTick(shootMessage.rollbackTick + i);
                if (!Physics.SphereCast(projectileOrigin, radius, projectileVelocity.normalized, out RaycastHit hit, projectileVelocity.magnitude * standardInterval, mask, QueryTriggerInteraction.Ignore))
                {
                    // HAVEN'T HIT SOMETHING YET, INCREMENT POSITION.
                    projectileOrigin += projectileVelocity * standardInterval;
                    if (hasGravity)
                        projectileVelocity += Physics.gravity * standardInterval;

                    continue;
                }

                if (hit.transform.TryGetComponent(out IDamagable damagable))
                    damagable.Damage(damage);

                return;
            }

            // ACTUALLY SPAWN PROJECTILE,
            // ONLY IF NOT ALREADY COLLIDED WITH SOMETHING IN PAST.
            SpawnProjectile(projectileOrigin, projectileVelocity);
        }

        [Server]
        public void DoServerTick(int processedTick)
        {
            origin = eyes.position;
            vel = rb.linearVelocity;
            for (int i = 0; i < pendingShootMessages.Count; i++)
            {
                if (processedTick < pendingShootMessages[i].inputTick || !cooldownHandler.CanCast(cooldownName))
                    continue;

                cooldownHandler.Cast(cooldownName);
                Shoot(pendingShootMessages[i]);
                pendingShootMessages.RemoveAt(i);
                break;
            }

            prevOrigin = origin;
        }
    }
}