using Mirror;
using UnityEngine;

namespace Mastic
{
    public class GlobalHealingPassive : MonoBehaviour
    {
        private float healingPerSecond;
        private IHealable healable;

        private void Awake()
        {
            healingPerSecond = EasySettings.Current.Get<float>(nameof(healingPerSecond));
            healable = GetComponent<IHealable>();
        }

        [ServerCallback]
        private void FixedUpdate() 
            => healable.Heal(healingPerSecond * Time.fixedDeltaTime);
    }
}