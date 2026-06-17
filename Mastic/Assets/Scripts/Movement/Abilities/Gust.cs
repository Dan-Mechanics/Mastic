using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Gust : NetworkBehaviour, IMovementAbility
    {
        [SerializeField] private float speed = default;
        [SerializeField] private EasyBinding ability1 = default;
        [SerializeField] private GameObject gustEffect = default;
        [SerializeField] private int maxPendingRequests = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private bool isInputChambered;
        private int cooldownIndex;
        private int previousTick;
        private Transform eyes;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cooldownHandler = GetComponent<CooldownHandler>();
            cooldownIndex = cooldownHandler.GetIndexFromName(nameof(Gust));
            eyes = transform.Find("eyes");
            previousTick = -1;
        }

        private void Update()
        {
            if (isLocalPlayer && ability1.WasPressed)
                isInputChambered = true;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (isInputChambered && cooldownHandler.CanCast(cooldownIndex))
            {
                cooldownHandler.Cast(cooldownIndex);
                pendingRequests.Add(inputTick);
                CmdRequestGust(inputTick);
            }

            isInputChambered = false;
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
                if (inputTick < pendingRequests[i] || !cooldownHandler.CanCast(cooldownIndex))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
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
            Instantiate(gustEffect, transform.position, Quaternion.identity);
            Vector3 force = eyes.forward * speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            movement.AddForce(force);
        }
    }
}
