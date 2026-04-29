using Mirror;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField, Min(1)] private int standardTickrate = default;
        [SerializeField] private EasyBinding disconnect = default;

        private AdaptiveTickrate adaptiveTickrate;
        private MovementDebugHUD movementDebugHUD;
        private PlayerLook mouseMovement;
        private NetworkManager networkManager;
        private PhysicsMovement physicsMovement;
        private NetworkMovement networkMovement;
        private ICameraInterpolation interpolation;
        private PlayerSetup playerSetup;

        private void Awake()
        {
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            physicsMovement = GetComponent<PhysicsMovement>();
            playerSetup = GetComponent<PlayerSetup>();
            mouseMovement = GetComponent<PlayerLook>();
            movementDebugHUD = GetComponent<MovementDebugHUD>();
            networkMovement = GetComponent<NetworkMovement>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();
            interpolation = FindAnyObjectByType<CameraHandlerExtrapolate>();

            physicsMovement.Setup();
            mouseMovement.Setup();
            adaptiveTickrate.Setup(networkManager, standardTickrate);
            movementDebugHUD.Setup(standardTickrate);
            networkMovement.Setup(standardTickrate, interpolation);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            playerSetup.Setup(true, false);
            networkMovement.OnPendingBufferChanged += adaptiveTickrate.ApplyTimeDilation;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
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
            if (isLocalPlayer && disconnect.WasPressed)
                connectionToServer.Disconnect();
        }
    }
}
