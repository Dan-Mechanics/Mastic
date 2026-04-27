using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// It would be smart to split these into separate channels in the future.
    /// That does mean that LagCompensation needs to keep track of more interfaces...
    /// </summary>
    public class TransformEntity : NetworkBehaviour, IEntity
    {
        [SerializeField] private GameObject hitbox = default;
        [SerializeField] private bool includePosition = default;
        [SerializeField] private bool includeRotation = default;
        [SerializeField] private bool includeScale = default;
        private Frame[] recording;
        private Frame present;
        private bool active;

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
        public void EnableCollision(bool active) => this.active = active;

        private void SetAsFrame(Frame frame)
        {
            hitbox.SetActive(frame.active);
            transform.SetPositionAndRotation(frame.pos, frame.rot);
            transform.localScale = frame.scale;
        }

        [Server]
        public void RecordFrame(int tick, int maxRecordingLength)
        {
            present.SetValues(
                transform.position,
                transform.rotation,
                transform.localScale,
                active);

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

            public void SetValues(Vector3 pos, Quaternion rot, Vector3 scale, bool active)
            {
                this.pos = pos;
                this.rot = rot;
                this.scale = scale;
                this.active = active;
            }
        }
    }
}