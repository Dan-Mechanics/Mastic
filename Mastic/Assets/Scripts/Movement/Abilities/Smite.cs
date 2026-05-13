using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Smite : NetworkBehaviour, IMovementAbility
    {
        [SerializeField] private float teleportRange = default;
        [SerializeField] private LayerMask teleportMask = default;
        [SerializeField] private float explosionForce = default;
        [SerializeField] private float explosionRadius = default;
        [SerializeField] private LayerMask explosionMask = default;
        [SerializeField] private EasyBinding primaryFire = default;
        [SerializeField] private int maxPendingRequests = default;
        [SerializeField] private int tickDuration = default;
        [SerializeField] private int cooldownIndex = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private PhysicsMovement physicsMovement;
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
            previousTick = -1;
            startingTick = -1;
            endingTick = -1;
        }

        [Client]
        public void DoLocalTick(int movementTick, IMovement movement)
        {
            if (!primaryFire.IsHeld || !CanCast(movementTick))
                return;

            cooldownHandler.Cast(cooldownIndex);
            pendingRequests.Add(movementTick);
            CmdRequestSmite(movementTick);
        }

        private bool IsAbilityActive(int tick) => tick >= startingTick && tick <= endingTick;

        [Command]
        private void CmdRequestSmite(int tick)
        {
            if (pendingRequests.Count >= maxPendingRequests || tick <= previousTick)
                return;

            pendingRequests.Add(tick);
            previousTick = tick;
            Debug.LogWarning($"{gameObject.name}: requested burning wings {tick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int tick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    Cast(tick);
            }

            physicsMovement.EnableGravity(!IsAbilityActive(tick));
            if (tick == endingTick)
            {
                Vector3 point = Vector3.zero;
                if (GetTeleportPoint(ref point))
                    Teleport(point);
            }
        }

        private bool GetTeleportPoint(ref Vector3 point)
        {
            entity.EnableHitbox(false);
            bool found = Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, teleportRange, teleportMask, QueryTriggerInteraction.Ignore);
            entity.EnableHitbox(true);
            if (found)
                point = hit.point;

            return found;
        }

        private void Teleport(Vector3 point)
        {
            point += Vector3.up * 0.5f;
            transform.position = point;
            rb.linearVelocity = Vector3.zero;
            if (!isServer)
                return;

            Collider[] colliders = Physics.OverlapSphere(point, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
            foreach (Collider coll in colliders)
            {
                Transform target = coll.transform.root;
                if (!target.TryGetComponent(out Rigidbody targetRb) || targetRb == rb)
                    continue;

                Vector3 dir = target.position - transform.position;
                targetRb.AddForce(Utils.GetSafeNormal(dir) * explosionForce, ForceMode.VelocityChange);
            }
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int movementTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(movementTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(movementTick);
                pendingRequests.RemoveAt(i);
                break;
            }


            physicsMovement.EnableGravity(!IsAbilityActive(movementTick));
            if (endingTick == movementTick)
            {
                Vector3 point = Vector3.zero;
                if (GetTeleportPoint(ref point))
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