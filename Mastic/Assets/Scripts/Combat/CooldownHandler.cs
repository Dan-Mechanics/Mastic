using Mirror;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mastic
{
    public class CooldownHandler : NetworkBehaviour
    {
        [SerializeField] private Cooldown[] registeredCooldowns = default;
        private Dictionary<string, CooldownValue> nameToCooldown;

        public void Initialize()
        {
            EasySettings easySettings = EasySettings.Current;
            int standardTickrate = easySettings.Get<int>(nameof(standardTickrate));
            nameToCooldown = new Dictionary<string, CooldownValue>();
            for (int i = 0; i < registeredCooldowns.Length; i++)
            {
                string name = registeredCooldowns[i].name.ToLowerInvariant();
                int stack = easySettings.Get<int>(name + nameof(stack));
                float cooldown = easySettings.Get<float>(name + nameof(cooldown));

                int minTicks = Mathf.CeilToInt(cooldown * standardTickrate);
                int maxTicks = minTicks * stack;
                nameToCooldown.Add(name, new CooldownValue(minTicks, maxTicks));
            }

            ICooldownsRequired[] subscribers = GetComponents<ICooldownsRequired>();
            subscribers.ToList().ForEach(x => x.AssignCooldowns(registeredCooldowns, this));
        }

        public bool CanCast(string name)
        {
            if (!nameToCooldown.ContainsKey(name))
                return false;

            return nameToCooldown[name].CanCast();
        }

        public (int, float) GetCooldownStatus(string name)
        {
            if (!nameToCooldown.ContainsKey(name))
                return (0, 0f);

            CooldownValue cooldown = nameToCooldown[name];
            int currentStack = Mathf.FloorToInt((float)cooldown.ticks / cooldown.minTicksRequired);
            float remainderPercentage = (float)(cooldown.ticks - currentStack * cooldown.minTicksRequired) / cooldown.minTicksRequired;
            if (cooldown.ticks >= cooldown.maxTicksAllowed)
                remainderPercentage = 1f;

            return (currentStack, remainderPercentage);
        }
        
        /// <summary>
        /// Make sure to also locally predict this change.
        /// </summary>
        public void Cast(string name)
        {
            if (nameToCooldown.ContainsKey(name))
                nameToCooldown[name].Cast();
        }

        /// <summary>
        /// This is called when player respawns.
        /// </summary>
        [Server]
        public void RechargeAll()
        {
            nameToCooldown.Values.ToList().ForEach(x => x.Recharge());
            TargetRechargeAll(connectionToClient);
        }

        public void Charge() 
            => nameToCooldown.Values.ToList().ForEach(x => x.Charge());

        [TargetRpc]
        private void TargetRechargeAll(NetworkConnectionToClient conn) 
            => nameToCooldown.Values.ToList().ForEach(x => x.Recharge());

        private class CooldownValue
        {
            public int ticks;
            public readonly int minTicksRequired;
            public readonly int maxTicksAllowed;

            public CooldownValue(int minTicksRequired, int maxTicksAllowed)
            {
                if (minTicksRequired <= 0)
                    minTicksRequired = 1;

                if (maxTicksAllowed <= 0)
                    maxTicksAllowed = 1;

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
