using UnityEngine;

namespace Mastic
{
    public class Projectile : MonoBehaviour
    {
        private float damage;
        private float lifetime;
        private bool isServer;

        public void Initialize(string name, bool isServer)
        {
            this.isServer = isServer;
            gameObject.name = name;
            EasySettings easySettings = EasySettings.Current;
            damage = easySettings.Get<float>(name + nameof(damage));
            lifetime = easySettings.Get<float>(name + nameof(lifetime));
        }

        private void Start() => Destroy(gameObject, lifetime);

        private void OnCollisionEnter(Collision collision)
        {
            Destroy(gameObject);
            if (!isServer)
                return;

            if (collision.transform.root.TryGetComponent(out IDamagable damagable))
                damagable.Damage(damage);
        }
    }
}