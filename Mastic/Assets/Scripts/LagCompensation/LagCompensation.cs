using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

namespace Mastic
{
    public class LagCompensation : MonoBehaviour
    {
        public int MaxRecordingLength => maxRecordingLength;
        
        [SerializeField, Min(1)] private int maxRecordingLength = default;
        private readonly List<IEntity> entities = new List<IEntity>();
        private int currentTick;
        private int oldestTick;

        [Server]
        public void SetAsTick(int tick)
        {
            if (tick >= currentTick)
                return;

            if (tick < oldestTick)
                tick = oldestTick;

            entities.ForEach(x => x.SetAsTick(tick));
            Physics.SyncTransforms();
        }

        [Server]
        public void SavePresent()
        {
            entities.ForEach(x => x.SavePresent());
        }

        [Server]
        public void ReturnToPresent()
        {
            entities.ForEach(x => x.ReturnToPresent());
        }

        [Server]
        public void RecordFrame()
        {
            entities.ForEach(x => x.RecordFrame(currentTick, maxRecordingLength));
            currentTick++;

            // MAKE SURE TO TEST THIS !!
            oldestTick = Mathf.Max(0, currentTick - maxRecordingLength);
        }

        [Server]
        public void Register(PlayerEntity entity) 
        {
            if (entity != null && !entities.Contains(entity))
                entities.Add(entity);
        }

        [Server]
        public void CleanEntities()
        {
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (entities[i] == null)
                    entities.RemoveAt(i);
            }
        }
    }
}