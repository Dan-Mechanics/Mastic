using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class TransformEntity : NetworkBehaviour, IEntity
    {
        [SerializeField] private GameObject hitbox = default;
        [SerializeField] private bool includePosition = default;
        [SerializeField] private bool includeRotation = default;
        [SerializeField] private bool includeScale = default;
        private readonly List<Frame> recording = new List<Frame>();
        private Frame present;
        private bool active;

        public override void OnStartServer()
        {
            base.OnStartServer();
            FindAnyObjectByType<LagCompensation>().Register(this);
        }

        public void Destroy()
        {

        }

        private void SetAsFrame(Frame frame)
        {
            if (includePosition)
                transform.localPosition = frame.pos;

            if (includeRotation)
                transform.localRotation = frame.rot;

            if (includeScale)
                transform.localScale = frame.scale;
        }

        [Server]
        public void RecordFrame(int tick, int maxRecordingLength)
        {
            present.tick = tick;
            if (includePosition)
                present.pos = transform.localPosition;

            if (includeRotation)
                present.rot = transform.localRotation;

            if (includeScale)
                present.scale = transform.localScale;

            recording.Add(present);
            while (recording.Count > maxRecordingLength)
            {
                recording.RemoveAt(0);
            }
        }

        [Server]
        public void SetAsTick(int tick)
        {
            // !PERFORMANCE,
            // you can have some basic checks here and limit searching.
            hitbox.SetActive(false);
            for (int i = recording.Count - 1; i >= 0; i--)
            {
                if (recording[i].tick != tick)
                    continue;

                hitbox.SetActive(true);
                SetAsFrame(recording[i]);
            }
        }

        [Server]
        public void ReturnToPresent() => SetAsFrame(present);

        private struct Frame
        {
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 scale;
            public int tick;
        }
    }
}