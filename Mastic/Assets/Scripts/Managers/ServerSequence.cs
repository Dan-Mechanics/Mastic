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

        public void Setup(LagCompensation lagCompensation) => this.lagCompensation = lagCompensation;

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
            }

            Physics.Simulate(Time.fixedDeltaTime);
            foreach (Player player in players)
            {
                player.networkMovement.LimitSpeed();
                player.networkMovement.SendAuthStateToClient(player.stateBufferIndex);
            }

            lagCompensation.RecordFrame();
            foreach (Player player in players)
            {
                for (int i = 0; i < player.shootables.Length; i++)
                {
                    player.shootables[i].DoShootTick(player.networkMovement.MovementTick);
                }
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
            public Transform eyes;
            public Transform transform;
            public IShootable[] shootables;
            public int stateBufferIndex;

            public Player(Transform transform)
            {
                this.transform = transform;
                shootables = transform.GetComponents<IShootable>();
                networkMovement = transform.GetComponent<NetworkMovement>();
                eyes = transform.Find("eyes");
            }
        }
    }
}