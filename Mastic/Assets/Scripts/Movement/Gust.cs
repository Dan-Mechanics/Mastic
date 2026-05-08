using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Gust : NetworkBehaviour, IMovementAbility
    {
        [SerializeField] private float speed = default;
        [SerializeField] private EasyBinding ability1 = default;
        [SerializeField] private int maxPendingRequests = default;
        [SerializeField] private int cooldownIndex = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private int previousTick;
        private Transform eyes;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cooldownHandler = GetComponent<CooldownHandler>();
            eyes = transform.Find("eyes");
            previousTick = -1;
        }

        [Client]
        public void DoLocalTick(int movementTick, IMovement movement)
        {
            if (ability1.IsHeld && cooldownHandler.CanCast(cooldownIndex))
            {
                cooldownHandler.Cast(cooldownIndex);
                pendingRequests.Add(movementTick);
                CmdRequestGust(movementTick);
            }
        }

        [Command]
        private void CmdRequestGust(int tick)
        {
            if (pendingRequests.Count >= maxPendingRequests || tick <= previousTick)
                return;

            pendingRequests.Add(tick);
            previousTick = tick;
            Debug.LogWarning($"{gameObject.name}: requested gust {tick} ...");
        }

        public void CheckAgainstTick(int tick, IMovement movement)
        {
            if (isServer)
            {
                CheckAgainstTickServer(tick, movement);
            }
            else
            {
                CheckAgainstTickClient(tick, movement);
            }
        }

        [Client]
        private void CheckAgainstTickClient(int tick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    PerformGust(movement);
            }
        }

        [Server]
        private void CheckAgainstTickServer(int tick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick != pendingRequests[i] || !cooldownHandler.CanCast(cooldownIndex))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                PerformGust(movement);
            }
        }

        public void CleanTicks(int upTo)
        {
            for (int i = pendingRequests.Count - 1; i >= 0; i--)
            {
                if (pendingRequests[i] <= upTo)
                    pendingRequests.RemoveAt(i);
            }
        }

        private void PerformGust(IMovement movement)
        {
            Vector3 force = eyes.forward * speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            movement.AddForce(force);
        }
    }
}
