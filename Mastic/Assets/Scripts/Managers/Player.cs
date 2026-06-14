using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class Player : NetworkBehaviour
    {
        [SerializeField] private GameObject player = default;
        [SerializeField] private EasyVar sensitivity = default;
        [SerializeField] private string[] abilities = default;
        [SerializeField] private List<UnityEngine.Object> localRemove = default;
        [SerializeField] private List<UnityEngine.Object> unlocalRemove = default;
        [SerializeField] private List<UnityEngine.Object> serverRemove = default;

        private ICameraInterpolation cameraInterpolation;
        private PlayerHealthDisplay playerHealthDisplay;
        private AdaptiveTickrate adaptiveTickrate;
        private CooldownHandler cooldownHandler;
        private NetworkMovement networkMovement;
        private EntityManager lagCompensation;
        private ClientSequence clientSequence;
        private CooldownDisplay cooldownDisplay;
        private DebugHandler debugHandler;
        private PlayerEntity playerEntity;
        private PlayerHealth playerHealth;
        private MouseLook mouseLook;

        private void Awake()
        {
            cooldownHandler = GetComponent<CooldownHandler>();
            mouseLook = GetComponent<MouseLook>();
            cooldownDisplay = GetComponent<CooldownDisplay>();
            playerHealthDisplay = GetComponent<PlayerHealthDisplay>();
            playerHealth = GetComponent<PlayerHealth>();
            clientSequence = GetComponent<ClientSequence>();
            adaptiveTickrate = GetComponent<AdaptiveTickrate>();
            playerEntity = GetComponent<PlayerEntity>();
            lagCompensation = FindAnyObjectByType<EntityManager>();
            debugHandler = GetComponent<DebugHandler>();
            networkMovement = GetComponent<NetworkMovement>();
            cameraInterpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();

            Utils.LowerStringArray(abilities);
            EasySettings easySettings = EasySettings.Current;
            int standardTickrate = easySettings.Get<int>(nameof(standardTickrate));
            cooldownHandler.Initialize(abilities);
            debugHandler.Initialize(standardTickrate);
            adaptiveTickrate.Initialize(standardTickrate);
            networkMovement.Initialize(standardTickrate, cameraInterpolation);
            clientSequence.Initialize(networkMovement, cooldownHandler, playerEntity);

            mouseLook.SetSensitivity(easySettings.Get<float>(nameof(sensitivity)));
            sensitivity.WriteSafely<float>(mouseLook.SetSensitivity);

            playerHealth.OnRespawn += playerEntity.RefreshRollbackBuffer;
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
                gameObject.name = $"{player.name} | local client";
                localRemove.ForEach(x => Destroy(x));

                cooldownDisplay.Initialize(abilities);
                cooldownHandler.OnCast += cooldownDisplay.FlashCooldown;

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
                gameObject.name = $"{player.name} | unlocal client";
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
