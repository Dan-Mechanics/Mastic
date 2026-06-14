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
        private Frame previous = Frame.Default;
        private Frame current = Frame.Default;
        private int uniqueId = -1;
        private float time;

        /*private void Hook(int _, int uniqueId)
        {
            EntityManager.Current.Register(uniqueId, this);
        }
*/
        private void Awake()
        {
            easySettings = EasySettings.Current;
            entityManager = EntityManager.Current;
            Debug.LogWarning("playrer");
            maxLerpValue = easySettings.Get<float>(nameof(maxLerpValue));
            mouseLook = GetComponent<MouseLook>();
            playerLayer = LayerMask.NameToLayer(playerLayerName);
            intangibleLayer = LayerMask.NameToLayer(intangibleLayerName);
            EnableHitbox(true);
            SavePresent();
            previous = current;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            recording = new Frame[entityManager.MaxRecordingLength];
            uniqueId = entityManager.FetchUniqueId();
            entityManager.Register(uniqueId, this);
           
            Invoke(nameof(SyncUniqueId), easySettings.Get<float>("respawndelay"));
        }

        private void SyncUniqueId()
            => RpcSyncUniqueId(uniqueId);

        [ClientRpc]
        private void RpcSyncUniqueId(int uniqueId)
        {
            this.uniqueId = uniqueId;
            EntityManager.Current.Register(uniqueId, this);
        }

        private void Update()
        {
            // ONLY IF UNLOCAL CLIENT.
            if (isServer || isLocalPlayer || uniqueId < 0)
                return;

            float lerpValue = Mathf.Clamp((Time.time - time) * 32f, 0f, maxLerpValue);
            Debug.Log(lerpValue);
            Frame lerped = Frame.LerpUnclamped(previous, current, lerpValue);
            SetAsFrame(lerped);
        }

        private void SetAsFrame(Frame frame)
        {
            transform.SetPositionAndRotation(frame.pos, frame.rot);
            eyes.localRotation = frame.localEyesRot;
            lookBone.rotation = eyes.rotation;
            lookBone.localRotation = Quaternion.Euler(0f, 0f, -eyes.localEulerAngles.x);
        }

        [Server]
        public void RecordFrame(int tick)
        {
            SavePresent();
            recording[tick % recording.Length] = current;
        }

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

     //   [Server]
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

      //  [Server]
        public void SavePresent()
        {
            current = new Frame()
            {
                pos = transform.position,
                rot = transform.rotation,
                localEyesRot = eyes.localRotation
            };
        }

        [Server]
        public void DoRollback(int prevTick, int currTick, float lerpValue)
        {
            Frame frame = Frame.LerpUnclamped(recording[prevTick % recording.Length], recording[currTick % recording.Length], lerpValue);
            SetAsFrame(frame);
        }

        /// <summary>
        /// Add: pos diff > X == teleport.
        /// </summary>
        private struct Frame
        {
            public Vector3 pos;
            public Quaternion rot;
            public Quaternion localEyesRot;

            public static Frame Default => new Frame()
            {
                pos = Vector3.zero,
                rot = Quaternion.identity,
                localEyesRot = Quaternion.identity
            };

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