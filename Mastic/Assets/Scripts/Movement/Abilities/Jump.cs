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

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cooldownHandler = GetComponent<CooldownHandler>();
            previousTick = -1;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (jump.IsHeld && CanJump(movement))
            {
                cooldownHandler.Cast(cooldownHandler.Last);
                pendingRequests.Add(inputTick);
                CmdRequestJump(inputTick);
            }
        }

        [Command]
        private void CmdRequestJump(int inputTick)
        {
            if (pendingRequests.Count >= maxPendingRequests || inputTick <= previousTick)
                return;

            pendingRequests.Add(inputTick);
            previousTick = inputTick;
            print($"{gameObject.name}: requested {GetType().Name} on {inputTick} ...");
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
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanJump(movement))
                    continue;

                cooldownHandler.Cast(cooldownHandler.Last);
                PerformJump();
                pendingRequests.RemoveAt(i);
                break;
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
