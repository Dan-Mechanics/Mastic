using UnityEngine;

namespace Mastic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EasyBinding closeGame = default;
        [SerializeField] private bool logSettings = default;

        private int standardTickrate;
        private SceneBoilerplate sceneBoilerplate;
        private SimpleNetworkManager networkManager;
        private ServerSequence serverSequence;

        private void Awake()
        {
            sceneBoilerplate = FindAnyObjectByType<SceneBoilerplate>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();   
            serverSequence = FindAnyObjectByType<ServerSequence>();

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

            networkManager.Initialize(standardTickrate);
            networkManager.OnRegisterPlayer += serverSequence.Register;
            networkManager.OnReload += serverSequence.Clear;
        }

        private void Update()
        {
            if (closeGame.WasPressed)
                Application.Quit();
        }
    }
}
