using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mastic
{
    public class SimpleNetworkManager : NetworkManager
    {
        public event Action<Transform> OnPlayerAdded;
        public event Action OnServerStarted;
        public event Action OnServerDisconnected;
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;

        private List<NetworkConnectionToClient> connections;
        private Transform respawns;

        public void Initialize(int standardTickrate)
        {
            respawns = GameObject.FindWithTag("Respawn").transform;
            connections = new List<NetworkConnectionToClient>();
            sendRate = standardTickrate;
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            print("CLIENT: DISCONNECTED FROM SERVER");

            Utils.UnlockMouse();
            OnClientDisconnected?.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            Debug.Log("SERVER: A CLIENT HAS DISCONNECTED");
            
            connections.Clear();
            OnServerDisconnected?.Invoke();
            NetworkServer.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            OnClientConnected?.Invoke();
            Debug.Log("CLIENT: CONNECTED TO SERVER");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (connections.Count >= maxConnections)
                return;

            connections.Add(conn);
            if (connections.Count < maxConnections)
                return;

            connections.ForEach(x => AddPlayer(x));
            OnServerStarted?.Invoke();
            print("STARTING GAME");
        }

        [Server]
        private void AddPlayer(NetworkConnectionToClient conn)
        {
            GameObject player = Instantiate(playerPrefab, respawns.GetChild(respawns.childCount - 1).position, Quaternion.identity);
            player.name = $"uninitialized_{playerPrefab.name}_[{conn.connectionId}]";

            NetworkServer.AddPlayerForConnection(conn, player);
            OnPlayerAdded?.Invoke(player.transform);
        }
    }
}