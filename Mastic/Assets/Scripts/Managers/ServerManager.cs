using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Mastic
{
    /// <summary>
    /// Server script for handling the sequence of player code.
    /// Rename this server sequence manager because it will include lagcompensation soon.
    /// </summary>
    public class ServerManager : MonoBehaviour
    {
        private readonly List<Player> players = new List<Player>();
        private float timer;

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

            // SHOOT STEP SOMEWHERE HERE.

            Physics.Simulate(Time.fixedDeltaTime);
            foreach (Player player in players)
            {
                player.networkMovement.LimitSpeed();
                player.networkMovement.Send(player.stateBufferIndex);
                player.networkMovement.RpcSendStateMessageToClients(player.transform.position, player.transform.rotation, player.eyes.localRotation);
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
        public void Register(Transform player) => players.Add(new Player(player));

        [Server]
        public void Clear() => players.Clear();

        private class Player 
        {
            public NetworkMovement networkMovement;
            //public Weapon weapon;
            public Transform eyes;
            public Transform transform;
            public int stateBufferIndex;

            public Player(Transform transform)
            {
                this.transform = transform;
                networkMovement = transform.GetComponent<NetworkMovement>();
                eyes = transform.Find("eyes");
            }
        }
    }
}