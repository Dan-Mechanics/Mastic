using UnityEngine;

namespace Mastic
{
    [CreateAssetMenu(fileName = nameof(MovementSettings), menuName = nameof(MovementSettings))]
    public class MovementSettings : ScriptableObject
    {
        public float speed;
        public float acceleration;
        public float multiplier;
        public float topSpeed;

        [Header("Grounded Settings")]
        public LayerMask mask;
        public float radius;
        public float offset;
        public float slopeLimit;
    }
}
