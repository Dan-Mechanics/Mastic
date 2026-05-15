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
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (ability1.IsHeld && cooldownHandler.CanCast(cooldownIndex))
            {
                cooldownHandler.Cast(cooldownIndex);
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
            Debug.LogWarning($"{gameObject.name}: requested gust {inputTick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int inputTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick == pendingRequests[i])
                    PerformGust();
            }
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !cooldownHandler.CanCast(cooldownIndex))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                PerformGust();
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

        private void PerformGust()
        {
            Vector3 force = eyes.forward * speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            rb.AddForce(force, ForceMode.VelocityChange);
        }
    }
}
