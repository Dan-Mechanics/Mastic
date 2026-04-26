using Mirror;
using UnityEngine;

namespace Mastic
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private EasyBinding closeGame = default;
        [SerializeField, Min(1)] private int tickrate = default;

        private SceneSetup sceneSetup;
        private MasticNetworkManager networkManager;
        private Sequence sequence;

        private void Awake()
        {
            sceneSetup = FindAnyObjectByType<SceneSetup>();
            networkManager = FindAnyObjectByType<MasticNetworkManager>();   
            sequence = FindAnyObjectByType<Sequence>(); 
        }

        private void Start()
        {
            sceneSetup.Setup(tickrate);
            networkManager.OnRegisterPlayer += sequence.Register;
        }

        private void Update()
        {
            if (closeGame.WasPressed)
                Application.Quit();
        }
    }
}
