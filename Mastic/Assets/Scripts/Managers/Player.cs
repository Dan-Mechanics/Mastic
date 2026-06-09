using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private string defaultName = default;
        [SerializeField] private List<Object> localRemove = default;
        [SerializeField] private List<Object> unlocalRemove = default;
        [SerializeField] private List<Object> serverRemove = default;

        private ICameraInterpolation cameraInterpolation;
        private IReliableAttackAbility[] reliableAttackAbilities;
        private IUnreliableAttackAbility[] unreliableAttackAbilities;
        private PlayerHealthDisplay playerHealthDisplay;
        private AdaptiveTickrate adaptiveTickrate;
        private CooldownHandler cooldownHandler;
        private NetworkMovement networkMovement;
        private LagCompensation lagCompensation;
        private ClientSequence clientSequence;
        private DebugHandler debugHandler;
        private PlayerEntity playerEntity;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            reliableAttackAbilities = GetComponents<IReliableAttackAbility>();
            unreliableAttackAbilities = GetComponents<IUnreliableAttackAbility>();
            cooldownHandler = GetComponent<CooldownHandler>();
            playerHealthDisplay = GetComponent<PlayerHealthDisplay>();
            playerHealth = GetComponent<PlayerHealth>();
            clientSequence = GetComponent<ClientSequence>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            playerEntity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            debugHandler = GetComponent<DebugHandler>();
            networkMovement = GetComponent<NetworkMovement>();
            cameraInterpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            InitializeAll();
        }

        private void InitializeAll()
        {
            int standardTickrate = EasySettings.Current.Get<int>(nameof(standardTickrate));
            cooldownHandler.Initialize();
            debugHandler.Initialize(standardTickrate);
            adaptiveTickrate.Initialize(standardTickrate);
            networkMovement.Initialize(standardTickrate, cameraInterpolation);
            clientSequence.Initialize(networkMovement, reliableAttackAbilities, unreliableAttackAbilities,
                cooldownHandler, playerEntity);

            playerHealth.OnRespawn += playerEntity.Reload;
            playerHealth.OnRespawn += cooldownHandler.RechargeAll;
            playerHealth.OnHealthChanged += playerHealthDisplay.DisplayHealth;
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

                playerHealthDisplay.Initialize("local_health");
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
                // playerHealthDisplay.Initialize("unlocal_health");
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
