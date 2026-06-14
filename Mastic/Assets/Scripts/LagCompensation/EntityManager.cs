using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class EntityManager : NetworkBehaviour
    {
        public static EntityManager Current => FindAnyObjectByType<EntityManager>();
        public int MaxRecordingLength => maxRecordingLength;
        public int RollbackTick { get; private set; }

        [SerializeField, Min(1)] private int maxRecordingLength = default;
        private readonly Dictionary<uint, IEntity> entities = new Dictionary<uint, IEntity>();
        private float maxLerpValue;
        private int prevCount = -1;
        private byte[] dataSizes;
        private float syncTime;
        private int currentTick;
        private int oldestTick;
        private byte[] netIds;
        private float[] data;

        private void Awake() 
            => maxLerpValue = EasySettings.Current.Get<float>(nameof(maxLerpValue));

        /// <summary>
        /// Called every 32th of a second, not more than once per frame.
        /// </summary>
        [Server]
        public void DoSync()
        {
            RemoveNullEntities();
            RecordFrames();

            if (prevCount != entities.Count)
                Reallocate();

            prevCount = entities.Count;

            int index = 0;
            foreach (var pair in entities)
            {
                pair.Value.WriteToData(index, data);
                index += pair.Value.DataSize;
            }

            RpcSync(currentTick - 1, netIds, dataSizes, data);
        }

        [ClientRpc]
        private void RpcSync(int tick, byte[] netIds, byte[] dataSizes, float[] data)
        {
            RemoveNullEntities();

            RollbackTick = tick;
            syncTime = Time.time;

            int index = 0;
            for (int i = 0; i < netIds.Length; i++)
            {
                if (entities.ContainsKey(netIds[i]))
                    entities[netIds[i]].ReadFromData(index, data, syncTime);

                index += dataSizes[i];
            }
        }

        public void RemoveNullEntities()
        {
            foreach (var pair in entities)
            {
                if (pair.Value == null)
                    entities.Remove(pair.Key);
            }
        }

        public void Register(uint netId, IEntity entity)
        {
            if (entity == null)
                return;

            entities[netId] = entity;
        }

        private void Reallocate()
        {
            netIds = new byte[entities.Count];
            dataSizes = new byte[entities.Count];

            int i = 0;
            int totalDataLength = 0;
            foreach (var pair in entities)
            {
                totalDataLength += pair.Value.DataSize;
                netIds[i] = (byte)pair.Key;
                dataSizes[i] = pair.Value.DataSize;
                i++;
            }

            data = new float[totalDataLength];
        }

        [Client]
        public float GetUnlocalLerpValue()
        {
            return Mathf.Clamp(Time.time - syncTime, 0f, maxLerpValue);
        }

        [Server]
        public void DoRollback(int tick, float lerpValue)
        {
            if (tick >= currentTick)
                return;
            
            if (tick < oldestTick)
                tick = oldestTick;

            lerpValue = Mathf.Clamp(lerpValue, 0f, maxLerpValue);
            int prevtick = tick - 1;
            if (prevtick < oldestTick)
                prevtick = oldestTick;

            foreach (var pair in entities)
            {
                pair.Value.DoRollback(prevtick, tick, lerpValue);
            }

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

        [Server]
        public void SavePresent()
        {
            foreach (var pair in entities)
            {
                pair.Value.SavePresent();
            }
        }

        [Server]
        public void ReturnToPresent()
        {
            foreach (var pair in entities)
            {
                pair.Value.ReturnToPresent();
            }

            Physics.SyncTransforms();
        }

        [Server]
        public void RecordFrames()
        {
            foreach (var pair in entities)
            {
                pair.Value.RecordFrame(currentTick);
            }

            currentTick++;
            oldestTick = currentTick - maxRecordingLength;
            if (oldestTick < 0)
                oldestTick = 0;
        }
    }
}