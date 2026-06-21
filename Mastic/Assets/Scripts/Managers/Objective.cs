using UnityEngine;
using Mirror;

namespace Mastic
{
    public class Objective : NetworkBehaviour
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
            => timer = new Timer(interval);

        private void Start()
            => startingPoint = transform.position;

        public override void OnStartServer()
        {
            base.OnStartServer();
            GetComponent<EnvironmentEntity>().Initialize(FindAnyObjectByType<LagCompensation>());
        }

        private void Update()
        {
            if (!isServer)
                return;

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
                var player = coll.transform.root;
                if (!player.CompareTag("Player"))
                    continue;

                if (!player.TryGetComponent(out NetworkIdentity identity))
                    continue;

                if (identity.netId % 2 == 0 == team)
                    return true;
            }

            return false;
        }

        private void FixedUpdate()
        {
            if (!isServer)
                return;
            
            if (reachedTarget)
                return;

            if (movingForward)
                transform.Translate(Time.fixedDeltaTime * speed * transform.forward, Space.World);

            if (Vector3.Distance(startingPoint, transform.position) > maxMoveDistance)
                reachedTarget = true;
        }
    }
}
