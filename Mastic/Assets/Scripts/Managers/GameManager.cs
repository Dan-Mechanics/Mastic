using UnityEngine;

namespace Mastic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EasyBinding closeGame = default;
        [SerializeField] private bool logSettings = default;

        private int standardTickrate;
        private SceneBoilerplate sceneBoilerplate;
        private SimpleNetworkManager simpleNetworkManager;
        private PlayerSpawner playerSpawner;
        private LobbyHandler lobbyHandler;
        private ServerSequence serverSequence;

        private void Awake()
        {
            sceneBoilerplate = FindAnyObjectByType<SceneBoilerplate>();
            simpleNetworkManager = FindAnyObjectByType<SimpleNetworkManager>();
            playerSpawner = FindAnyObjectByType<PlayerSpawner>();
            serverSequence = FindAnyObjectByType<ServerSequence>();
            lobbyHandler = FindAnyObjectByType<LobbyHandler>(FindObjectsInactive.Include);

            EasySettings easySettings = EasySettings.Current;
            if (logSettings)
                easySettings.Log(Debug.Log);

            ICameraInterpolation interpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            interpolation.MaxLerpValue = easySettings.Get<float>(nameof(interpolation.MaxLerpValue));
            standardTickrate = EasySettings.Current.Get<int>(nameof(standardTickrate));
        }

        private void Start()
        {
            sceneBoilerplate.AssignTickrate(standardTickrate);
            sceneBoilerplate.Initialize();
            lobbyHandler.Initialize(simpleNetworkManager);

            simpleNetworkManager.OnClientConnected += lobbyHandler.Disable;
            simpleNetworkManager.OnClientConnected += playerSpawner.ConnectToServer;
            simpleNetworkManager.OnClientDisconnected += lobbyHandler.Enable;

            simpleNetworkManager.OnServerGameStarted += lobbyHandler.Disable;
            simpleNetworkManager.OnServerDisconnected += lobbyHandler.Enable;

            simpleNetworkManager.Initialize(standardTickrate);
            playerSpawner.OnPlayerSpawned += serverSequence.Register;
            simpleNetworkManager.OnPlayersConnected += playerSpawner.InitializePlayers;
            simpleNetworkManager.OnServerDisconnected += serverSequence.Clear;
        }

        private void Update()
        {
            if (closeGame.WasPressed)
                Application.Quit();
        }
    }
}
