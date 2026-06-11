using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for player.
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        public int RollbackTick { get; private set; }

        [SerializeField] private string playerLayerName = default;
        [SerializeField] private string intangibleLayerName = default;
        [SerializeField] private Transform lookBone = default;
        private MouseLook mouseLook;
        private int intangibleLayer;
        private int playerLayer;
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

        [Server]
        public void RecordFrame(int tick)
        {
            present.SetValues(transform.position, mouseLook.RotationX, mouseLook.RotationY);
            recording[tick % recording.Length] = present;
            RollbackTick = tick;
            RpcSendAuthState(present, tick);
        }

        [Server]
        public void SetAsTick(int tick) 
            => SetAsFrame(recording[tick % recording.Length]);

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcSendAuthState(Frame frame, int tick)
        {
            RollbackTick = tick;
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