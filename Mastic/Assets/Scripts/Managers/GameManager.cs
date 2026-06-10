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
        private LobbyCanvasHandler lobbyCanvasHandler;
        private ServerSequence serverSequence;

        private void Awake()
        {
            sceneBoilerplate = FindAnyObjectByType<SceneBoilerplate>();
            simpleNetworkManager = FindAnyObjectByType<SimpleNetworkManager>();   
            serverSequence = FindAnyObjectByType<ServerSequence>();
            lobbyCanvasHandler = FindAnyObjectByType<LobbyCanvasHandler>();

            EasySettings easySettings = EasySettings.Current;
            if (logSettings)
                easySettings.Log(Debug.Log);

            var interpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            interpolation.MaxLerpValue = easySettings.Get<float>(nameof(interpolation.MaxLerpValue));
            standardTickrate = EasySettings.Current.Get<int>(nameof(standardTickrate));
        }

        private void Start()
        {
            sceneBoilerplate.AssignTickrate(standardTickrate);
            sceneBoilerplate.Initialize();
            lobbyCanvasHandler.Initialize();

            simpleNetworkManager.OnClientConnected += lobbyCanvasHandler.Disable;
            simpleNetworkManager.OnClientDisconnected += lobbyCanvasHandler.Enable;

            simpleNetworkManager.OnServerStarted += lobbyCanvasHandler.Disable;
            simpleNetworkManager.OnServerDisconnected += lobbyCanvasHandler.Enable;

            simpleNetworkManager.Initialize(standardTickrate);
            simpleNetworkManager.OnPlayerAdded += serverSequence.Register;
            simpleNetworkManager.OnServerDisconnected += serverSequence.Clear;
        }

        private void Update()
        {
            if (closeGame.WasPressed)
                Application.Quit();
        }
    }
}
