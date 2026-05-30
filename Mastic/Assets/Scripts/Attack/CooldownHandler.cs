using Mirror;
using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class CooldownHandler : NetworkBehaviour
    {
        [SerializeField] private string[] cooldownNames = default;
        private Dictionary<string, Cooldown> nameToCooldown;
        private Cooldown[] cooldowns;

        public void Initialize()
        {
            EasySettings easySettings = EasySettings.Current;
            int standardTickrate = easySettings.Get<int>(nameof(standardTickrate));
            nameToCooldown = new Dictionary<string, Cooldown>();
            cooldowns = new Cooldown[cooldownNames.Length];
            for (int i = 0; i < cooldowns.Length; i++)
            {
                string cooldownName = cooldownNames[i];
                int stack = easySettings.Get<int>(cooldownName + nameof(stack));
                float cooldown = easySettings.Get<int>(cooldownName + nameof(cooldown));

                int minTicks = Mathf.CeilToInt(cooldown * standardTickrate);
                int maxTicks = minTicks * stack;
                Cooldown newCooldown = new Cooldown(minTicks, maxTicks);
                cooldowns[i] = newCooldown;
                nameToCooldown.Add(cooldownName, newCooldown);
            }
        }

        public bool CanCast(string cooldownName)
        {
            if (!nameToCooldown.ContainsKey(cooldownName))
                return false;

            Cooldown cooldown = nameToCooldown[cooldownName];
            return cooldown.ticks >= cooldown.minTicksRequired;
        }
        
        /// <summary>
        /// Make sure to also locally predict this change.
        /// </summary>
        public void Cast(string cooldownName)
        {
            if (!nameToCooldown.ContainsKey(cooldownName))
                return;

            Cooldown cooldown = nameToCooldown[cooldownName];
            cooldown.ticks -= cooldown.minTicksRequired;
            cooldown.Clamp();
        }

        /// <summary>
        /// TODO: hook this to respawn Action.
        /// </summary>
        [Server]
        public void RechargeAll()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].ticks = cooldowns[i].maxTicksAllowed;
            }

            TargetRechargeAll(connectionToClient);
        }

        public void Charge()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].ticks++;
                cooldowns[i].Clamp();
            }
        }

        [TargetRpc]
        private void TargetRechargeAll(NetworkConnectionToClient conn)
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].ticks = cooldowns[i].maxTicksAllowed;
            }
        }

        private class Cooldown
        {
            public int ticks;
            public int minTicksRequired;
            public int maxTicksAllowed;

            public Cooldown(int minTicksRequired, int maxTicksAllowed)
            {
                this.minTicksRequired = minTicksRequired;
                this.maxTicksAllowed = maxTicksAllowed;
            }

            public void Clamp() => ticks = Mathf.Clamp(ticks, 0, maxTicksAllowed);
        }
    }
}
