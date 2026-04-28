using Mirror;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// The goal of this is to have a decoupling between 
    /// network manager and the player manager.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EasyBinding closeGame = default;
        [SerializeField, Min(1)] private int standardTickrate = default;
        [SerializeField] private string spawnpointTag = default;

        private SceneSetup sceneSetup;
        private SimpleNetworkManager networkManager;
        private PlayerManager sequence;
        private Transform spawnpoint;

        private void Awake()
        {
            sceneSetup = FindAnyObjectByType<SceneSetup>();
            networkManager = FindAnyObjectByType<SimpleNetworkManager>();   
            sequence = FindAnyObjectByType<PlayerManager>();
            spawnpoint = GameObject.FindWithTag(spawnpointTag).transform;
        }

        private void Start()
        {
            sceneSetup.Setup(standardTickrate);
            networkManager.Setup(spawnpoint, standardTickrate);
            networkManager.OnRegisterPlayer += sequence.Register;
            networkManager.OnReload += sequence.Clear;
        }

        private void Update()
        {
            if (closeGame.WasPressed)
                Application.Quit();
        }
    }
}
