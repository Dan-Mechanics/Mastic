using UnityEngine;

namespace Mastic
{
    public class FireStrikeBehaviour : MonoBehaviour
    {
        [SerializeField] private float damage = 0f;
        [SerializeField] private Rigidbody rb = null;

        private Weapon weapon;

        private bool destroyed;
        private bool isServer;

        private void Start()
        {
            Destroy(gameObject, 2.5f);
        }

        public void Setup(Weapon weapon, bool isServer) 
        {
            this.weapon = weapon;
            this.isServer = isServer;
        }

        /// <summary>
        /// If the projectile spawns inside a player, idk if it still works.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (destroyed) { return; }

            destroyed = true;

            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true; // not certain about this.

            Destroy(gameObject);

            if (!isServer) { return; }

            if (!collision.transform.CompareTag("Player")) { return; }

            collision.transform.GetComponent<PlayerHealth>().Damage(damage);
            
            weapon.TargetCallDamage(weapon.connectionToClient, damage);
        }
    }
}