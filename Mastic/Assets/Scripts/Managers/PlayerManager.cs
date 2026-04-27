using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Mastic
{
    public class PlayerManager : MonoBehaviour
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
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                player.stateBufferIndex = player.networkMovement.DoServerMovementTick();
            }

            Physics.Simulate(Time.fixedDeltaTime);
            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                player.networkMovement.CapVelocity();
                player.networkMovement.Send(player.stateBufferIndex);
                player.networkMovement.RpcSendStateMessageToClients(player.transform.position, player.transform.rotation, player.eyes.localRotation);
            }
        }

        private void Clean()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (!players[i].Exists)
                    players.RemoveAt(i);
            }
        }

        [Server]
        public void Register(Transform player) => players.Add(new Player(player));

        [Server]
        public void Clear() => players.Clear();

        private class Player 
        {
            public bool Exists => transform != null;
            
            public NetworkPhysicsMovement networkMovement;
            //public Weapon weapon;
            public Transform eyes;
            public Transform transform;
            public int stateBufferIndex;

            public Player(Transform transform)
            {
                this.transform = transform;
                networkMovement = transform.GetComponent<NetworkPhysicsMovement>();
                eyes = transform.Find("eyes");
            }
        }
    }
}