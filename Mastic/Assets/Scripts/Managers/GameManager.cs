using UnityEngine;

namespace Mastic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EasyBinding closeGame = default;
        [SerializeField, Min(1)] private int standardTickrate = default;
        [SerializeField] private string spawnpointTag = default;

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
            FindAnyObjectByType<EasySettings>().Log(Debug.Log);
        }

        private void Start()
        {
            // MAKE SURE THE SCENE SETUP IS DONE FIRST.
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
