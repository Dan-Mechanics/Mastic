using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        public SharedPlayerFields Shared { get; set; }
        
        [SerializeField] private string defaultName = default;
        [SerializeField] private List<Object> localRemove = default;
        [SerializeField] private List<Object> unlocalRemove = default;
        [SerializeField] private List<Object> serverRemove = default;

        private int standardTickrate;
        private ICameraInterpolation cameraInterpolation;
        private IAttackAbility[] attackAbilities;
        private List<IMovementAbility> movementAbilities;
        private AdaptiveTickrate adaptiveTickrate;
        private CooldownHandler cooldownHandler;
        private NetworkMovement networkMovement;
        private LagCompensation lagCompensation;
        private ClientSequence clientSequence;
        private DebugHandler debugHandler;
        private PlayerEntity playerEntity;

        private void Awake()
        {
            attackAbilities = GetComponents<IAttackAbility>();
            cooldownHandler = GetComponent<CooldownHandler>();
            movementAbilities = GetComponents<IMovementAbility>().ToList();
            clientSequence = GetComponent<ClientSequence>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            playerEntity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            debugHandler = GetComponent<DebugHandler>();
            networkMovement = GetComponent<NetworkMovement>();
            cameraInterpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            Shared = new SharedPlayerFields();
            Initialize();
        }

        /// <summary>
        /// For server, local and unlocal client.
        /// </summary>
        private void Initialize()
        {
            EasySettings easySettings = FindAnyObjectByType<EasySettings>();
            easySettings.Get(nameof(standardTickrate), ref standardTickrate);

            debugHandler.Initialize(standardTickrate);
            adaptiveTickrate.Initialize(standardTickrate);
            playerEntity.SetShared(Shared);

            networkMovement.Initialize(standardTickrate, cameraInterpolation,
                Shared, movementAbilities);

            clientSequence.Initialize(networkMovement, attackAbilities,
                cooldownHandler, Shared);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            playerEntity.Initialize(lagCompensation);
            gameObject.name = $"{defaultName} | server";
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
