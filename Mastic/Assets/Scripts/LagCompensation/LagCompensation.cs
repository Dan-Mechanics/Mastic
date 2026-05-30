using Mirror;
using System.Collections.Generic;
using UnityEngine;

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
        public int GetProjectileRollbackTickCount(ref int rollbackTick)
        {
            if (rollbackTick < oldestTick)
                rollbackTick = oldestTick;

            if (rollbackTick >= currentTick)
                return 0;

            // WILL BE AT LEAST 1, AND WILL NEVER BE GREATER THAN MAXRECORDINGLENGTH.
            return currentTick - rollbackTick;
        }

        /// <summary>
        /// This should only be called after RecordFrame().
        /// </summary>
        [Server]
        public void ReturnToPresent()
        {
            for (int i = 0; i < entities.Count; i++)
            {
                entities[i].ReturnToPresent();
            }

            Physics.SyncTransforms();
        }

        [Server]
        public void RecordFrame()
        {
            entities.ForEach(x => x.RecordFrame(currentTick));
            currentTick++;

            oldestTick = currentTick - maxRecordingLength;
            if (oldestTick < 0)
                oldestTick = 0;
        }

        [Server]
        public void Register(IEntity entity) 
        {
            if (entity != null && !entities.Contains(entity))
                entities.Add(entity);
        }

        /// <summary>
        /// This should be called first in the sequence.
        /// </summary>
        [Server]
        public void RemoveNullEntities()
        {
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (entities[i] == null)
                    entities.RemoveAt(i);
            }
        }
    }
}