using UnityEngine;

namespace Mastic
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb = default;
        [SerializeField] private Collider coll = default;
        private float damage;
        private bool isServer;
        
        public void Initialize(Vector3 velocityChange, string name, Collider sender, bool isServer, float damage, float lifetime)
        {
            this.damage = damage;
            this.isServer = isServer;
            Physics.IgnoreCollision(sender, coll);
            gameObject.name = name;
            transform.forward = velocityChange.normalized;
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
            Destroy(gameObject, lifetime);
        }

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