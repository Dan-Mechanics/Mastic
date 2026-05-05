using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for position, rotation, scale and enabled.
    /// </summary>
    public class TransformEntity : MonoBehaviour, IEntity
    {
        [SerializeField] private GameObject hitbox = default;
        private Frame[] recording;
        private Frame present;
        private bool active;

        [Server]
        public void Initialize(LagCompensation lagCompensation)
        {
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
        public void EnableCollision(bool active) => this.active = active;

        private void SetAsFrame(Frame frame)
        {
            hitbox.SetActive(frame.active);
            if (!frame.active)
                return;

            transform.SetLocalPositionAndRotation(frame.pos, frame.rot);
            transform.localScale = frame.scale;
        }

        [Server]
        public void RecordFrame(int tick)
        {
            present.active = active;
            if (present.active)
                present.SetValues(transform.localPosition, transform.localRotation, transform.localScale);

            recording[tick % recording.Length] = present;
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

            public void SetValues(Vector3 pos, Quaternion rot, Vector3 scale)
            {
                this.pos = pos;
                this.rot = rot;
                this.scale = scale;
            }
        }
    }
}