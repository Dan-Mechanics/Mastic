using Mirror;
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
        private LagCompensation lagCompensation;
        private PlayerSetup playerSetup;
        private PlayerLook playerLook;
        private Weapon weapon;
        private Transform eyes;
        private Transform cam;

        private void Awake()
        {
            eyes = transform.Find("eyes");
            cam = GameObject.FindWithTag("MainCamera").transform;

            weapon = GetComponent<Weapon>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            physicsMovement = GetComponent<PhysicsMovement>();
            playerSetup = GetComponent<PlayerSetup>();
            playerEntity = GetComponent<PlayerEntity>();
            playerLook = GetComponent<PlayerLook>();
            movementDebugHUD = GetComponent<MovementDebugHUD>();
            networkMovement = GetComponent<NetworkMovement>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            interpolation = cam.GetComponent<ICameraInterpolation>();
        }

        private void Setup()
        {
            weapon.Setup(cam, eyes, interpolation, lagCompensation);
            physicsMovement.Setup();
            playerLook.Setup();
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

            weapon.DoClientUpdate(networkMovement.MovementTick, playerEntity.RollbackTick);
            if (disconnect.WasPressed)
                connectionToServer.Disconnect();
        }
    }
}
