using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for player.
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        [SerializeField] private string playerLayerName = default;
        [SerializeField] private string intangibleLayerName = default;
        [SerializeField] private Transform lookBone = default;
        private MouseLook mouseLook;
        private int intangibleLayer;
        private int playerLayer;
        private int displayedTick;
        private Frame[] recording;
        private Frame present;

        [Server]
        public void Initialize(LagCompensation lagCompensation)
        {
            recording = new Frame[lagCompensation.MaxRecordingLength];
            lagCompensation.Register(this);
        }

        private void Awake()
        {
            mouseLook = GetComponent<MouseLook>();
            playerLayer = LayerMask.NameToLayer(playerLayerName);
            intangibleLayer = LayerMask.NameToLayer(intangibleLayerName);
            EnableHitbox(true);
        }

        private void SetAsFrame(Frame frame)
        {
            transform.position = frame.position;
            mouseLook.SetRotationTemporarily(frame.xRotation, frame.yRotation);

            // THIS IS WHERE LOOKBONE SHOULD GO.
            // INCLUDING HITBOX IF THAT IS NOT ATTACHED TO LOOKBONE.
            if (!isLocalPlayer)
                lookBone.localRotation = Quaternion.Euler(0f, 0f, -frame.xRotation);
        }

        public int GetDisplayedTick()
            => displayedTick;

        [Server]
        public void RecordFrame(int tick)
        {
            present.SetValues(transform.position, mouseLook.RotationX, mouseLook.RotationY);
            recording[tick % recording.Length] = present;
            displayedTick = tick;
            RpcSendAuthState(present, tick);
        }

        [Server]
        public void SetAsTick(int tick) 
            => SetAsFrame(recording[tick % recording.Length]);

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcSendAuthState(Frame frame, int tick)
        {
            displayedTick = tick;
            if (!isLocalPlayer)
                SetAsFrame(frame);
        }

        /// <summary>
        /// This is called when player respawns.
        /// </summary>
        [Server]
        public void RefreshRollbackBuffer()
        {
            present.SetValues(transform.position, mouseLook.RotationX, mouseLook.RotationY);
            for (int i = 0; i < recording.Length; i++)
            {
                recording[i] = present;
            }
        }

        [Server]
        public void ReturnToPresent() 
            => SetAsFrame(present);

        public void EnableHitbox(bool value) 
            => gameObject.layer = value ? playerLayer : intangibleLayer;

        /// <summary>
        /// Player could have active bool in the future.
        /// Right now this is not relevant.
        /// </summary>
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