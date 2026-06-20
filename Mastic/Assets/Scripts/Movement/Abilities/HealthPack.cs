using UnityEngine;

namespace Mastic
{
    public class HealthPack : MonoBehaviour
    {
        [SerializeField] private float healing = default;
        private bool isDestroyed;
        private bool isServer;

        public void Initialize(bool isServer)
            => this.isServer = isServer;

        private void OnTriggerEnter(Collider other)
        {
            if (isDestroyed)
                return;

            if (isServer && other.transform.root.TryGetComponent(out IHealable healable))
                healable.Heal(healing);

            isDestroyed = true;
            Destroy(gameObject);
        }
    }
}
