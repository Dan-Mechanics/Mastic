using Mirror;
using UnityEngine;

namespace Mastic
{
    public class TransformEntity : NetworkBehaviour, IEntity
    {
        [SerializeField] private GameObject hitbox = default;
        private Frame[] recording;
        private Frame present;
        private bool hitboxActive;

        public override void OnStartServer()
        {
            base.OnStartServer();
            LagCompensation lagCompensation = FindAnyObjectByType<LagCompensation>();
            recording = new Frame[lagCompensation.MaxRecordingLength];
            EnableHitbox(true);
            lagCompensation.Register(this);
        }

        private void SetAsFrame(Frame frame)
        {
            transform.position = frame.pos;
            hitbox.SetActive(frame.active);
        }

        [Server]
        public void RecordFrame(int tick, int maxRecordingLength)
        {
            recording[tick % recording.Length].
                SetValues(transform.position, hitboxActive);
        }

        [Server]
        public void SavePresent()
        {
            present.SetValues(transform.position, hitboxActive);
        }

        [Server]
        public void SetAsTick(int tick)
        {
            SetAsFrame(recording[tick % recording.Length]);
        }

        [Server]
        public void EnableHitbox(bool active) => hitboxActive = active;

        [Server]
        public void ReturnToPresent()
        {
            SetAsFrame(present);
        }

        private struct Frame
        {
            public Vector3 pos;
            public bool active;

            public void SetValues(Vector3 pos, bool active)
            {
                this.pos = pos;
                this.active = active;
            }
        }
    }
}