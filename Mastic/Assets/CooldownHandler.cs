using Mirror;
using UnityEngine;

namespace Mastic
{
    public class CooldownHandler : NetworkBehaviour
    {
        [SerializeField] private Cooldown[] cooldowns = default;

        private void FixedUpdate() => ChargeCooldowns(Time.fixedDeltaTime);

        public bool CanCast(int index)
        {
            if (index < 0 || index >= cooldowns.Length)
                return false;

            return cooldowns[index].value >= cooldowns[index].minRequired;
        }
        
        /// <summary>
        /// Make sure to also locally predict this change.
        /// </summary>
        public void Cast(int index)
        {
            if (index < 0 || index >= cooldowns.Length)
                return;

            cooldowns[index].value -= cooldowns[index].minRequired;
            cooldowns[index].Clamp();

            if (isServer)
                TargetSyncCooldown(connectionToClient, index, cooldowns[index].value, NetworkTime.time);
        }

        /// <summary>
        /// Idea: hook this to respawn Action.
        /// </summary>
        [Server]
        public void RechargeCooldownsFully()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].value = cooldowns[i].maxValue;
                TargetSyncCooldown(connectionToClient, i, cooldowns[i].value, NetworkTime.time);
            }
        }

        public void ChargeCooldowns(float interval)
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].value += interval;
                cooldowns[i].Clamp();
            }
        }

        [TargetRpc]
        private void TargetSyncCooldown(NetworkConnectionToClient conn, int index, float cooldown, double sendTime)
        {
            cooldowns[index].value = cooldown + (float)(NetworkTime.time - sendTime);
            cooldowns[index].Clamp();
        }

        [System.Serializable]
        public struct Cooldown
        {
            public float value;
            public float minRequired;
            public float maxValue;

            public void Clamp() => value = Mathf.Clamp(value, 0f, maxValue);
        }
    }
}
