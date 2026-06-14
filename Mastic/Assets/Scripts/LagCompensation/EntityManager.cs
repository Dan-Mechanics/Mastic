using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class EntityManager : NetworkBehaviour
    {
        public static EntityManager Current => FindAnyObjectByType<EntityManager>();

        public int MaxRecordingLength => maxRecordingLength;
        public int Tick { get; private set; }
        public float CurrentTime { get; private set; }

        [SerializeField, Min(1)] private int maxRecordingLength = default;
        private readonly Dictionary<uint, IEntity> entities = new Dictionary<uint, IEntity>();
        private int prevCount = -1;
        private byte[] dataSizes;
        private int currentTick;
        private int oldestTick;
        private byte[] netIds;
        private float[] data;

        [Server]
        public void DoTick()
        {
            RemoveNullEntities();
            if (prevCount != entities.Count)
                Reallocate();

            prevCount = entities.Count;

            int index = 0;
            foreach (var pair in entities)
            {
                pair.Value.WriteToData(index, data);
                index += pair.Value.DataSize;
            }

            RpcSync(Tick, netIds, dataSizes, data);
            Tick++;
        }

        [ClientRpc]
        private void RpcSync(int tick, byte[] netIds, byte[] dataSizes, float[] data)
        {
            RemoveNullEntities();

            Tick = tick;
            CurrentTime = Time.time;

            int index = 0;
            for (int i = 0; i < netIds.Length; i++)
            {
                if (entities.ContainsKey(netIds[i]))
                    entities[netIds[i]].ReadFromData(index, data, CurrentTime);

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

            entity.Initialize(maxRecordingLength);
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
        public void RecordFrame()
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