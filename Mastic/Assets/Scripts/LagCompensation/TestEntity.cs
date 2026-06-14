using Mirror;
using UnityEngine;

namespace Mastic
{
    public class TestEntity : NetworkBehaviour, IEntity
    {
        public byte DataSize => 3;

        private float time;
        private Vector3[] recording;
        private Vector3 previous;
        private Vector3 current;
        private EasySettings easySettings;
        private EntityManager entityManager;
        private float maxLerpValue;

        private void Awake()
        {
            easySettings = EasySettings.Current;
            entityManager = EntityManager.Current;
            entityManager.Register(netId, this);
            maxLerpValue = easySettings.Get<float>(nameof(maxLerpValue));
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            recording = new Vector3[entityManager.MaxRecordingLength];
        }

        private void Update()
        {
            // ONLY IF UNLOCAL CLIENT.
            if (isServer || isLocalPlayer)
                return;

            float lerpValue = Mathf.Clamp(Time.time - time, 0f, maxLerpValue);
            transform.position = Vector3.LerpUnclamped(previous, current, lerpValue);
        }

        [Server]
        public void RecordFrame(int tick)
        {
            SavePresent();
            recording[tick % recording.Length] = current;
        }

        private void SetAsFrame(Vector3 frame)
        {
            transform.position = frame;
        }

        [Server]
        public void DoRollback(int prevTick, int currTick, float lerpValue)
        {
            Vector3 frame = Vector3.LerpUnclamped(recording[prevTick % recording.Length], recording[currTick % recording.Length], lerpValue);
            SetAsFrame(frame);
        }

        [Server]
        public void ReturnToPresent()
        {
            SetAsFrame(current);
        }

        [Server]
        public void SavePresent()
        {
            current = transform.position;
        }

        [Client]
        public void ReadFromData(int index, float[] data, float time)
        {
            this.time = time;
            previous = current;
            current = new Vector3(data[index], data[index + 1], data[index + 2]);
        }

        [Server]
        public void WriteToData(int index, float[] data)
        {
            // point of taste if u wanna use current from recordframe()
            // here. i know you cant carry over all things, in the name of performance,
            // though you may want to. so you will need to fetch some stuff again.
            data[index] =     current.x;
            data[index + 1] = current.y;
            data[index + 2] = current.z;
        }
    }
}