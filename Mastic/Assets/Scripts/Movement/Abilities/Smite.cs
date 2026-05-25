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
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (!primaryFire.IsHeld || !CanCast(inputTick))
                return;

            cooldownHandler.Cast(cooldownIndex);
            pendingRequests.Add(inputTick);
            CmdRequestSmite(inputTick);
        }

        private bool IsAbilityActive(int tick) => tick >= startingTick && tick <= endingTick;

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
                if (inputTick == pendingRequests[i])
                    Cast(inputTick);
            }

            physicsMovement.EnableGravity(!IsAbilityActive(inputTick));
            if (inputTick == endingTick)
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
            point += Vector3.up;
            transform.position = point;
            rb.linearVelocity = Vector3.zero;
            if (!isServer)
                return;

            Collider[] colliders = Physics.OverlapSphere(point, explosionRadius, explosionMask, QueryTriggerInteraction.Ignore);
            foreach (Collider coll in colliders)
            {
                Transform target = coll.transform.root;
                if (target == transform)
                    continue;

                if (!target.TryGetComponent(out NetworkMovement networkMovement))
                    continue;

                Vector3 dir = target.position - (point - Vector3.up);
                networkMovement.AddForce(Utils.GetRealNormal(dir) * explosionForce);
            }
        }

        [TargetRpc]
        private void TargetCast(NetworkConnectionToClient conn, int inputTick) => Cast(inputTick);

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(serverTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(serverTick);
                TargetCast(connectionToClient, inputTick);
                pendingRequests.RemoveAt(i);
                break;
            }

            physicsMovement.EnableGravity(!IsAbilityActive(serverTick));
            if (endingTick == serverTick)
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