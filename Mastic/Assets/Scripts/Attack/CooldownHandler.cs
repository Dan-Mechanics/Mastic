using Mirror;
using UnityEngine;

namespace Mastic
{
    public class CooldownHandler : NetworkBehaviour
    {
        public int Last => cooldowns.Length - 1;
        [SerializeField] private Cooldown[] cooldowns = default;

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
        }

        /// <summary>
        /// TODO: hook this to respawn Action.
        /// </summary>
        [Server]
        public void ChargeAllFully()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].value = cooldowns[i].maxValue;
            }

            TargetChargeAllFully(connectionToClient);
        }

        public void Charge()
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].value++;
                cooldowns[i].Clamp();
            }
        }

        [TargetRpc]
        private void TargetChargeAllFully(NetworkConnectionToClient conn)
        {
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].value = cooldowns[i].maxValue;
            }
        }

        [System.Serializable]
        public struct Cooldown
        {
            public int value;
            public int minRequired;
            public int maxValue;
            //public string debugOutput;

            public void Clamp() => value = Mathf.Clamp(value, 0, maxValue);
        }

        /*protected override void OnValidate()
        {
            base.OnValidate();
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i].debugOutput = $"{cooldowns[i].minRequired / 64f}s / {cooldowns[i].maxValue / 64f}s";
            }
        }*/
    }
}
