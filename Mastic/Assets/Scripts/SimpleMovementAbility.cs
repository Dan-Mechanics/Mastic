using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    [Serializable]
    public class SimpleMovementAbility
    {
        public float Speed => speed;
        public event Action OnPerform;
        
        [SerializeField] private float speed = 0f;
        [SerializeField] private int cooldownTicks = 0;

        private int lastRequestTick = -1;
        private int lastPerformTick;
        private readonly List<int> requestTicks = new List<int>();

        private void Perform(NetworkPhysicsMovement networkPhysicsMovement) 
        {
            lastPerformTick = networkPhysicsMovement.CurrentTick;

            OnPerform?.Invoke();
        }

        public void AddRequestTick(int requestTick)
        {
            if (requestTicks.Count >= NetworkPhysicsMovement.MAX_PENDING_INPUT_COUNT) { return; }
            if (requestTick <= lastRequestTick) { return; }

            requestTicks.Add(requestTick);
            lastRequestTick = requestTick;

            Debug.LogWarning($"requested ... {requestTicks.Count}");
        }

        public void CleanRequests(int upTo)
        {
            for (int i = requestTicks.Count - 1; i >= 0; i--)
            {
                if (requestTicks[i] <= upTo) { requestTicks.RemoveAt(i); }
            }
        }

        public bool CanPerform(NetworkPhysicsMovement networkPhysicsMovement)
        {
            return networkPhysicsMovement.CurrentTick - lastPerformTick >= cooldownTicks;
        }

        public void Try(NetworkPhysicsMovement networkPhysicsMovement, int inputTick) 
        {
            for (int i = 0; i < requestTicks.Count; i++)
            {
                if (requestTicks[i] == inputTick && CanPerform(networkPhysicsMovement)) { Perform(networkPhysicsMovement); }
            }
        }
    }
}