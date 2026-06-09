using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class CooldownHandler : NetworkBehaviour
    {
        [SerializeField] private string[] cooldownNames = default;
        private Dictionary<string, Cooldown> cooldowns;

        public void Initialize()
        {
            EasySettings easySettings = EasySettings.Current;
            int standardTickrate = easySettings.Get<int>(nameof(standardTickrate));
            cooldowns = new Dictionary<string, Cooldown>();
            for (int i = 0; i < cooldownNames.Length; i++)
            {
                string cooldownName = cooldownNames[i].ToLowerInvariant();
                int stack = easySettings.Get<int>(cooldownName + nameof(stack));
                float cooldown = easySettings.Get<float>(cooldownName + nameof(cooldown));

                int minTicks = Mathf.CeilToInt(cooldown * standardTickrate);
                int maxTicks = minTicks * stack;
                cooldowns.Add(cooldownName, new Cooldown(minTicks, maxTicks));
            }
        }

        public bool CanCast(string cooldownName)
        {
            cooldownName = cooldownName.ToLowerInvariant();
            if (cooldowns.ContainsKey(cooldownName))
            {
                return cooldowns[cooldownName].CanCast();
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Make sure to also locally predict this change.
        /// </summary>
        public void Cast(string cooldownName)
        {
            cooldownName = cooldownName.ToLowerInvariant();
            if (cooldowns.ContainsKey(cooldownName))
                cooldowns[cooldownName].Cast();
        }

        /// <summary>
        /// This is called when player respawns.
        /// </summary>
        [Server]
        public void RechargeAll()
        {
            cooldowns.Values.ToList().ForEach(x => x.Recharge());
            TargetRechargeAll(connectionToClient);
        }

        public void Charge()
        {
            cooldowns.Values.ToList().ForEach(x => x.Charge());
        }

        [TargetRpc]
        private void TargetRechargeAll(NetworkConnectionToClient conn)
        {
            cooldowns.Values.ToList().ForEach(x => x.Recharge());
        }

        private class Cooldown
        {
            private int ticks;
            private readonly int minTicksRequired;
            private readonly int maxTicksAllowed;

            public Cooldown(int minTicksRequired, int maxTicksAllowed)
            {
                this.minTicksRequired = minTicksRequired;
                this.maxTicksAllowed = maxTicksAllowed;
            }

            public void Charge()
            {
                ticks++;
                Clamp();
            }

            public void Cast()
            {
                ticks -= minTicksRequired;
                Clamp();
            }

            public bool CanCast() => ticks >= minTicksRequired;
            public void Recharge() => ticks = maxTicksAllowed;
            private void Clamp() => ticks = Mathf.Clamp(ticks, 0, maxTicksAllowed);
        }
    }
}
