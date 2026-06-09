using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class BurningWings : NetworkBehaviour, IMovement, IMovementAbility
    {
        public byte Index { get; set; }
        public bool IsGrounded => false;

        [SerializeField] private float speed = default;
        [SerializeField] private EasyBinding ability2 = default;
        [SerializeField] private int maxPendingRequests = default;
        [SerializeField] private int tickDuration = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private NetworkMovement networkMovement;
        private PhysicsMovement physicsMovement;
        private string cooldownName;
        private int previousTick;
        private int startingTick;
        private int endingTick;
        private Transform eyes;
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            networkMovement = GetComponent<NetworkMovement>();
            eyes = transform.Find("eyes");
            physicsMovement = GetComponent<PhysicsMovement>();
            cooldownHandler = GetComponent<CooldownHandler>();
            cooldownName = nameof(BurningWings);
            previousTick = -1;
            startingTick = -1;
            endingTick = -1;
        }

        [Client]
        public void DoLocalTick(int inputTick, IMovement movement)
        {
            if (!ability2.IsHeld || !CanCast(inputTick))
                return;

            cooldownHandler.Cast(cooldownName);
            pendingRequests.Add(inputTick);
            CmdRequestBurningWings(inputTick);
        }

        public void Move(float vert, float hori, float interval) 
            => rb.linearVelocity = eyes.forward * speed;

        private bool IsAbilityActive(int tick) 
            => tick >= startingTick && tick <= endingTick;

        [Command]
        private void CmdRequestBurningWings(int inputTick)
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

            networkMovement.SetMovement(IsAbilityActive(inputTick) ? Index : physicsMovement.Index);
        }

        [Server]
        public void CheckAgainstTickServer(int inputTick, IMovement movement, int serverTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (inputTick < pendingRequests[i] || !CanCast(serverTick))
                    continue;

                cooldownHandler.Cast(cooldownName);
                Cast(serverTick);
                pendingRequests.RemoveAt(i);
                break;
            }

            networkMovement.SetMovement(IsAbilityActive(serverTick) ? Index : physicsMovement.Index);
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
                cooldownHandler.CanCast(cooldownName);
        }

        /// <summary>
        /// Do nothing because while
        /// in Burning Wings, the player is cc immune.
        /// </summary>
        public void AddForce(Vector3 velocityChange) { }

        private void Cast(int tick)
        {
            startingTick = tick;
            endingTick = startingTick + tickDuration;
        }
    }
}