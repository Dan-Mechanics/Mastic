using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// It would be smart to split these into separate channels in the future.
    /// </summary>
    public class TransformEntity : NetworkBehaviour, IEntity
    {
        [SerializeField] private GameObject hitbox = default;
        [SerializeField] private bool includePosition = default;
        [SerializeField] private bool includeRotation = default;
        [SerializeField] private bool includeScale = default;
        private bool currentlyActive;
        private Frame[] recording;
        private Frame present;

        public override void OnStartServer()
        {
            base.OnStartServer();
            LagCompensation lagCompensation = new LagCompensation();    
            recording = new Frame[lagCompensation.MaxRecordingLength];
            EnableCollision(true);
            lagCompensation.Register(this);
        }

        [Server]
        public void Deregister(float fullyGoneTime)
        {
            EnableCollision(false);
            Destroy(gameObject, fullyGoneTime);
        }

        [Server]
        public void EnableCollision(bool active) => currentlyActive = active;

        private void SetAsFrame(Frame frame)
        {
            hitbox.SetActive(frame.active);
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
            present.active = currentlyActive;
            if (includePosition)
                present.pos = transform.localPosition;

            if (includeRotation)
                present.rot = transform.localRotation;

            if (includeScale)
                present.scale = transform.localScale;

            recording[tick % maxRecordingLength] = present;
        }

        [Server]
        public void SetAsTick(int tick) => SetAsFrame(recording[tick % recording.Length]);

        [Server]
        public void ReturnToPresent() => SetAsFrame(present);
        
        private struct Frame
        {
            public Vector3 pos;
            public Quaternion rot;
            public Vector3 scale;
            public bool active;
        }
    }
}