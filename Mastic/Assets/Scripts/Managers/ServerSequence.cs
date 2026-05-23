using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class ServerSequence : MonoBehaviour
    {
        private readonly List<ServerPlayer> players = new List<ServerPlayer>();
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
            foreach (ServerPlayer player in players)
            {
                player.entity.EnableHitbox(false);
                for (int i = 0; i < player.attackAbilities.Length; i++)
                {
                    player.attackAbilities[i].DoServerTick(player.Shared.processedTick);
                }

                player.entity.EnableHitbox(true);
            }

            lagCompensation.ReturnToPresent();

            // ===

            foreach (ServerPlayer player in players)
            {
                player.networkMovement.DoServerTick();
                //player.cooldownHandler.Charge();
            }

            foreach (ServerPlayer player in players)
            {
                player.networkMovement.DoServerMovementAbilities();
                player.cooldownHandler.Charge();
            }

            Physics.Simulate(Time.fixedDeltaTime);
            foreach (ServerPlayer player in players)
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
        public void Register(Transform player) => players.Add(new ServerPlayer(player));

        [Server]
        public void Clear() => players.Clear();

        [Server]
        public PlayerMovementConnection[] GetPlayerMovementConnections()
        {
            Clean();
            PlayerMovementConnection[] result = new PlayerMovementConnection[players.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new PlayerMovementConnection(players[i].connectionToClient, players[i].Shared.processedTick, i);
            }

            return result;
        }

        public struct PlayerMovementConnection
        {
            public NetworkConnectionToClient connection;
            public int processedTick;
            public int index;

            public PlayerMovementConnection(NetworkConnectionToClient connection, int processedTick, int index)
            {
                this.connection = connection;
                this.processedTick = processedTick;
                this.index = index;
            }
        }

        private class ServerPlayer 
        {
            public SharedPlayerFields Shared => player.Shared;
            private readonly Player player;

            public NetworkConnectionToClient connectionToClient;
            public IAttackAbility[] attackAbilities;
            public NetworkMovement networkMovement;
            public CooldownHandler cooldownHandler;
            public Transform transform;
            public PlayerEntity entity;

            public ServerPlayer(Transform transform)
            {
                this.transform = transform;
                player = transform.GetComponent<Player>();
                attackAbilities = transform.GetComponents<IAttackAbility>();
                entity = transform.GetComponent<PlayerEntity>();
                networkMovement = transform.GetComponent<NetworkMovement>();
                connectionToClient = networkMovement.connectionToClient;
                cooldownHandler = transform.GetComponent<CooldownHandler>();
            }
        }
    }
}