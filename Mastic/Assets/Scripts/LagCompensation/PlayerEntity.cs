using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for player.
    /// </summary>
    public class PlayerEntity : MonoBehaviour, IEntity
    {
        private Frame[] recording;
        private Frame present;

        [Server]
        public void Setup(LagCompensation lagCompensation)
        {
            recording = new Frame[lagCompensation.MaxRecordingLength];
            lagCompensation.Register(this);
        }

        private void SetAsFrame(Frame frame)
        {
            transform.position = frame.pos;
            // ADD ROTATION HERE TOO.
        }

        [Server]
        public void RecordFrame(int tick)
        {
            present.SetValues(transform.position);
            recording[tick % recording.Length] = present;
        }

        [Server]
        public void SetAsTick(int tick)
        {
            SetAsFrame(recording[tick % recording.Length]);
        }

        /// <summary>
        /// Invoke when respawned.
        /// </summary>
        [Server]
        public void RefreshBuffer()
        {
            present.SetValues(transform.position);
            for (int i = 0; i < recording.Length; i++)
            {
                recording[i] = present;
            }
        }

        [Server]
        public void ReturnToPresent() => SetAsFrame(present);

        private struct Frame
        {
            public Vector3 pos;
            // ADD ROTATION HERE TOO.

            public void SetValues(Vector3 pos)
            {
                this.pos = pos;
            }
        }
    }
}