using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private string defaultName = default;
        [SerializeField, Min(1)] private int standardTickrate = default;
        [SerializeField] private List<Object> localRemove = default;
        [SerializeField] private List<Object> unlocalRemove = default;
        [SerializeField] private List<Object> serverRemove = default;

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
        private SharedPlayerFields shared;

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
            StartAll();
        }

        public void SetShared(SharedPlayerFields shared) => this.shared = shared;

        /// <summary>
        /// For server, local and unlocal client.
        /// </summary>
        private void StartAll()
        {
            if (shared == null)
                SetShared(new SharedPlayerFields());

            debugHandler.Initialize(standardTickrate);
            adaptiveTickrate.Initialize(standardTickrate);
            playerEntity.Initialize(lagCompensation);
            playerEntity.SetShared(shared);

            networkMovement.Initialize(standardTickrate, cameraInterpolation,
                shared, movementAbilities);

            clientSequence.Initialize(networkMovement, attackAbilities,
                cooldownHandler, shared);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
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

        private void FixedUpdate() => print(shared);
    }
}
