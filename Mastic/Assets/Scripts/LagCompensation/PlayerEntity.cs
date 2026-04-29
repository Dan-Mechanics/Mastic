using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for player.
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        private Frame[] recording;
        private Frame present;
        private int currentTick;

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
            RpcSendAuthState(present, tick);
        }

        [Server]
        public void SetAsTick(int tick)
        {
            SetAsFrame(recording[tick % recording.Length]);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcSendAuthState(Frame frame, int tick)
        {
            if (isLocalPlayer)
            {
                currentTick = tick;
            }
            else
            {
                SetAsFrame(frame);
            }
        }

        /// <summary>
        /// Invoke when (re)spawned.
        /// This is important because otherwise we can rollback
        /// the player to a time before he was dead and then he will
        /// get shot in the spawn room.
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

        [Client]
        public int GetCurrentTick() => currentTick;

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