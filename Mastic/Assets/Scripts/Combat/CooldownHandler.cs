using System;
using UnityEngine;

namespace Mastic
{
    public class CooldownHandler : MonoBehaviour
    {
        public event Action<int> OnCast;
        private CooldownValues[] cooldowns;
        private string[] abilities;

        public void Initialize(string[] abilities)
        {
            this.abilities = abilities;
            EasySettings easySettings = EasySettings.Current;
            int standardTickrate = easySettings.Get<int>(nameof(standardTickrate));

            cooldowns = new CooldownValues[abilities.Length];
            for (int i = 0; i < cooldowns.Length; i++)
            {
                string cooldownName = abilities[i];
                int stack = easySettings.Get<int>(cooldownName + nameof(stack));
                float cooldown = easySettings.Get<float>(cooldownName + nameof(cooldown));

                int minTicks = Mathf.CeilToInt(cooldown * standardTickrate);
                int maxTicks = minTicks * stack;
                cooldowns[i] = new CooldownValues(minTicks, maxTicks);
            }
        }

        public int GetIndexFromName(string cooldownName)
        {
            cooldownName = cooldownName.ToLowerInvariant();
            for (int i = 0; i < abilities.Length; i++)
            {
                if (cooldownName == abilities[i])
                    return i;
            }

            return 0;
        }

        public bool CanCast(int index)
        {
            if (index < 0 || index >= cooldowns.Length)
                return false;

            return cooldowns[index].value >= cooldowns[index].minValueRequired;
        }

        public bool GetCooldownStatus(int index, out int stack, out float remainderPercentage)
        {
            stack = 0;
            remainderPercentage = 0f;
            if (index < 0 || index >= cooldowns.Length)
                return false;

            CooldownValues cooldown = cooldowns[index];
            stack = Mathf.FloorToInt((float)cooldown.value / cooldown.minValueRequired);
            remainderPercentage = (float)(cooldown.value - stack * cooldown.minValueRequired) / cooldown.minValueRequired;
            if (cooldown.value >= cooldown.maxValueAllowed)
                remainderPercentage = 1f;

            return true;
        }
        
        /// <summary>
        /// Make sure to also locally predict this change.
        /// </summary>
        public void Cast(int index)
        {
            if (index < 0 || index >= cooldowns.Length)
                return;

            CooldownValues cooldown = cooldowns[index];
            cooldown.value = Mathf.Clamp(cooldown.value - cooldown.minValueRequired, 0, cooldown.maxValueAllowed);
            OnCast?.Invoke(index);
        }

        public void RechargeAll()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].value = cooldowns[i].maxValueAllowed;
            }
        }

        public void Charge()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                CooldownValues cooldown = cooldowns[i];
                cooldown.value = Mathf.Clamp(cooldown.value + 1, 0, cooldown.maxValueAllowed);
            }
        }

        private class CooldownValues
        {
            public int value;
            public readonly int minValueRequired;
            public readonly int maxValueAllowed;

            public CooldownValues(int minValueRequired, int maxValueAllowed)
            {
                if (minValueRequired <= 0)
                    minValueRequired = 1;

                if (maxValueAllowed <= 0)
                    maxValueAllowed = 1;

                if (minValueRequired > maxValueAllowed)
                    (minValueRequired, maxValueAllowed) = (maxValueAllowed, minValueRequired);

                this.minValueRequired = minValueRequired;
                this.maxValueAllowed = maxValueAllowed;
            }
        }
    }
}
