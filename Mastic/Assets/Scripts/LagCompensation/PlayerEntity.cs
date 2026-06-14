using Mirror;
using System;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// TODO: I AM STILL WORKING ON THE DEPENDENCY STRUCUTRE HERE...
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        public byte DataSize => 5;

        [SerializeField] private string playerLayerName = default;
        [SerializeField] private string intangibleLayerName = default;
        [SerializeField] private Transform lookBone = default;
        [SerializeField] private Transform eyes = default;
        private EasySettings easySettings;
        private EntityManager entityManager;
        private MouseLook mouseLook;
        private int intangibleLayer;
        private float maxLerpValue;
        private int playerLayer;
        private Frame[] recording;
        private Frame previous;
        private Frame current;
        private float time;

        private void Awake()
        {
            easySettings = EasySettings.Current;
            entityManager = EntityManager.Current;
            entityManager.Register(netId, this);
            maxLerpValue = easySettings.Get<float>(nameof(maxLerpValue));
            mouseLook = GetComponent<MouseLook>();
            playerLayer = LayerMask.NameToLayer(playerLayerName);
            intangibleLayer = LayerMask.NameToLayer(intangibleLayerName);
            EnableHitbox(true);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            recording = new Frame[entityManager.MaxRecordingLength];
        }

        private void Update()
        {
            // ONLY IF UNLOCAL CLIENT.
            if (isServer || isLocalPlayer)
                return;

            float lerpValue = Mathf.Clamp(Time.time - time, 0f, maxLerpValue);
            Frame lerped = Frame.LerpUnclamped(previous, current, lerpValue);
            SetAsFrame(lerped);
        }

        private void SetAsFrame(Frame frame)
        {
            transform.SetPositionAndRotation(frame.pos, frame.rot);
            eyes.localRotation = frame.localEyesRot;
            lookBone.rotation = eyes.rotation;
        }

        [Server]
        public void RecordFrame(int tick)
        {
            SavePresent();
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
            SavePresent();
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
                pos = new Vector3(data[index], data[index + 1], data[index + 2]),
                localEyesRot = Quaternion.AngleAxis(data[index + 3], Vector3.right),
                rot = Quaternion.AngleAxis(data[index + 4], Vector3.up)
            };
        }

        [Server]
        public void WriteToData(int index, float[] data)
        {
            data[index]     = current.pos.x;
            data[index + 1] = current.pos.y;
            data[index + 2] = current.pos.z;       // WE DON'T USE CURRENT HERE BECAUSE
            data[index + 3] = mouseLook.RotationX; // THEN I WOULD HAVE TO DO CRAZY CONVERSIONS.
            data[index + 4] = mouseLook.RotationY; // TLDR: FOR PERFORMANCE. 
        }

        public void SavePresent()
        {
            current = new Frame()
            {
                pos = transform.position,
                localEyesRot = Quaternion.AngleAxis(mouseLook.RotationX, Vector3.right),
                rot = Quaternion.AngleAxis(mouseLook.RotationY, Vector3.up)
            };
        }

        public void DoRollback(int prevTick, int currTick, float lerpValue)
        {
            Frame frame = Frame.LerpUnclamped(recording[prevTick % recording.Length], recording[currTick % recording.Length], lerpValue);
            SetAsFrame(frame);
        }

        private struct Frame
        {
            public Vector3 pos;
            public Quaternion rot;
            public Quaternion localEyesRot;

            public static Frame LerpUnclamped(Frame a, Frame b, float t)
            {
                return new Frame()
                {
                    pos = Vector3.LerpUnclamped(a.pos, b.pos, t),
                    rot = Quaternion.LerpUnclamped(a.rot, b.rot, t),
                    localEyesRot = Quaternion.LerpUnclamped(a.localEyesRot, b.localEyesRot, t),
                };
            }
        }
    }
}