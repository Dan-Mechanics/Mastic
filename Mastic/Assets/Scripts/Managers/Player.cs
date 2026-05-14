using Mirror;
using System.Collections.Generic;
using System.Linq;
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

        private IAttackAbility[] attackAbilities;
        private Jump jump;
        private MouseLook mouseLook;
        private AdaptiveTickrate adaptiveTickrate;
        private DebugHandler debugHandler;
        private PlayerEntity entity;
        private CooldownHandler cooldownHandler;
        private SimpleNetworkManager simpleNetworkManager;
        private PhysicsMovement physicsMovement;
        private NetworkMovement networkMovement;
        private LagCompensation lagCompensation;
        private ICameraInterpolation cameraInterpolation;

        private void Awake()
        {
            attackAbilities = GetComponents<IAttackAbility>();
            jump = GetComponent<Jump>();
            mouseLook = GetComponent<MouseLook>();
            cooldownHandler = GetComponent<CooldownHandler>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            physicsMovement = GetComponent<PhysicsMovement>();
            entity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            debugHandler = GetComponent<DebugHandler>();
            networkMovement = GetComponent<NetworkMovement>();
            simpleNetworkManager = FindAnyObjectByType<SimpleNetworkManager>();
            cameraInterpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            Initialize();
        }

        /// <summary>
        /// For server, local and unlocal client.
        /// </summary>
        private void Initialize()
        {
            adaptiveTickrate.Initialize(simpleNetworkManager, standardTickrate);
            debugHandler.Initialize(standardTickrate);
            mouseLook.Initialize();
            jump.Initialize();
            entity.Initialize(lagCompensation);
            physicsMovement.Initialize();
            networkMovement.Initialize(standardTickrate, cameraInterpolation);
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

                adaptiveTickrate.OnDisplayTickrate += debugHandler.DisplayTickrate;
                adaptiveTickrate.OnPlayTickrateChangedSound += debugHandler.PlayTickrateChangedSound;
                networkMovement.OnDisplayServerState += debugHandler.DisplayServerState;
                networkMovement.OnDisplayTick += debugHandler.DisplayTick;
                networkMovement.OnDisplayCheats += debugHandler.DisplayCheats;
                networkMovement.OnDisplayReconsile += debugHandler.DisplayReconsile;
                networkMovement.OnPlayReconsileSound += debugHandler.PlayReconsileSound;
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

            for (int i = 0; i < attackAbilities.Length; i++)
            {
                attackAbilities[i].DoLocalUpdate(networkMovement.MovementTick, entity.RollbackTick);
            }

            int ticks = networkMovement.DoLocalUpdate();
            for (int i = 0; i < ticks; i++)
            {
                cooldownHandler.Charge();
            }

            if (disconnect.WasPressed)
                connectionToServer.Disconnect();
        }
    }
}
