using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Jump : NetworkBehaviour, IMovementAbility
    {
        [SerializeField] private float speed = default;
        [SerializeField] private EasyBinding jump = default;
        [SerializeField] private int maxPendingRequests = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private int previousTick;
        private Rigidbody rb;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            cooldownHandler = GetComponent<CooldownHandler>();
            previousTick = -1;
        }

        [Client]
        public void DoLocalTick(int movementTick, IMovement movement)
        {
            if (jump.IsHeld && CanJump(movement))
            {
                cooldownHandler.Cast(cooldownHandler.Last);
                pendingRequests.Add(movementTick);
                CmdRequestJump(movementTick);
            }
        }

        [Command]
        private void CmdRequestJump(int tick)
        {
            if (pendingRequests.Count >= maxPendingRequests || tick <= previousTick)
                return;

            pendingRequests.Add(tick);
            previousTick = tick;
            Debug.LogWarning($"{gameObject.name}: requested jump {tick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int tick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    PerformJump();
            }
        }

        [Server]
        public void CheckAgainstTickServer(int tick, IMovement movement, int movementTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick != pendingRequests[i] || !CanJump(movement))
                    continue;

                cooldownHandler.Cast(cooldownHandler.Last);
                PerformJump();
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

        private bool CanJump(IMovement movement)
        {
            return movement.IsGrounded &&
                cooldownHandler.CanCast(cooldownHandler.Last);
        }

        private void PerformJump()
        {
            Vector3 force = Vector3.up * speed;
            if (rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            rb.AddForce(force, ForceMode.VelocityChange);
        }
    }
}
