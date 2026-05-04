using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    [Serializable]
    public class MovementAbility
    {
        public event Action OnCast;

        public float speed;
        public int cooldownTicks;

        private readonly List<int> requestTicks = new List<int>();
        private int lastTickRequested = -1;
        private int lastTickCasted;

        private void Cast(NetworkMovement networkPhysicsMovement) 
        {
            lastTickCasted = networkPhysicsMovement.MovementTick;
            OnCast?.Invoke();
        }

        public void AddRequestTick(int requestTick)
        {
            if (requestTicks.Count >= NetworkMovement.MAX_PENDING_INPUT_COUNT)
                return;

            if (requestTick <= lastTickRequested)
                return;

            requestTicks.Add(requestTick);
            lastTickRequested = requestTick;
            Debug.LogWarning($"requests: {requestTicks.Count}");
        }

        public void CleanRequests(int upTo)
        {
            for (int i = requestTicks.Count - 1; i >= 0; i--)
            {
                if (requestTicks[i] <= upTo)
                    requestTicks.RemoveAt(i);
            }
        }

        public bool CanCast(NetworkMovement networkMovement) => networkMovement.MovementTick - lastTickCasted >= cooldownTicks;

        public void CastOnTick(int tick, NetworkMovement networkMovement) 
        {
            for (int i = 0; i < requestTicks.Count; i++)
            {
                if (requestTicks[i] == tick && CanCast(networkMovement))
                    Cast(networkMovement);
            }
        }
    }
}