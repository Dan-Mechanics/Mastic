using UnityEngine;

namespace Mastic
{
    public class ForceZone : MonoBehaviour
    {
        [SerializeField] private LayerMask mask = default;
        private Collider[] colliders;
        private Vector3 halfExtents;
        private Vector3 force;

        public void Initialize(int expectedColliders, Vector3 force)
        {
            this.force = force;
            colliders = new Collider[expectedColliders];
            halfExtents = transform.localScale / 2f;
        }

        public void DoTick()
        {
            int count = Physics.OverlapBoxNonAlloc(transform.position, halfExtents, colliders, transform.rotation, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].transform.root.TryGetComponent(out NetworkMovement networkMovement))
                    networkMovement.AddForce(force * Time.fixedDeltaTime);
            }
        }
    }
}
