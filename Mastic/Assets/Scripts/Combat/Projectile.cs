using UnityEngine;
using Mirror;

namespace Mastic
{
    public class Projectile : MonoBehaviour
    {
        private float damage;
        
        public void Initialize(Vector3 velocity, float radius, bool hasGravity, Collider sender, float damage, float lifetime)
        {
            this.damage = damage;
            Physics.IgnoreCollision(GetComponent<Collider>(), sender);
            transform.localScale = 2f * radius * Vector3.one;
            transform.forward = velocity.normalized;
            Rigidbody rb = GetComponent<Rigidbody>();
            rb.useGravity = hasGravity;
            rb.constraints = hasGravity ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.angularDamping = 0f;
            rb.linearDamping = 0f;
            rb.AddForce(velocity, ForceMode.VelocityChange);
            Destroy(gameObject, lifetime);
        }

        [ServerCallback]
        private void OnCollisionEnter(Collision collision)
        {
            Destroy(gameObject);
            if (collision.transform.root.TryGetComponent(out IDamagable damagable))
                damagable.Damage(damage);
        }
    }
}