using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mastic
{
    public class SimpleNetworkManager : NetworkManager
    {
        public event Action<List<ConnectInstructions>> OnPlayersConnected;
        public event Action OnServerGameStarted;
        public event Action OnServerDisconnected;
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;
        private List<ConnectInstructions> connections;

        public void Initialize(int standardTickrate)
        {
            connections = new List<ConnectInstructions>();
            sendRate = standardTickrate;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            NetworkServer.RegisterHandler<CreatePlayerMessage>(OnCreatePlayer);
            print("SERVER: START");
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            Utils.UnlockMouse();
            OnClientDisconnected?.Invoke();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            print("CLIENT: DISCONNECTED FROM SERVER");
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            connections.Clear();
            OnServerDisconnected?.Invoke();
            NetworkServer.Shutdown();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            Debug.Log("SERVER: A CLIENT HAS DISCONNECTED");
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();
            OnClientConnected?.Invoke();
            Debug.Log("CLIENT: CONNECTED TO SERVER");
        }

        private void OnCreatePlayer(NetworkConnectionToClient conn, CreatePlayerMessage message)
        {
            if (connections.Count >= maxConnections)
            {
                // MAX CAPACITY REACHED.
                conn.Disconnect();
                return;
            }

            connections.Add(new ConnectInstructions(conn, message.instructions));
            if (connections.Count >= maxConnections)
            {
                OnPlayersConnected?.Invoke(connections);
                OnServerGameStarted?.Invoke();
                print("STARTING SERVER");
            }
        }

        public class ConnectInstructions
        {
            public NetworkConnectionToClient conn;
            public string instructions;

            public ConnectInstructions(NetworkConnectionToClient conn, string instructions)
            {
                this.conn = conn;
                this.instructions = instructions;
            }
        }

        /// <summary>
        /// https://mirror-networking.gitbook.io/docs/manual/guides/gameobjects/custom-character-spawning
        /// </summary>
        public struct CreatePlayerMessage : NetworkMessage
        {
            public string instructions;
        }
    }
}