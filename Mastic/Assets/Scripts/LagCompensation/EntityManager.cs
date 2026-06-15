using Mirror;
using Mirror.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
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
        private readonly Queue<Packet> buffer = new Queue<Packet>();
        private float maxLerpValue;
        private byte[] dataSizes;
        private int currentTick;
        private int oldestTick;
        private int[] uniqueIds;
        private int prevCount = -1;
        private int nextUniqueId;
        private float[] data;
        private float time;
        private float interval = 1 / 32f;
        private float next;

        private void Awake() 
            => maxLerpValue = EasySettings.Current.Get<float>(nameof(maxLerpValue));

        /// <summary>
        /// Called every 32th of a second, not more than once per frame.
        /// </summary>
        [Server]
        public void DoServerTick()
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

        private void Update()
        {
            if (!isClient)
                return;

            if (buffer.Count > 3 && Time.time > next)
            {
                next = Time.time + interval;
                var packet = buffer.Dequeue();
                var _tick = packet.tick;
                var _uniqueIds = packet.uniqueIds;
                var _data = packet.data;
                var _dataSizes = packet.dataSizes;

                RemoveNullEntities();
                RollbackTick = _tick;
                int index = 0;
                for (int i = 0; i < _uniqueIds.Length; i++)
                {
                    if (entities.ContainsKey(_uniqueIds[i]))
                        entities[_uniqueIds[i]].ReadFromDataReal(index, _data, time);

                    index += _dataSizes[i];
                }
            }
        }

        [ClientRpc]
        private void RpcSync(int tick, int[] uniqueIds, byte[] dataSizes, float[] data)
        {
            buffer.Enqueue(new Packet()
            {
                tick = tick,
                uniqueIds = uniqueIds,
                data = data,
                dataSizes = dataSizes
            });

              int tempIndex = 0;
              time = Time.time;
              for (int i = 0; i < uniqueIds.Length; i++)
              {
                  if (entities.ContainsKey(uniqueIds[i]))
                      entities[uniqueIds[i]].ReadFromData(tempIndex, data, time);
            
                  tempIndex += dataSizes[i];
              }

            /*if (buffer.Count > 4)
            {
                var packet = buffer.Dequeue();
                int _tick = packet.tick;
                int[] _uniqueIds = packet.uniqueIds;
                float[] _data = packet.data;
                byte[] _dataSizes = packet.dataSizes;

                RemoveNullEntities();
                RollbackTick = _tick;
                int index = 0;
                for (int i = 0; i < _uniqueIds.Length; i++)
                {
                    if (entities.ContainsKey(_uniqueIds[i]))
                        entities[_uniqueIds[i]].ReadFromDataReal(index, _data, time);

                    index += _dataSizes[i];
                }
            }*/
        }

        private struct Packet
        {
            public int tick;
            public int[] uniqueIds;
            public float[] data;
            public byte[] dataSizes;
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
            => Mathf.Clamp((Time.time - time) * 32f, 0f, maxLerpValue);

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