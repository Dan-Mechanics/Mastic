using Mirror;
using System;
using UnityEngine;

namespace Mastic
{
    public class ClientSequence : NetworkBehaviour
    {
        public event Action<string> OnDisplayCheats;

        [SerializeField] private EasyBinding disconnect = default;
        private IAttackAbility[] attackAbilities;
        private NetworkMovement networkMovement;
        private CooldownHandler cooldownHandler;
        private SharedPlayerFields shared;
        private float timer;

        public void Initialize(NetworkMovement networkMovement, IAttackAbility[] attackAbilities, CooldownHandler cooldownHandler, SharedPlayerFields shared)
        {
            this.networkMovement = networkMovement;
            this.attackAbilities = attackAbilities;
            this.cooldownHandler = cooldownHandler;
            this.shared = shared;
        }

        [Client]
        public void DoLocalUpdate()
        {
            if (disconnect.WasPressed)
            {
                connectionToServer.Disconnect();
                return;
            }

            for (int i = 0; i < attackAbilities.Length; i++)
            {
                attackAbilities[i].DoLocalUpdate(shared.currentTick, shared.rollbackTick);
            }

            int clientPacketMultiplier = 1;
            if (Input.GetKey(KeyCode.Mouse4)) { clientPacketMultiplier = 2; }
            else if (Input.GetKey(KeyCode.Mouse2)) { clientPacketMultiplier = 0; }

            timer += Time.deltaTime;
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;
                OnDisplayCheats?.Invoke($"cheats: {clientPacketMultiplier}");
                for (int i = 0; i < clientPacketMultiplier; i++)
                {
                    networkMovement.DoLocalTick();
                    cooldownHandler.Charge();
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow)) { shared.currentTick += 10; Debug.LogWarning("+10"); }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { shared.currentTick -= 10; Debug.LogWarning("-10"); }
        }
    }
}