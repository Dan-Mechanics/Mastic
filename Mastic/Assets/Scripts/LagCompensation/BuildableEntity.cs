using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for scale, active.
    /// </summary>
    public class BuildableEntity : MonoBehaviour, IEntity
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
        public void EnableCollision(bool active) 
            => this.active = active;

        private void SetAsFrame(Frame frame)
        {
            hitbox.SetActive(frame.active);
            transform.localScale = frame.scale;
        }

        [Server]
        public void RecordFrame(int tick)
        {
            present.SetValues(transform.localScale, active);
            recording[tick % recording.Length] = present;
        }

        [Server]
        public void SetAsTick(int tick) 
            => SetAsFrame(recording[tick % recording.Length]);

        [Server]
        public void ReturnToPresent() 
            => SetAsFrame(present);
        
        private struct Frame
        {
            public Vector3 scale;
            public bool active;

            public void SetValues(Vector3 scale, bool active)
            {
                this.scale = scale;
                this.active = active;
            }
        }
    }
}