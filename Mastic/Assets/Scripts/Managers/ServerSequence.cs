using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class ServerSequence : MonoBehaviour
    {
        private readonly List<ServerPlayerWrapper> players = new List<ServerPlayerWrapper>();
        private LagCompensation lagCompensation;
        private float timer;

        private void Awake() => lagCompensation = FindAnyObjectByType<LagCompensation>();

        [ServerCallback]
        private void Update()
        {
            timer += Time.deltaTime;
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;
                Tick();
            }
        }

        private void Tick() 
        {
            Clean();
            lagCompensation.Clean();
            lagCompensation.RecordFrame();
            foreach (ServerPlayerWrapper player in players)
            {
                // MAKE SURE THE PLAYER CAN'T SHOOT HIMSELF.
                player.entity.EnableHitbox(false);
                for (int i = 0; i < player.attackAbilities.Length; i++)
                {
                    // HERE IS THE BUG !!
                    player.attackAbilities[i].DoServerTick(player.Shared.receivedInputTick);
                }

                player.entity.EnableHitbox(true);
            }

            lagCompensation.ReturnToPresent();

            // ===

            foreach (ServerPlayerWrapper player in players)
            {
                player.networkMovement.DoServerTick();
                player.cooldownHandler.Charge();
            }

            Physics.Simulate(Time.fixedDeltaTime);
            foreach (ServerPlayerWrapper player in players)
            {
                player.networkMovement.SendStateMessageToClient();
            }
        }

        private void Clean()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i] == null || players[i].transform == null)
                    players.RemoveAt(i);
            }
        }

        [Server]
        public void Register(Transform player) => players.Add(new ServerPlayerWrapper(player));

        [Server]
        public void Clear() => players.Clear();

        private class ServerPlayerWrapper 
        {
            public SharedPlayerFields Shared => player.Shared;
            private readonly Player player;

            public Transform transform;
            public IAttackAbility[] attackAbilities;
            public PlayerEntity entity;
            public NetworkMovement networkMovement;
            public CooldownHandler cooldownHandler;

            public ServerPlayerWrapper(Transform transform)
            {
                this.transform = transform;
                player = transform.GetComponent<Player>();
                attackAbilities = transform.GetComponents<IAttackAbility>();
                entity = transform.GetComponent<PlayerEntity>();
                networkMovement = transform.GetComponent<NetworkMovement>();
                cooldownHandler = transform.GetComponent<CooldownHandler>();
            }
        }
    }
}