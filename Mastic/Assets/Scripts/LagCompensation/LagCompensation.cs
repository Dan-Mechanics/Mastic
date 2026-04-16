using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Mastic
{
    /// <summary>
    /// https://docs.google.com/document/d/1ruX-Pqfwd8WIK8eIo0dRAs86pEsc5Z398IOfDLNskYc/edit?tab=t.0#bookmark=id.yvrx5neiq0iy
    /// </summary>
    public class LagCompensation : MonoBehaviour
    {
        public static LagCompensation instance;
        [SerializeField, Min(0)] private int maxRecordingLength = default;
        
        private readonly List<IEntity> entities = new List<IEntity>();
        private int currentTick;

        private void Awake() => instance = this;

        [Server]
        public void RewindTime(int tick, uint callerNetId)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                IEntity entity = entities[i];
                if (entity == null || entity.GetNetId() == callerNetId)
                    continue;

                entity.SavePresent();
                entity.RewindTime(tick);
            }

            Physics.SyncTransforms();
        }

        [Server]
        public void ReturnToPresent()
        {
            entities.ForEach(x => x?.ReturnToPresent());
            Clean();
        }

        [Server]
        public void RecordFrame()
        {
            entities.ForEach(x => x?.RecordFrame(currentTick, maxRecordingLength));
            currentTick++;
        }

        [Server]
        public void Register(PlayerEntity entity) 
        {
            if (entity == null || entities.Contains(entity))
                return;

            entities.Add(entity);
        }

        private void Clean()
        {
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (entities[i] == null)
                    entities.RemoveAt(i);
            }
        }
    }
}