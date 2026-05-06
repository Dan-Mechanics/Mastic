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
        public void DoLocalUpdate(int movementTick)
        {
            if (ability1.WasPressed && cooldownHandler.CanCast(0))
            {
                pendingRequests.Add(movementTick);
                cooldownHandler.Cast(0);
                CmdRequestGust(movementTick);
            }
        }

        [Command]
        private void CmdRequestGust(int tick)
        {
            if (pendingRequests.Count >= maxPendingRequests || tick <= previousTick)
                return;

            if (!cooldownHandler.CanCast(0))
                return;

            cooldownHandler.Cast(0);
            pendingRequests.Add(tick);
            previousTick = tick;
            Debug.LogWarning($"{gameObject.name}: requested gust {tick} ...");
        }

        public void CheckAgainstTick(int tick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    CheckGust(movement);
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

        private void CheckGust(IMovement movement)
        {
            Vector3 force = eyes.forward * speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            movement.AddForce(force);
        }
    }
}
