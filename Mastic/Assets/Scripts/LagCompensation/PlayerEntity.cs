using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for player.
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        /// <summary>
        /// I am here assuming that the tick on the local
        /// player is the same as the tick that the player is shooting at.
        /// </summary>
        public int RollbackTick => tick;
        
        [SerializeField] private PlayerLook playerLook = default;
        [SerializeField] private string playerLayerName = default;
        [SerializeField] private string intangibleLayerName = default;
        private int intangibleLayer;
        private int playerLayer;
        private Frame[] recording;
        private Frame present;
        private int tick;

        [Server]
        public void Setup(LagCompensation lagCompensation)
        {
            recording = new Frame[lagCompensation.MaxRecordingLength];
            lagCompensation.Register(this);
            playerLayer = LayerMask.NameToLayer(playerLayerName);
            intangibleLayer = LayerMask.NameToLayer(intangibleLayerName);
            EnableHitbox(true);
        }

        private void SetAsFrame(Frame frame)
        {
            transform.position = frame.position;
            playerLook.SetAsRotation(frame.xRotation, frame.yRotation);
            
            // THIS IS WHERE LOOKBONE SHOULD GO.
            // INCLUDING HITBOX IF THAT IS NOT ATTACHED TO LOOKBONE.
        }

        [Server]
        public void RecordFrame(int tick)
        {
            present.SetValues(transform.position, playerLook.RotationX, playerLook.RotationY);
            recording[tick % recording.Length] = present;
            RpcSendAuthState(present, tick);
        }

        [Server]
        public void SetAsTick(int tick) => SetAsFrame(recording[tick % recording.Length]);

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcSendAuthState(Frame frame, int tick)
        {
            if (isLocalPlayer)
            {
                this.tick = tick;
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
            present.SetValues(transform.position, playerLook.RotationX, playerLook.RotationY);
            for (int i = 0; i < recording.Length; i++)
            {
                recording[i] = present;
            }
        }

        [Server]
        public void ReturnToPresent() => SetAsFrame(present);

        [Server]
        public void EnableHitbox(bool value)
        {
            if (value)
            {
                gameObject.layer = playerLayer;
            }
            else
            {
                gameObject.layer = intangibleLayer;
            }
        }

        private struct Frame
        {
            public Vector3 position;
            public float xRotation;
            public float yRotation;

            public void SetValues(Vector3 position, float xRotation, float yRotation)
            {
                this.position = position;
                this.xRotation = xRotation;
                this.yRotation = yRotation;
            }
        }
    }
}