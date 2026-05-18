using UnityEngine;

namespace Mastic
{
    public class ForceZone : MonoBehaviour
    {
        [SerializeField] private Vector3 force = default;
        [SerializeField] private LayerMask mask = default;
        private Collider[] colliders;
        private Vector3 halfExtents;

        public void Initialize(int expectedRigidbodies, Vector3 force)
        {
            this.force = force;
            colliders = new Collider[expectedRigidbodies];
            halfExtents = transform.localScale / 2f;
        }

        public void DoTick()
        {
            int count = Physics.OverlapBoxNonAlloc(transform.position, halfExtents, colliders, transform.rotation, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!colliders[i].transform.root.TryGetComponent(out Rigidbody rb))
                    continue;

                rb.AddForce(force, ForceMode.Acceleration);
            }
        }
    }
}
