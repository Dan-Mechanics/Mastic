using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class PlayerSpawner : MonoBehaviour
    {
        public event Action<Transform> OnPlayerSpawned;
        [SerializeField] private EasyVar element = default;
        [SerializeField] private GameObject[] prefabs = default;
        private Transform respawnHolder;

        [Server]
        public void InitializePlayers(List<SimpleNetworkManager.ConnectInstructions> players)
        {
            respawnHolder = GameObject.FindWithTag("Respawn").transform;
            foreach (var player in players)
            {
                if (int.TryParse(player.instructions, out int index))
                {
                    // SANITIZE INPUT.
                    index = Mathf.Clamp(index, 0, prefabs.Length - 1);
                    SpawnPlayer(player.conn, prefabs[index]);
                }
                else
                {
                    // SERVER: GOODBYE CLIENT.
                    player.conn.Disconnect();
                }
            }
        }

        /// <summary>
        /// https://mirror-networking.gitbook.io/docs/manual/guides/gameobjects/custom-character-spawning
        /// </summary>
        [Client]
        public void ConnectToServer()
        {
            var message = new SimpleNetworkManager.CreatePlayerMessage()
            {
                instructions = element.Get<string>()
            };

            NetworkClient.Send(message);
        }

        private void SpawnPlayer(NetworkConnectionToClient conn, GameObject prefab)
        {
            Vector3 spawnPos = respawnHolder.GetChild(respawnHolder.childCount - 1).position;
            GameObject player = Instantiate(prefab, spawnPos, Quaternion.identity);
            player.name = $"uninitialized_{prefab.name}_[{conn.connectionId}]";

            NetworkServer.AddPlayerForConnection(conn, player);
            OnPlayerSpawned?.Invoke(player.transform);
        }
    }
}
