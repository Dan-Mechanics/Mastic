using UnityEngine;

namespace Mastic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EasyBinding closeGame = default;
        [SerializeField] private string spawnpointTag = default;

        private int standardTickrate;
        private SceneBoilerplate sceneBoilerplate;
        private SimpleNetworkManager networkManager;
        private ServerSequence serverSequence;
        private Transform spawnpoint;

        private void Awake()
        {
            sceneBoilerplate = FindAnyObjectByType<SceneBoilerplate>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();   
            serverSequence = FindAnyObjectByType<ServerSequence>();
            spawnpoint = GameObject.FindWithTag(spawnpointTag).transform;

            var easySettings = EasySettings.Current;
            easySettings.Log(Debug.Log);

            var interpolation = GameObject.FindWithTag("MainCamera").GetComponent<ICameraInterpolation>();
            interpolation.MaxLerpValue = easySettings.Get<float>(nameof(interpolation.MaxLerpValue));
        }

        private void Start()
        {
            standardTickrate = EasySettings.Current.Get<int>(nameof(standardTickrate));

            sceneBoilerplate.SetTickrate(standardTickrate);
            sceneBoilerplate.Initialize();

            networkManager.Initialize(spawnpoint, standardTickrate);
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
