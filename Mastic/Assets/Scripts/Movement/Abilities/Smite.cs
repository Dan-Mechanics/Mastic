using Mirror;
using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

namespace Mastic
{
    public class Smite : NetworkBehaviour, IMovementAbility, IDamageFeedback
    {
        public event Action<float> OnAuthoritativeDamage;
        public event Action<float> OnPredictDamage;

        [SerializeField] private float teleportRange = default;
        [SerializeField] private LayerMask teleportMask = default;
        [SerializeField] private float explosionForce = default;
        [SerializeField] private float explosionRadius = default;
        [SerializeField] private LayerMask explosionMask = default;
        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private GameObject smiteEffect = default;
        [SerializeField] private GameObject smiteImpactEffect = default;
        [SerializeField] private int maxPendingRequests = default;
        [SerializeField] private int tickDuration = default;
        [SerializeField] private float damage = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private PhysicsMovement physicsMovement;
        private float smiteImpactEffectDuration;
        private int cooldownIndex;
        private PlayerEntity entity;
        private int previousTick;
        private int startingTick;
        private int endingTick;
        private Transform cam;
        private Rigidbody rb;

        private void Awake()
        {
            cam = GameObject.FindWithTag("MainCamera").transform;
            rb = GetComponent<Rigidbody>();
            physicsMovement = GetComponent<PhysicsMovement>();
            cooldownHandler = GetComponent<CooldownHandler>();
            entity = GetComponent<PlayerEntity>();
            cooldownIndex = cooldownHandler.GetIndexFromName(nameof(Smite));
            EasySettings easySettings = EasySettings.Current;
            smiteImpactEffectDuration = easySettings.Get<float>(nameof(smiteImpactEffectDuration));
            previousTick = -1;
            startingTick = -1;
            endingTick = -1;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (!primaryFire.IsHeld || !CanCast(inputTick))
                return;

            OnPredictDamage?.Invoke(damage);
            Instantiate(smiteEffect, transform.position, Quaternion.identity);
            cooldownHandler.Cast(cooldownIndex);
            pendingRequests.Add(inputTick);
            CmdRequestSmite(inputTick);
        }

        private bool IsAbilityActive(int tick) 
            => tick >= startingTick && tick <= endingTick;

        [Command]
        private void CmdRequestSmite(int inputTick)
        {
            if (pendingRequests.Count >= maxPendingRequests || inputTick <= previousTick)
                return;

            pendingRequests.Add(inputTick);
            previousTick = inputTick;
            print($"{gameObject.name}: requested {GetType().Name} on {inputTick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int inputTick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick != pendingRequests[i])
                    continue;

                Instantiate(smiteEffect, transform.position, Quaternion.identity);
                Cast(inputTick);
            }

            physicsMovement.EnableGravity(!IsAbilityActive(inputTick));
            if (inputTick == endingTick)
            {
                Vector3 point = Vector3.zero;
                if (NetcodeUtils.GetAimingPoint(entity, cam, teleportRange, teleportMask, ref point))
                    Teleport(point);
            }
        }

        /*private bool GetTeleportPoint(ref Vector3 point)
        {
            entity.EnableHitbox(false);
            bool found = Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, teleportRange, teleportMask, QueryTriggerInteraction.Ignore);
            entity.EnableHitbox(true);
            if (found)
                point = hit.point;

            return found;
        }*/

        private void Teleport(Vector3 point)
        {
            Vector3 effectPos = point;
            SpawnImpactEffect(effectPos);

            point += Vector3.up;
            transform.position = point;
            rb.linearVelocity = Vector3.zero;
            if (!isServer)
                return;

            RpcTeleport(effectPos);

            float totalDamage = 0f;
            Collider[] colliders = Physics.OverlapSphere(point, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < colliders.Length; i++)
            {
                Transform target = colliders[i].transform.root;
                if (target == transform)
                    continue; // PREVENT SELF-DAMAGE.

                // ADD KNOCKBACK.
                if (target.TryGetComponent(out NetworkMovement networkMovement))
                {
                    Vector3 dir = target.position - (point - Vector3.up);
                    networkMovement.AddForce(Utils.Normalize(dir) * explosionForce);
                }

                // DEAL DAMAGE.
                if (target.TryGetComponent(out IDamagable damagable))
                {
                    damagable.Damage(damage);
                    totalDamage += damage;
                }
            }

            if (totalDamage > 0f)
                TargetDisplayHitPip(connectionToClient, totalDamage);
        }

        private void SpawnImpactEffect(Vector3 effectPos)
        {
            GameObject go = Instantiate(smiteImpactEffect, effectPos, Quaternion.identity);
            Destroy(go, smiteImpactEffectDuration);
        }

        [TargetRpc]
        private void TargetCast(NetworkConnectionToClient conn, int inputTick) 
            => Cast(inputTick);

        [TargetRpc]
        public void TargetDisplayHitPip(NetworkConnectionToClient conn, float damage) 
            => OnAuthoritativeDamage?.Invoke(damage);

        [ClientRpc]
        private void RpcTeleport(Vector3 effectPos)
        {
            if (!isLocalPlayer)
                SpawnImpactEffect(effectPos);
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(serverTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Instantiate(smiteEffect, transform.position, Quaternion.identity);
                Cast(serverTick);
                TargetCast(connectionToClient, inputTick);
                pendingRequests.RemoveAt(i);
                break;
            }

            physicsMovement.EnableGravity(!IsAbilityActive(serverTick));
            if (endingTick == serverTick)
            {
                Vector3 point = Vector3.zero;
                if (NetcodeUtils.GetAimingPoint(entity, cam, teleportRange, teleportMask, ref point))
                    Teleport(point);
            }
        }

        public void CleanPendingRequests(int upTo)
        {
            for (int i = pendingRequests.Count - 1; i >= 0; i--)
            {
                if (pendingRequests[i] <= upTo)
                    pendingRequests.RemoveAt(i);
            }
        }

        private bool CanCast(int tick)
        {
            return !IsAbilityActive(tick) &&
                cooldownHandler.CanCast(cooldownIndex);
        }

        private void Cast(int tick)
        {
            startingTick = tick;
            endingTick = startingTick + tickDuration;
        }
    }
}