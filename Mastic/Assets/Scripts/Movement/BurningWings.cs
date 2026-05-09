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
        [SerializeField] private int cooldownIndex = default;

        private readonly List<int> pendingRequests = new List<int>();
        private CooldownHandler cooldownHandler;
        private NetworkMovement networkMovement;
        private PhysicsMovement physicsMovement;
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
            previousTick = -1;
            startingTick = -1;
            endingTick = -1;
        }

        [Client]
        public void DoLocalTick(int movementTick, IMovement movement)
        {
            if (ability2.IsHeld && CanCast(movementTick))
            {
                cooldownHandler.Cast(cooldownIndex);
                pendingRequests.Add(movementTick);
                CmdRequestBurningWings(movementTick);
            }
        }

        private bool IsAbilityActive(int tick) => tick >= startingTick && tick <= endingTick;

        [Command]
        private void CmdRequestBurningWings(int tick)
        {
            if (pendingRequests.Count >= maxPendingRequests || tick <= previousTick)
                return;

            pendingRequests.Add(tick);
            previousTick = tick;
            Debug.LogWarning($"{gameObject.name}: requested burning wings {tick} ...");
        }

        [Client]
        public void CheckAgainstTickClient(int tick, IMovement movement)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick == pendingRequests[i])
                    Cast(tick);
            }

            networkMovement.SetMovement(IsAbilityActive(tick) ? Index : physicsMovement.Index);
        }

        [Server]
        public void CheckAgainstTickServer(int tick, IMovement movement, int movementTick)
        {
            for (int i = 0; i < pendingRequests.Count; i++)
            {
                if (tick != pendingRequests[i] || !CanCast(movementTick))
                    continue;

                cooldownHandler.Cast(cooldownIndex);
                Cast(movementTick);
            }

            networkMovement.SetMovement(IsAbilityActive(movementTick) ? Index : physicsMovement.Index);
        }

        public void CleanTicks(int upTo)
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
                cooldownHandler.CanCast(cooldownIndex);
        }

        private void Cast(int tick)
        {
            startingTick = tick;
            endingTick = startingTick + tickDuration;
        }

        public void Move(float vert, float hori, float interval) => rb.linearVelocity = eyes.forward * speed;
        public void AddForce(Vector3 velocityChange) { }
    }
}
