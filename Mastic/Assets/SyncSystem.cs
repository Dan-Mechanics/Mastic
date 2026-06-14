using Mirror;
using Newtonsoft.Json.Linq;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SyncSystem
{
    public interface ISyncEntity
    {
        byte DataSize { get; }
        void ReadFromData(int index, float[] data, float time);
        void WriteToData(int index, float[] data);
        void RecordFrame(int tick);
        void SetAsTick(int tick, float lerpValue);
        void ReturnToPresent();
    }


    /// <summary>
    /// Requirements:
    /// 1. world data must be sent reliable and all unpacked at the same time.
    /// 2. flexibility for different types of entities.
    /// 3. 32 ticks per second with lerp.
    /// 4. decent performance.
    /// </summary>
    public class SyncSystem : NetworkBehaviour
    {
        public int Tick { get; private set; }
        public float CurrentTime { get; private set; }

        private readonly Dictionary<uint, ISyncEntity> entities = new Dictionary<uint, ISyncEntity>();
        private int prevCount = -1;
        private byte[] dataSizes;
        private byte[] netIds;
        private float[] data;

        [Server]
        public void DoTick()
        {
            CleanNullEntities();
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
            CleanNullEntities();

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

        private void CleanNullEntities()
        {
            foreach (var pair in entities)
            {
                if (pair.Value == null)
                    entities.Remove(pair.Key);
            }
        }

        public void Register(uint netId, ISyncEntity entity) 
            => entities[netId] = entity;

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
    }
}
