using Mirror;
using System;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// Lag compensation for player.
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        public byte DataSize => 5;

        private float time;
        [SerializeField] private string playerLayerName = default;
        [SerializeField] private string intangibleLayerName = default;
        [SerializeField] private Transform lookBone = default;
        [SerializeField] private Transform eyes = default;
        private MouseLook mouseLook;
        private int intangibleLayer;
        private int playerLayer;
        private Frame[] recording;
        private Frame previous;
        private Frame current;
        private float maxLerpValue;

        [Server]
        public void Initialize(int maxRecordingLength)
        {
            recording = new Frame[maxRecordingLength];
        }

        private void Awake()
        {
            maxLerpValue = EasySettings.Current.Get<float>(nameof(maxLerpValue));
            mouseLook = GetComponent<MouseLook>();
            playerLayer = LayerMask.NameToLayer(playerLayerName);
            intangibleLayer = LayerMask.NameToLayer(intangibleLayerName);
            EnableHitbox(true);
        }

        private void Start()
        {
            FindAnyObjectByType<EntityManager>().Register(netId, this);
        }

        private void Update()
        {
            // ONLY IF UNLOCAL CLIENT.
            if (isServer || isLocalPlayer)
                return;

            float lerpValue = Mathf.Clamp(Time.time - time, 0f, maxLerpValue);
            Frame lerped = Frame.LerpUnclamped(previous, current, lerpValue);
            transform.SetPositionAndRotation(lerped.position, lerped.rot);
            eyes.localRotation = lerped.eyesLocalRot;
            lookBone.rotation = eyes.rotation;
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
            current = new Frame()
            {
                position = transform.position,
                eyesLocalRot = Quaternion.AngleAxis(mouseLook.RotationX, Vector3.right),
                rot = Quaternion.AngleAxis(mouseLook.RotationY, Vector3.up)
            };
            recording[tick % recording.Length] = current;
        }

        [Server]
        public void SetAsTick(int tick) 
            => SetAsFrame(recording[tick % recording.Length]);

        /// <summary>
        /// This is called when player respawns.
        /// </summary>
        [Server]
        public void RefreshRollbackBuffer()
        {
            current = new Frame()
            {
                position = transform.position,
                eyesLocalRot = Quaternion.AngleAxis(mouseLook.RotationX, Vector3.right),
                rot = Quaternion.AngleAxis(mouseLook.RotationY, Vector3.up)
            };
            for (int i = 0; i < recording.Length; i++)
            {
                recording[i] = current;
            }
        }

        [Server]
        public void ReturnToPresent() 
            => SetAsFrame(current);

        public void EnableHitbox(bool value) 
            => gameObject.layer = value ? playerLayer : intangibleLayer;

        [Client]
        public void ReadFromData(int index, float[] data, float time)
        {
            this.time = time;
            previous = current;
            current = new Frame()
            {
                position = new Vector3(data[index], data[index + 1], data[index + 2]),
                eyesLocalRot = Quaternion.AngleAxis(data[index + 3], Vector3.right),
                rot = Quaternion.AngleAxis(data[index + 4], Vector3.up)
            };
        }

        [Server]
        public void WriteToData(int index, float[] data)
        {
            data[index] = transform.position.x;
            data[index + 1] = transform.position.y;
            data[index + 2] = transform.position.z;
            data[index + 3] = mouseLook.RotationX;
            data[index + 4] = mouseLook.RotationY;
        }

        private struct Frame
        {
            public Vector3 position;
            public Quaternion rot;
            public Quaternion eyesLocalRot;

            public static Frame LerpUnclamped(Frame a, Frame b, float t)
            {
                return new Frame()
                {
                    position = Vector3.LerpUnclamped(a.position, b.position, t),
                    rot = Quaternion.LerpUnclamped(a.rot, b.rot, t),
                    eyesLocalRot = Quaternion.LerpUnclamped(a.eyesLocalRot, b.eyesLocalRot, t),
                };
            }
        }
    }
}