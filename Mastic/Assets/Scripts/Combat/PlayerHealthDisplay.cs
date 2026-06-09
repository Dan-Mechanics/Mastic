using UnityEngine;
using Mirror;

namespace Mastic
{
    public class PlayerHealthDisplay : MonoBehaviour
    {
        private FillBar fillBar;
        private float oldValue, newValue;
        private float syncInterval, time;

        public void Initialize(string name)
        {
            EasySettings easySettings = EasySettings.Current;
            float maxHealth = easySettings.Get<float>(nameof(maxHealth));
            syncInterval = easySettings.Get<float>(nameof(syncInterval));
            fillBar = new FillBar();
            fillBar.Initialize(name, transform, 0f, maxHealth);
            DisplayHealth(0f, 0f);
            fillBar.Set(0f);
        }

        [ClientCallback]
        private void FixedUpdate()
        {
            float t = (Time.time - time) / syncInterval;
            fillBar.Set(Mathf.Lerp(oldValue, newValue, t));
        }

        public void DisplayHealth(float oldValue, float newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
            time = Time.time;
        }
    }
}