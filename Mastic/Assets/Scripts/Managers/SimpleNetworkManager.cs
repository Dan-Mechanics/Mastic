using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mastic
{
    public class SimpleNetworkManager : NetworkManager
    {
        public event Action<Transform> OnRegisterPlayer;
        public event Action OnReload;

        private List<NetworkConnectionToClient> connections;
        private Transform spawnpoint;

        public void Setup(Transform spawnpoint, int standardTickrate)
        {
            this.spawnpoint = spawnpoint;
            connections = new List<NetworkConnectionToClient>();
            sendRate = standardTickrate;
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            print("CLIENT: DISCONNECTED FROM SERVER");

            Utils.UnlockMouse();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            Debug.Log("SERVER: A CLIENT HAS DISCONNECTED");

            connections.Clear();
            OnReload?.Invoke();
            NetworkServer.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            Debug.Log("CLIENT: CONNECTED TO SERVER");
            Utils.LockMouse();
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (connections.Count >= maxConnections)
                return;

            connections.Add(conn);
            if (connections.Count < maxConnections)
                return;

            connections.ForEach(x => AddPlayer(conn));
            print("STARTING GAME");
        }

        [Server]
        private void AddPlayer(NetworkConnectionToClient conn)
        {
            GameObject player = Instantiate(playerPrefab, spawnpoint.position, Quaternion.identity);
            player.name = $"uninitialized_{playerPrefab.name}_[{conn.connectionId}]";
            OnRegisterPlayer?.Invoke(player.transform);
            NetworkServer.AddPlayerForConnection(conn, player);
        }
    }
}