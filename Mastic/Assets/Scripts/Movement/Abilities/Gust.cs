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
        private string cooldownName;
        private int previousTick;
        private Transform eyes;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cooldownName = nameof(Gust);
            cooldownHandler = GetComponent<CooldownHandler>();
            eyes = transform.Find("eyes");
            previousTick = -1;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (ability1.IsHeld && cooldownHandler.CanCast(cooldownName))
            {
                cooldownHandler.Cast(cooldownName);
                pendingRequests.Add(inputTick);
                CmdRequestGust(inputTick);
            }
        }

        [Command]
        private void CmdRequestGust(int inputTick)
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
                    PerformGust(movement);
            }
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !cooldownHandler.CanCast(cooldownName))
                    continue;

                cooldownHandler.Cast(cooldownName);
                PerformGust(movement);
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

        private void PerformGust(IMovement movement)
        {
            Vector3 force = eyes.forward * speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            movement.AddForce(force);
        }
    }
}
