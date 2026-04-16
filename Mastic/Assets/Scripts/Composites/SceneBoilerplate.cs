using System.Globalization;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApplyYourself
{
    public class SceneBoilerplate : MonoBehaviour
    {
        [SerializeField] private EasyBinding reload = default;
        [SerializeField] private EasyBinding escape = default;
        [SerializeField] private EasyBinding shift = default;
        [SerializeField] private EasyBinding ctrl = default;
        [SerializeField] private int fps = default;
        [SerializeField] private float physicsTicksPerSecond = default;

        private void Awake()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Application.targetFrameRate = fps;
            Time.fixedDeltaTime = 1f / physicsTicksPerSecond;

            QualitySettings.SetQualityLevel(0);
            QualitySettings.vSyncCount = 0;
        }

        private void Update()
        {
            if (reload.WasPressed)
                ReloadScene();

            if ((shift.IsHeld || ctrl.IsHeld) && escape.WasPressed)
                QuitGame();
        }

        public void QuitGame() => Application.Quit();
        public void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}