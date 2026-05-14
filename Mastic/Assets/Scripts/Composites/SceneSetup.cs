using System.Globalization;
using System.Threading;
using UnityEngine;

namespace Mastic
{
    public class SceneSetup : MonoBehaviour
    {
        [SerializeField, Min(1)] private int framerateLimit = default;
        [SerializeField, Min(1)] private int tickrate = default;
        [SerializeField] private SimulationMode simulationMode = default;
        [SerializeField] private bool initializeOnAwake = default;

        private void Awake()
        {
            if (initializeOnAwake)
                Initialize();
        }

        public void SetTickrate(int tickrate) => this.tickrate = tickrate;

        public void Initialize()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Application.targetFrameRate = framerateLimit;
            Time.fixedDeltaTime = 1f / tickrate;
            Physics.simulationMode = simulationMode;

            QualitySettings.SetQualityLevel(0, false);
            QualitySettings.vSyncCount = 0;

            Destroy(gameObject);
        }
    }
}