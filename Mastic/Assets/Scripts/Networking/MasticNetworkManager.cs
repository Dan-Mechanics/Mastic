using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mastic
{
    /// <summary>
    /// Dont rename pls. --> it breaks everything.
    /// </summary>
    public class MasticNetworkManager : NetworkManager
    {
        [Header("Dan-Mechanics")]

        public const int STANDARD_TICKRATE = 64;
        public const float STANDARD_FIXED_DELTA_TIME = 1f / STANDARD_TICKRATE;

        [SerializeField] private Sequence sequence = null;
        [SerializeField] private int maxFps = 0;

        private readonly List<NetworkConnectionToClient> connections = new List<NetworkConnectionToClient>();
        private Transform respawn;

        public override void Awake()
        {
            base.Awake();

            respawn = GameObject.FindWithTag("Respawn").transform;
            sequence = FindAnyObjectByType<Sequence>();

            // ---

            Application.targetFrameRate = maxFps;
            QualitySettings.SetQualityLevel(0, false);

            Time.fixedDeltaTime = STANDARD_FIXED_DELTA_TIME;
            sendRate = STANDARD_TICKRATE;
            Physics.simulationMode = SimulationMode.Script;
        }

        /*public override void Start()
        {
            base.Start();

            Application.targetFrameRate = maxFps;
            QualitySettings.SetQualityLevel(0, false);

            Time.fixedDeltaTime = STANDARD_FIXED_DELTA_TIME;
            sendRate = STANDARD_TICKRATE;
            Physics.autoSimulation = false;

            *//*Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;*//*
        }*/

        public override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.Q) && Input.GetKey(KeyCode.LeftShift)) { Application.Quit(); }
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();

            Debug.Log("DISCONNECTED FROM SERVER");

            // what if we leave mid-shoot animation ? >>>
            //GameObject.FindWithTag("MainCamera").transform.GetChild(0).gameObject.SetActive(false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);

            Debug.Log("SERVER: A CLIENT HAS DISCONNECTED");

            NetworkServer.Shutdown();

            sequence.Clear();
            connections.Clear();

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public override void OnClientConnect()
        {
            base.OnClientConnect();

            /*Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;*/

            Debug.Log("CLIENT CONNECTED");
        }

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            if (connections.Count >= maxConnections) { return; }
            
            connections.Add(conn);
            
            if(connections.Count >= maxConnections) 
            {
                Debug.Log("STARTING GAME...");

                for (int i = 0; i < connections.Count; i++)
                {
                    GameObject player = Instantiate(playerPrefab, respawn.position, Quaternion.identity);

                    //player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
                    player.name = $"{playerPrefab.name} not yet initialized";

                    sequence.Register(player.GetComponent<NetworkPhysicsMovement>());
                    //LagCompensation.instance.Register(player.GetComponent<Entity>());

                    NetworkServer.AddPlayerForConnection(connections[i], player);
                }
            }
        }
    }
}