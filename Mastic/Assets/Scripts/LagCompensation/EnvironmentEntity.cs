using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for position and rotation.
    /// </summary>
    public class EnvironmentEntity : MonoBehaviour, IEntity
    {
        private Frame[] recording;
        private Frame present;

        [Server]
        public void Initialize(LagCompensation lagCompensation)
        {
            recording = new Frame[lagCompensation.MaxRecordingLength];
            lagCompensation.Register(this);
        }

        private void SetAsFrame(Frame frame) => transform.SetPositionAndRotation(frame.pos, frame.rot);

        [Server]
        public void RecordFrame(int tick)
        {
            present.SetValues(transform.position, transform.rotation);
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

            public void SetValues(Vector3 pos, Quaternion rot)
            {
                this.pos = pos;
                this.rot = rot;
            }
        }
    }
}