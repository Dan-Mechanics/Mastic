using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private EasyBinding disconnect = default;
        [SerializeField, Min(1)] private int standardTickrate = default;

        private AdaptiveTickrate adaptiveTickrate;
        private MovementDebugHUD movementDebugHUD;
        private PlayerEntity playerEntity;
        private NetworkManager networkManager;
        private PhysicsMovement physicsMovement;
        private NetworkMovement networkMovement;
        private ICameraInterpolation interpolation;
        private PlayerSetup playerSetup;
        private IWeapon[] weapons;

        private void Awake()
        {
            weapons = GetComponents<IWeapon>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            physicsMovement = GetComponent<PhysicsMovement>();
            playerSetup = GetComponent<PlayerSetup>();
            playerEntity = GetComponent<PlayerEntity>();
            movementDebugHUD = GetComponent<MovementDebugHUD>();
            networkMovement = GetComponent<NetworkMovement>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();
            interpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
        }

        private void Setup()
        {
            physicsMovement.Setup();
            adaptiveTickrate.Setup(networkManager, standardTickrate);
            movementDebugHUD.Setup(standardTickrate);
            networkMovement.Setup(standardTickrate, interpolation);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Setup();

            playerSetup.Setup(true, false);
            networkMovement.OnPendingBufferChanged += adaptiveTickrate.ApplyTimeDilation;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Setup();

            if (isLocalPlayer)
            {
                Utils.LockMouse();
                playerSetup.Setup(false, true);

                networkMovement.OnReceiveAuthoritativeState += movementDebugHUD.DisplayServerState;
                adaptiveTickrate.OnTickrateChanged += movementDebugHUD.DisplayTickrate;
                networkMovement.OnCurrentTickChanged += movementDebugHUD.DisplayTick;
                networkMovement.OnCheatsChanged += movementDebugHUD.DisplayCheats;
                networkMovement.OnReconsileStateChanged += movementDebugHUD.IndicateReconsile;
            }
            else
            {
                playerSetup.Setup(false, false);
            }
        }

        private void Update()
        {
            if (!isLocalPlayer)
                return;

            for (int i = 0; i < weapons.Length; i++)
            {
                weapons[i].DoLocalTick(networkMovement.MovementTick, playerEntity.RollbackTick);
            }

            if (disconnect.WasPressed)
                connectionToServer.Disconnect();
        }
    }
}
