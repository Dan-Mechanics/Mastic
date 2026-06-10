using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class CooldownDisplay : NetworkBehaviour
    {
        [SerializeField] private Transform cooldownHolder = default;
        private CooldownHandler cooldownHandler;
        private CooldownVisual[] visuals;
        private float fadeSpeed;

        public void Initialize(CooldownHandler cooldownHandler, List<string> abilities)
        {
            this.cooldownHandler = cooldownHandler;
            EasySettings easySettings = EasySettings.Current;
            fadeSpeed = easySettings.Get<float>(nameof(fadeSpeed));

            int length = abilities.Count > cooldownHolder.childCount ? cooldownHolder.childCount : abilities.Count;
            visuals = new CooldownVisual[length];
            for (int i = 0; i < visuals.Length; i++)
            {
                visuals[i] = new CooldownVisual(cooldownHolder.GetChild(i));
                visuals[i].icon.sprite = Resources.Load<Sprite>($"{abilities[i]}");
                visuals[i].key.text = easySettings.Get<string>(abilities[i] + nameof(CooldownVisual.key));
                visuals[i].flash.alpha = 1f;
            }

            Refresh();
        }

        private void FixedUpdate()
        {
            if (isLocalPlayer)
                Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                if (!cooldownHandler.GetCooldownStatus(i, out int stack, out float remainderPercentage))
                    continue;

                visuals[i].stack.text = Utils.GetRoman(stack);
                visuals[i].icon.fillAmount = remainderPercentage;
                visuals[i].icon.color = stack > 0 ? Color.white : Color.gray;
                visuals[i].flash.alpha -= Time.fixedDeltaTime * fadeSpeed;
            }
        }

        [Client]
        public void FlashCooldown(int index) 
        {
            if (index >= 0 || index < visuals.Length)
                visuals[index].flash.alpha = 1f;
        }

        private class CooldownVisual
        {
            public CanvasGroup flash;
            public TMP_Text stack;
            public TMP_Text key;
            public Image icon;

            public CooldownVisual(Transform transform)
            {
                stack = transform.Find(nameof(stack)).GetComponent<TMP_Text>();
                icon = transform.Find(nameof(icon)).GetComponent<Image>();
                flash = transform.Find(nameof(flash)).GetComponent<CanvasGroup>();
                key = transform.Find(nameof(key)).GetComponent<TMP_Text>();
            }
        }
    }
}
