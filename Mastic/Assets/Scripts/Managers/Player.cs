using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        public PlayerTicks PlayerTicks { get; set; }
        
        [SerializeField] private string defaultName = default;
        [SerializeField] private List<Object> localRemove = default;
        [SerializeField] private List<Object> unlocalRemove = default;
        [SerializeField] private List<Object> serverRemove = default;

        private ICameraInterpolation cameraInterpolation;
        private IReliableAttackAbility[] reliableAttackAbilities;
        private IUnreliableAttackAbility[] unreliableAttackAbilities;
        private AdaptiveTickrate adaptiveTickrate;
        private CooldownHandler cooldownHandler;
        private NetworkMovement networkMovement;
        private LagCompensation lagCompensation;
        private ClientSequence clientSequence;
        private DebugHandler debugHandler;
        private PlayerEntity playerEntity;

        private void Awake()
        {
            reliableAttackAbilities = GetComponents<IReliableAttackAbility>();
            unreliableAttackAbilities = GetComponents<IUnreliableAttackAbility>();
            cooldownHandler = GetComponent<CooldownHandler>();
            clientSequence = GetComponent<ClientSequence>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            playerEntity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            debugHandler = GetComponent<DebugHandler>();
            networkMovement = GetComponent<NetworkMovement>();
            cameraInterpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            PlayerTicks = new PlayerTicks();
            Initialize();
        }

        /// <summary>
        /// For server, local and unlocal client.
        /// </summary>
        private void Initialize()
        {
            int standardTickrate = EasySettings.Current.Get<int>(nameof(standardTickrate));
            debugHandler.Initialize(standardTickrate);
            adaptiveTickrate.Initialize(standardTickrate);
            networkMovement.Initialize(standardTickrate, cameraInterpolation, PlayerTicks);
            clientSequence.Initialize(networkMovement, reliableAttackAbilities, unreliableAttackAbilities,
                cooldownHandler, playerEntity);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            playerEntity.Initialize(lagCompensation);
            serverRemove.ForEach(x => Destroy(x));
            print($"{gameObject.name}: setup completed".ToUpperInvariant());
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (isLocalPlayer)
            {
                Utils.LockMouse();
                gameObject.name = $"{defaultName} | local client";
                localRemove.ForEach(x => Destroy(x));

                adaptiveTickrate.OnDisplayTickrate += debugHandler.DisplayTickrate;
                adaptiveTickrate.OnPlayTickrateChangedSound += debugHandler.PlayTickrateChangedSound;
                networkMovement.OnDisplayServerState += debugHandler.DisplayServerState;
                networkMovement.OnDisplayTick += debugHandler.DisplayTick;
                clientSequence.OnDisplayCheats += debugHandler.DisplayCheats;
                networkMovement.OnDisplayReconsile += debugHandler.DisplayReconsile;
                networkMovement.OnPlayReconsileSound += debugHandler.PlayReconsileSound;
            }
            else
            {
                gameObject.tag = "Untagged";
                gameObject.name = $"{defaultName} | unlocal client";
                unlocalRemove.ForEach(x => Destroy(x));
            }

            print($"{gameObject.name}: setup completed".ToUpperInvariant());
        }

        private void Update()
        {
            if (isLocalPlayer)
                clientSequence.DoLocalUpdate();
        }
    }
}
