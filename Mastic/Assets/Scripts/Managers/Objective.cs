using UnityEngine;
using Mirror;

namespace Mastic
{
    public class Objective : MonoBehaviour
    {
        [SerializeField] private float interval = default;
        [SerializeField] private float speed = default;
        [SerializeField] private float maxMoveDistance = default;
        [SerializeField] private float radius = default;
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private bool team = default;
        private Vector3 startingPoint;
        private bool movingForward;
        private bool reachedTarget;
        private Timer timer;

        private void Awake()
        {
            timer = new Timer(interval);
        }

        private void Start()
        {
            startingPoint = transform.position;
        }

        private void Update()
        {
            if (reachedTarget)
                return;
            
            if (timer.Tick(Time.deltaTime))
                movingForward = CheckContest();
        }

        private bool CheckContest()
        {
            var colliders = Physics.OverlapSphere(transform.position, radius, mask, QueryTriggerInteraction.Ignore);
            foreach (var coll in colliders)
            {
                if (!coll.transform.root.CompareTag("Player"))
                    continue;

                if (!coll.transform.root.TryGetComponent(out NetworkIdentity identity))
                    continue;

                if (identity.netId % 2 == 0 == team)
                    return true;
            }

            return false;
        }

        private void FixedUpdate()
        {
            if (reachedTarget)
                return;

            if (movingForward)
                transform.Translate(Time.fixedDeltaTime * speed * transform.forward, Space.World);

            if (Vector3.Distance(startingPoint, transform.position) > maxMoveDistance)
                reachedTarget = true;
        }
    }
}
