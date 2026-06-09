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
        private string cooldownName;
        private int previousTick;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cooldownHandler = GetComponent<CooldownHandler>();
            cooldownName = nameof(Jump).ToLowerInvariant();
            previousTick = -1;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (jump.IsHeld && CanJump(movement))
            {
                cooldownHandler.Cast(cooldownName);
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
        public void CheckAgainstTickClient(int tick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    PerformJump(movement);
            }
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanJump(movement))
                    continue;

                cooldownHandler.Cast(cooldownName);
                PerformJump(movement);
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
                cooldownHandler.CanCast(cooldownName);
        }

        private void PerformJump(IMovement movement)
        {
            Vector3 velChange = Vector3.up * speed;
            if (rb.linearVelocity.y < 0f)
                velChange.y -= rb.linearVelocity.y;

            movement.AddForce(velChange);
        }
    }
}
