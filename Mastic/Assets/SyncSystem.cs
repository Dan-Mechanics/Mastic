using Mirror;
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
    }

    public class ExampleEntity : NetworkBehaviour, ISyncEntity
    {
        public byte DataSize => 9;

        private Vector3 prevPosition;
        private Vector3 currPosition;
        private Vector3 prevRotation;
        private Vector3 currRotation;
        private Vector3 prevScale;
        private Vector3 currScale;
        private float maxLerpValue;
        private float time;

        private void Awake()
        {
            maxLerpValue = Mastic.EasySettings.Current.Get<float>(nameof(maxLerpValue));
            prevPosition = transform.localPosition;
            currPosition = prevPosition;
            prevRotation = transform.localEulerAngles;
            currRotation = prevRotation;
            prevScale = transform.localScale;
            currScale = prevScale;
        }

        private void Update()
        {
            float value = Mathf.Clamp(Time.time - time, 0f, maxLerpValue);
            SetAsLerpValue(value);
        }

        private void SetAsLerpValue(float value)
        {
            transform.localPosition = Vector3.LerpUnclamped(prevPosition, currPosition, value);
            transform.localEulerAngles = Vector3.LerpUnclamped(prevRotation, currRotation, value);
            transform.localScale = Vector3.LerpUnclamped(prevScale, currScale, value);
        }

        public void ReadFromData(int index, float[] data, float time)
        {
            this.time = time;
            prevPosition = currPosition;
            currPosition = new Vector3(data[index], data[index + 1], data[index + 2]);

            prevRotation = currRotation;
            currRotation = new Vector3(data[index + 3], data[index + 4], data[index + 5]);

            prevScale = currScale;
            currScale = new Vector3(data[index + 6], data[index + 7], data[index + 8]);
        }

        public void WriteToData(int index, float[] data)
        {
            data[index] = transform.localPosition.x;
            data[index + 1] = transform.localPosition.y;
            data[index + 2] = transform.localPosition.z;
            data[index + 3] = transform.localEulerAngles.x;
            data[index + 4] = transform.localEulerAngles.y;
            data[index + 5] = transform.localEulerAngles.z;
            data[index + 6] = transform.localRotation.x;
            data[index + 7] = transform.localRotation.y;
            data[index + 8] = transform.localRotation.z;
        }
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
