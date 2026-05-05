using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private EasyBinding disconnect = default;
        [SerializeField] private string playerName = default;
        [SerializeField, Min(1)] private int standardTickrate = default;
        [SerializeField] private List<Object> localRemove = default;
        [SerializeField] private List<Object> unlocalRemove = default;
        [SerializeField] private List<Object> serverRemove = default;

        private MouseLook mouseLook;
        private AdaptiveTickrate adaptiveTickrate;
        private DebugHUD debugHUD;
        private PlayerEntity entity;
        private SimpleNetworkManager simpleNetworkManager;
        private PhysicsMovement physicsMovement;
        private NetworkMovement networkMovement;
        private LagCompensation lagCompensation;
        private CameraHandlerExtrapolate cameraHandlerExtrapolate;
        private IAttack[] attacks;

        private void Awake()
        {
            GetReferences();
            Initialize();
        }

        private void GetReferences()
        {
            attacks = GetComponents<IAttack>();
            mouseLook = GetComponent<MouseLook>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            physicsMovement = GetComponent<PhysicsMovement>();
            entity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            debugHUD = GetComponent<DebugHUD>();
            networkMovement = GetComponent<NetworkMovement>();
            simpleNetworkManager = FindAnyObjectByType<SimpleNetworkManager>();
            cameraHandlerExtrapolate = GameObject.FindWithTag("MainCamera").GetComponent<CameraHandlerExtrapolate>();
        }

        /// <summary>
        /// For server, local and unlocal client.
        /// </summary>
        private void Initialize()
        {
            adaptiveTickrate.Initialize(simpleNetworkManager, standardTickrate);
            debugHUD.Initialize(standardTickrate);
            mouseLook.Initialize();
            entity.Initialize(lagCompensation);
            physicsMovement.Initialize();
            networkMovement.Initialize(standardTickrate, cameraHandlerExtrapolate, physicsMovement);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            gameObject.name = $"{playerName} | server";
            serverRemove.ForEach(x => Destroy(x));
            print($"{gameObject.name}: setup completed".ToUpperInvariant());
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isLocalPlayer)
            {
                Utils.LockMouse();
                gameObject.name = $"{playerName} | local client";
                localRemove.ForEach(x => Destroy(x));

                networkMovement.OnDisplayServerState += debugHUD.DisplayServerState;
                adaptiveTickrate.OnDisplayTickrate += debugHUD.DisplayTickrate;
                networkMovement.OnDisplayTick += debugHUD.DisplayTick;
                networkMovement.OnDisplayCheats += debugHUD.DisplayCheats;
                networkMovement.OnDisplayReconsile += debugHUD.DisplayReconsile;
            }
            else
            {
                gameObject.name = $"{playerName} | unlocal client";
                unlocalRemove.ForEach(x => Destroy(x));
            }

            print($"{gameObject.name}: setup completed".ToUpperInvariant());
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            for (int i = 0; i < attacks.Length; i++)
            {
                attacks[i].DoLocalTick(networkMovement.MovementTick, entity.RollbackTick);
            }

            if (disconnect.WasPressed)
                connectionToServer.Disconnect();
        }
    }
}
