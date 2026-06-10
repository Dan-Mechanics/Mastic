using Mirror;
using System;
using UnityEngine;

namespace Mastic
{
    public class ClientSequence : NetworkBehaviour
    {
        public event Action<string> OnDisplayCheats;

        [SerializeField] private EasyBinding disconnect = default;
        private IReliableAttackAbility[] reliableAttackAbilities;
        private IUnreliableAttackAbility[] unreliableAttackAbilities;
        private NetworkMovement networkMovement;
        private PlayerEntity playerEntity;
        private CooldownHandler cooldownHandler;
        private CooldownDisplay cooldownDisplay;
        private float maxConsecutiveTicks;
        private float timer;

        public void Initialize(NetworkMovement networkMovement, CooldownHandler cooldownHandler, PlayerEntity playerEntity) 
        {
            this.networkMovement = networkMovement;
            this.cooldownHandler = cooldownHandler;
            this.playerEntity = playerEntity;
            reliableAttackAbilities = GetComponents<IReliableAttackAbility>();
            unreliableAttackAbilities = GetComponents<IUnreliableAttackAbility>();
            cooldownDisplay = GetComponent<CooldownDisplay>();
            maxConsecutiveTicks = EasySettings.Current.Get<float>(nameof(maxConsecutiveTicks));
        }

        [Client]
        public void DoLocalUpdate()
        {
            if (disconnect.WasPressed)
            {
                connectionToServer.Disconnect();
                return;
            }

            foreach (IReliableAttackAbility reliable in reliableAttackAbilities)
            {
                reliable.DoLocalUpdate(networkMovement.InputTick - 1, playerEntity.RollbackTick);
            }

            foreach (IUnreliableAttackAbility unreliable in unreliableAttackAbilities)
            {
                unreliable.DoLocalUpdate(networkMovement.InputTick - 1);
            }

            // DEBUG.
            int clientPacketMultiplier = 1;
            if (Input.GetKey(KeyCode.Mouse4)) { clientPacketMultiplier = 2; }
            else if (Input.GetKey(KeyCode.Mouse2)) { clientPacketMultiplier = 0; }

            timer += Time.deltaTime;
            timer = Mathf.Clamp(timer, 0f, Time.fixedDeltaTime * maxConsecutiveTicks);
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;
                OnDisplayCheats?.Invoke($"cheats: {clientPacketMultiplier} | fps: {Mathf.RoundToInt(1f / Time.smoothDeltaTime)}");
                for (int i = 0; i < clientPacketMultiplier; i++)
                {
                    networkMovement.DoLocalTick(playerEntity.RollbackTick);
                    cooldownHandler.Charge();
                }
            }

            cooldownDisplay.DoLocalUpdate();

            // DEBUG.
            if (Input.GetKeyDown(KeyCode.UpArrow))
                networkMovement.IncrementInputTick(10);

            if (Input.GetKeyDown(KeyCode.DownArrow))
                networkMovement.IncrementInputTick(-10);
        }
    }
}