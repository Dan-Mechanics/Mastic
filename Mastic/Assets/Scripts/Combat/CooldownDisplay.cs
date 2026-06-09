using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class CooldownDisplay : NetworkBehaviour, ICooldownsRequired
    {
        [SerializeField] private Transform cooldownHolder = default;
        private CooldownHandler cooldownHandler;
        private CooldownVisual[] visuals;
        private Cooldown[] cooldowns;

        public void AssignCooldowns(Cooldown[] cooldowns, CooldownHandler cooldownHandler)
        {
            this.cooldowns = cooldowns;
            this.cooldownHandler = cooldownHandler;
            visuals = new CooldownVisual[cooldowns.Length];
            for (int i = 0; i < cooldownHolder.childCount; i++)
            {
                visuals[i] = new CooldownVisual(cooldownHolder.GetChild(i));
                visuals[i].icon.sprite = cooldowns[i].icon;
            }
        }

        private void FixedUpdate()
        {
            if (!isLocalPlayer)
                return;

            for (int i = 0; i < cooldowns.Length; i++)
            {
              // (int currentStack, float remainderPercentage) 
              //     = cooldownHandler.GetCooldownStatus(cooldowns[i].name);
              //
              // visuals[i].stack.text = Utils.GetRoman(currentStack);
              // visuals[i].icon.fillAmount = remainderPercentage;
              // visuals[i].icon.color = currentStack > 0 ? Color.white : Color.gray;
            }
        }

        private class CooldownVisual
        {
            public TMP_Text stack;
            public Image icon;

            public CooldownVisual(Transform transform)
            {
                stack = transform.Find(nameof(stack)).GetComponent<TMP_Text>();
                icon = transform.Find(nameof(icon)).GetComponent<Image>();
            }
        }
    }
}
