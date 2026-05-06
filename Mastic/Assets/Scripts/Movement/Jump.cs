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
        private int previousTick;
        private Rigidbody rb;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            previousTick = -1;
        }

        [Client]
        public void DoLocalUpdate(int movementTick)
        {
            if (jump.WasPressed)
            {
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

        public void CheckAgainstTick(int tick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    CheckJump(movement);
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

        private void CheckJump(IMovement movement)
        {
            if (!movement.IsGrounded)
                return;

            Vector3 force = Vector3.up * speed;
            if (rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            movement.AddForce(force);
        }
    }
}
