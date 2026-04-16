using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Mastic
{
    /// <summary>
    /// Make it so that this works for multible clients too.
    /// </summary>
    public class Sequence : MonoBehaviour
    {
        //private readonly List<ServerAuthClientPredSimple> players = new List<ServerAuthClientPredSimple>();
        private readonly List<Player> players = new List<Player>();

        private float timer;

        [ServerCallback]
        private void Update()
        {
            timer += Time.deltaTime;

            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;
                _FixedUpdate();
            }
        }

        private void _FixedUpdate() 
        {
            Clean();

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                player.stateBufferIndex = player.movement.DoServerMovementTick();
            }

            Physics.Simulate(Time.fixedDeltaTime);

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];

                player.movement.CapVelocity();
                player.movement.Send(player.stateBufferIndex);
                player.movement.RpcSendStateMessageToClients(player.body.position, player.body.rotation, player.eyes.localRotation);
            }
        }

        private void Clean()
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].movement == null) { players.RemoveAt(i); }
            }
        }

        public void Register(NetworkPhysicsMovement movement) 
        {
            players.Add(new Player(movement, movement.transform.Find("eyes"), movement.transform));
        }

        public void Clear()
        {
            players.Clear();
        }

        private class Player 
        {
            public NetworkPhysicsMovement movement;
            //public Weapon weapon;
            public Transform eyes;
            public Transform body;
            public int stateBufferIndex;

            public Player(NetworkPhysicsMovement movement, Transform eyes, Transform body)
            {
                this.movement = movement;
                this.eyes = eyes;
                this.body = body;

                stateBufferIndex = 0;
            }
        }
    }
}