using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class ServerSequence : MonoBehaviour
    {
        public int PlayerCount => players.Count;
        private readonly List<ServerPlayer> players = new List<ServerPlayer>();
        private LagCompensation lagCompensation;
        private float maxConsecutiveTicks;
        private float timer;

        private void Awake()
        {
            maxConsecutiveTicks = EasySettings.Current.Get<float>(nameof(maxConsecutiveTicks));
            lagCompensation = FindAnyObjectByType<LagCompensation>();
        }

        [ServerCallback]
        private void Update()
        {
            timer += Time.deltaTime;
            timer = Mathf.Clamp(timer, 0f, Time.fixedDeltaTime * maxConsecutiveTicks);
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;
                Tick();
            }
        }

        private void Tick() 
        {
            RemoveNullPlayers();
            lagCompensation.CleanNullEntities();
            lagCompensation.RecordFrame();
            foreach (ServerPlayer player in players)
            {
                player.entity.EnableHitbox(false);
                foreach (IReliableAttackAbility reliable in player.reliableAttackAbilities)
                {
                    reliable.DoServerTick(player.networkMovement.ProcessedTick);
                }

                foreach (IUnreliableAttackAbility unreliable in player.unreliableAttackAbilities)
                {
                    unreliable.DoServerTick(player.networkMovement.ProcessedTick,
                        player.networkMovement.ServerTick, player.networkMovement.GetPreviousInputMessage());
                }

                player.entity.EnableHitbox(true);
            }

            lagCompensation.ReturnToPresent();

            // ===

            foreach (ServerPlayer player in players)
            {
                player.networkMovement.DoServerTick();
            }

            // WE BATCH ALL THE MOVEMENT ABILITIES BECAUSE
            // OTHERWISE WE HAVE TO MESS WITH ORDERING OF PLAYERS.
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

        [Server]
        public void RemoveNullPlayers()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i] == null || players[i].transform == null)
                    players.RemoveAt(i);
            }
        }

        [Server]
        public void Register(Transform player) 
            => players.Add(new ServerPlayer(player));

        [Server]
        public void Clear() 
            => players.Clear();

        public (NetworkConnectionToClient, int) GetProcessedTick(int index)
        {
            if (index < 0 || index >= players.Count)
                return default;

            return (players[index].networkMovement.connectionToClient, players[index].networkMovement.ProcessedTick);
        }

        private class ServerPlayer 
        {
            public NetworkConnectionToClient connectionToClient;
            public IReliableAttackAbility[] reliableAttackAbilities;
            public IUnreliableAttackAbility[] unreliableAttackAbilities;
            public NetworkMovement networkMovement;
            public CooldownHandler cooldownHandler;
            public Transform transform;
            public PlayerEntity entity;

            public ServerPlayer(Transform transform)
            {
                this.transform = transform;
                reliableAttackAbilities = transform.GetComponents<IReliableAttackAbility>();
                unreliableAttackAbilities = transform.GetComponents<IUnreliableAttackAbility>();
                entity = transform.GetComponent<PlayerEntity>();
                networkMovement = transform.GetComponent<NetworkMovement>();
                connectionToClient = networkMovement.connectionToClient;
                cooldownHandler = transform.GetComponent<CooldownHandler>();
            }
        }
    }
}