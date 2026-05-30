using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class PiercingIcicle : NetworkBehaviour, IAttackAbility
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private CooldownHandler cooldownHandler = default;
        [SerializeField] private int cooldownIndex = default;
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
            maxPendingRequests = easySettings.Get<int>(nameof(maxPendingRequests));
            tolerance = easySettings.Get<float>(nameof(tolerance));
            damage = easySettings.Get<float>(nameof(PiercingIcicle) + nameof(damage));
            lifetime = easySettings.Get<float>(nameof(PiercingIcicle) + nameof(lifetime));
            speed = easySettings.Get<float>(nameof(PiercingIcicle) + nameof(speed));
            radius = easySettings.Get<float>(nameof(PiercingIcicle) + nameof(radius));
            hasGravity = easySettings.Get<bool>(nameof(PiercingIcicle) + nameof(hasGravity));
        }

        public void DoLocalUpdate(int inputTick, int rollbackTick)
        {
            if (!primaryFire.WasPressed || !cooldownHandler.CanCast(cooldownIndex))
                return;

            cooldownHandler.Cast(cooldownIndex);
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
            AllowNoregLenience(shootMessage);

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
            vel = rb.linearVelocity;
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