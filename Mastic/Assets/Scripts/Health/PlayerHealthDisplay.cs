using UnityEngine;
using Mirror;

namespace Mastic
{
    public class PlayerHealthDisplay : MonoBehaviour
    {
        private FillBar fillBar;
        private float oldValue, newValue;
        private float syncInterval, time;

        public void Initialize(string fillBarName)
        {
            EasySettings easySettings = EasySettings.Current;
            float maxHealth = easySettings.Get<float>(nameof(maxHealth));
            syncInterval = easySettings.Get<float>(nameof(syncInterval));
            fillBar = new FillBar(fillBarName, transform, 0f, maxHealth);
            fillBar.Set(0f);
        }

        [ClientCallback]
        private void FixedUpdate()
        {
            if (fillBar == null)
                return;

            float lerpValue = (Time.time - time) / syncInterval;
            fillBar.Set(Mathf.Lerp(oldValue, newValue, lerpValue));
        }

        public void DisplayHealth(float oldValue, float newValue)
        {
            this.oldValue = oldValue;
            this.newValue = newValue;
            time = Time.time;
        }
    }
}