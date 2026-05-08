using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Mastic
{
    public class ServerSequence : MonoBehaviour
    {
        private readonly List<Player> players = new List<Player>();
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
            foreach (Player player in players)
            {
                player.stateBufferIndex = player.networkMovement.DoServerTick();
                player.cooldownHandler.Charge();
            }

            Physics.Simulate(Time.fixedDeltaTime);
            foreach (Player player in players)
            {
                player.networkMovement.LimitSpeed();
                player.networkMovement.SendAuthStateToClient(player.stateBufferIndex);
            }

            lagCompensation.Clean();
            lagCompensation.RecordFrame();
            foreach (Player player in players)
            {
                // MAKE SURE THE PLAYER CAN'T SHOOT HIMSELF.
                player.entity.EnableHitbox(false);
                for (int i = 0; i < player.attacks.Length; i++)
                {
                    player.attacks[i].DoServerTick(player.networkMovement.MovementTick);
                }

                player.entity.EnableHitbox(true);
            }

            lagCompensation.ReturnToPresent();
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
        public void Register(Transform player) => players.Add(new Player(player));

        [Server]
        public void Clear() => players.Clear();

        private class Player 
        {
            public NetworkMovement networkMovement;
            public CooldownHandler cooldownHandler;
            public Transform transform;
            public IAttack[] attacks;
            public PlayerEntity entity;
            public int stateBufferIndex;

            public Player(Transform transform)
            {
                this.transform = transform;
                attacks = transform.GetComponents<IAttack>();
                entity = transform.GetComponent<PlayerEntity>();
                networkMovement = transform.GetComponent<NetworkMovement>();
                cooldownHandler = transform.GetComponent<CooldownHandler>();
            }
        }
    }
}