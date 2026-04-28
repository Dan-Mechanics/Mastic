using Mirror;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField, Min(1)] private int standardTickrate = default;
        [SerializeField] private EasyBinding disconnect = default;

        private MovementTickrate movementTickrate;
        private MovementDebugHUD movementDebugHUD;
        private MouseMovement mouseMovement;
        private NetworkManager networkManager;
        private NetworkMovement networkMovement;
        private ICameraInterpolation interpolation;
        private PlayerSetup playerSetup;

        private void Awake()
        {
            movementTickrate = GetComponent<MovementTickrate>();
            playerSetup = GetComponent<PlayerSetup>();
            mouseMovement = GetComponent<MouseMovement>();
            movementDebugHUD = GetComponent<MovementDebugHUD>();
            networkMovement = GetComponent<NetworkMovement>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();
            interpolation = FindAnyObjectByType<CameraHandlerExtrapolate>();

            mouseMovement.Setup();
            movementTickrate.Setup(networkManager, standardTickrate);
            movementDebugHUD.Setup(standardTickrate);
            networkMovement.Setup(standardTickrate, interpolation);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            playerSetup.Setup(true, false);
            networkMovement.OnBeforeServerTick += movementTickrate.ApplyTimeDilation;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isLocalPlayer)
            {
                Utils.LockMouse();
                playerSetup.Setup(false, true);

                movementTickrate.OnTickrateChanged += movementDebugHUD.DisplayTickrate;
                networkMovement.OnTick += movementDebugHUD.DisplayTick;
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
