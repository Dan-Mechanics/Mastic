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
        private SharedPlayerFields sharedPlayerFields;
        private IAttackAbility[] attackAbilities;
        private List<IMovementAbility> movementAbilities;
        private AdaptiveTickrate adaptiveTickrate;
        private CooldownHandler cooldownHandler;
        private PhysicsMovement physicsMovement;
        private NetworkMovement networkMovement;
        private LagCompensation lagCompensation;
        private ClientSequence clientSequence;
        private DebugHandler debugHandler;
        private PlayerEntity playerEntity;
        private MouseLook mouseLook;

        private void Awake()
        {
            SetShared(new SharedPlayerFields());
            attackAbilities = GetComponents<IAttackAbility>();
            mouseLook = GetComponent<MouseLook>();
            cooldownHandler = GetComponent<CooldownHandler>();
            movementAbilities = GetComponents<IMovementAbility>().ToList();
            clientSequence = GetComponent<ClientSequence>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            physicsMovement = GetComponent<PhysicsMovement>();
            playerEntity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<LagCompensation>();
            debugHandler = GetComponent<DebugHandler>();
            networkMovement = GetComponent<NetworkMovement>();
            cameraInterpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            Initialize();
        }

        public void SetShared(SharedPlayerFields sharedPlayerFields)
        {
            if (this.sharedPlayerFields == null)
                this.sharedPlayerFields = sharedPlayerFields;
        }

        /// <summary>
        /// For server, local and unlocal client.
        /// </summary>
        private void Initialize()
        {
            debugHandler.Initialize(standardTickrate);
            mouseLook.Initialize();
            adaptiveTickrate.Initialize(standardTickrate);
            playerEntity.Initialize(lagCompensation);
            playerEntity.SetShared(sharedPlayerFields);
            physicsMovement.Initialize();

            networkMovement.Initialize(standardTickrate, cameraInterpolation,
                sharedPlayerFields, movementAbilities);

            clientSequence.Initialize(networkMovement, attackAbilities,
                cooldownHandler, sharedPlayerFields);
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
    }
}
