using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// TODO: I AM STILL WORKING ON THE DEPENDENCY STRUCUTRE HERE...
    /// if you are making a buildable, then you would send the unique id
    /// in the same Rpc message is that the buildable was made itself...
    /// i think that makes sense yeah. Server --> clients RpcSpawnGlacier ( int uniqueId)
    /// </summary>
    public class PlayerEntity : NetworkBehaviour, IEntity
    {
        public byte DataSize => 5;

        [SerializeField] private string playerLayerName = default;
        [SerializeField] private string intangibleLayerName = default;
        [SerializeField] private Transform lookBone = default;
        [SerializeField] private Transform eyes = default;
        [SyncVar(hook = nameof(OnNewUniqueId))] private int uniqueId = -1;
        private EasySettings easySettings;
        private EntityManager entityManager;
        private MouseLook mouseLook;
        private int intangibleLayer;
        private float maxLerpValue;
        private int playerLayer;
      //  private float time;
        private Frame[] recording;
        private Frame previous = Frame.Default;
        private Frame current = Frame.Default;
    //    private readonly Queue<Frame> buffer = new Queue<Frame>();   

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
            Invoke(nameof(RegisterPlayerDelayed), easySettings.Get<float>(nameof(RegisterPlayerDelayed)));
        }

        private void RegisterPlayerDelayed()
        {
            uniqueId = entityManager.FetchUniqueId();
            entityManager.Register(uniqueId, this);
        }

        private void Update()
        {
            // ONLY IF UNLOCAL CLIENT.
            if (isServer || isLocalPlayer || uniqueId < 0)
                return;

            // use time float here for better decoupling.
            float lerpValue = entityManager.GetUnlocalLerpValue();
           // Debug.Log(lerpValue);
            Frame lerped = Frame.Lerp(previous, current, lerpValue);
            SetAsFrame(lerped);
        }

        private void OnNewUniqueId(int _, int uniqueId)
            => EntityManager.Current.Register(uniqueId, this);

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
        public void ReadFromDataReal(int index, float[] data, float time)
        {
            MakeCleanSlate();
            current = new Frame()
            {
                pos = new Vector3(data[index], data[index + 1], data[index + 2]),
                localEyesRot = Quaternion.AngleAxis(data[index + 3], Vector3.right),
                rot = Quaternion.AngleAxis(data[index + 4], Vector3.up)
            };
        }

        [Client]
        public void MakeCleanSlate()
            => previous = current;

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
        public void DoRollback(int prevTick, int currTick, float lerpValue, bool isCleanSlate)
        {
            if (isCleanSlate)
                prevTick = currTick;

            Frame frame = Frame.Lerp(recording[prevTick % recording.Length], recording[currTick % recording.Length], lerpValue);
            SetAsFrame(frame);
        }

        /// <summary>
        /// Add: pos diff > some_value == teleport.
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

            public static Frame Lerp(Frame a, Frame b, float t)
            {
                return new Frame()
                {
                    pos = Vector3.Lerp(a.pos, b.pos, t),
                    rot = Quaternion.Lerp(a.rot, b.rot, t),
                    localEyesRot = Quaternion.Lerp(a.localEyesRot, b.localEyesRot, t),
                };
            }
        }
    }
}