using Mirror;
using UnityEngine;

namespace Mastic
{
    public class PlayerEntity : NetworkBehaviour, IEntity
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

            // FOR THE TIME BEING, THE PLAYER WILL SPAWN WITH HITBOX ENABLED.
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
            present.SetValues(transform.position, hitboxActive);
            recording[tick % recording.Length] = present;
        }

        [Server]
        public void SetAsTick(int tick)
        {
            SetAsFrame(recording[tick % recording.Length]);
        }

        [Server]
        public void EnableHitbox(bool active)
        {
            hitboxActive = active;
            hitbox.SetActive(hitboxActive);
        }

        [Server]
        public void ReturnToPresent() => SetAsFrame(present);

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