using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class EntityManager : NetworkBehaviour
    {
        public static EntityManager Current => FindAnyObjectByType<EntityManager>(FindObjectsInactive.Include);
        public int MaxRecordingLength => maxRecordingLength;
        public int RollbackTick { get; private set; }

        [SerializeField, Min(1)] private int maxRecordingLength = default;
        private readonly Dictionary<int, IEntity> entities = new Dictionary<int, IEntity>();
        private float clientSyncReceiveTime;
        private float maxLerpValue;
        private byte[] dataSizes;
        private int currentTick;
        private int oldestTick;
        private int[] uniqueIds;
        private int prevCount = -1;
        private int nextUniqueId;
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

            RpcSync(currentTick - 1, uniqueIds, dataSizes, data);
        }

        private void LogArray<T>(T[] array)
        {
            print($"sending array of Length {array.Length}");
            for (int i = 0; i < array.Length; i++)
            {
                print($"{i}: {array[i]}");
            }
        }

        [ClientRpc]
        private void RpcSync(int tick, int[] uniqueIds, byte[] dataSizes, float[] data)
        {
            RemoveNullEntities();
           // netIdentity.id
            Debug.Log($"NETID {netId} SPEAKING !! count of shit is {uniqueIds.Length}");
            LogArray(uniqueIds);
           LogArray(dataSizes);
            LogArray(data);

            RollbackTick = tick;
            clientSyncReceiveTime = Time.time;

            int index = 0;
            for (int i = 0; i < uniqueIds.Length; i++)
            {
                if (entities.ContainsKey(uniqueIds[i]))
                    entities[uniqueIds[i]].ReadFromData(index, data, clientSyncReceiveTime);

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

        [Server]
        public int FetchUniqueId()
            => nextUniqueId++;

        public void Register(int uniqueId, IEntity entity)
        {
            Debug.LogWarning($"netidi{uniqueId}");
            if (entities.ContainsKey(uniqueId) || entity == null)
            {
                Debug.LogError($"Register failed for {uniqueId}.");
                return;
            }
            
            // POSSIBLY ADD INIT HERE FOR DECOUPLING.
            entities[uniqueId] = entity;
        }

        private void Reallocate()
        {
            uniqueIds = new int[entities.Count];
            dataSizes = new byte[entities.Count];

            int i = 0;
            int totalDataLength = 0;
            foreach (var pair in entities)
            {
                totalDataLength += pair.Value.DataSize;
                uniqueIds[i] = pair.Key;
                dataSizes[i] = pair.Value.DataSize;
                i++;
            }

            data = new float[totalDataLength];
            Debug.LogWarning($"ent counts {entities.Count}");
        }

        [Client]
        public float GetUnlocalLerpValue() 
            => Mathf.Clamp((Time.time - clientSyncReceiveTime) * 32f, 0f, maxLerpValue);

        [Server]
        public void DoRollback(int tick, float lerpValue)
        {
            // THIS IS LITERALLY IMPOSSIBLE BEHAVIOUR, RETURN ON SIGHT.
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