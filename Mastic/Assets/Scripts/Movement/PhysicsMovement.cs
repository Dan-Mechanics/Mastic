using UnityEngine;

namespace Mastic
{
    public class PhysicsMovement : MonoBehaviour, IMovement
    {
        [Header("Settings")]
        [SerializeField] private float speed = default;
        [SerializeField] private float acceleration = default;
        [SerializeField] private float multiplier = default;
        [SerializeField] private float topSpeed = default;

        [Header("Grounded Settings")]
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private float radius = default;
        [SerializeField] private float offset = default;
        [SerializeField] private float slopeLimit = default;

        private Rigidbody rb;
        private bool controllable;
        private bool hasGravity;

        public void Initialize()
        {
            rb = GetComponent<Rigidbody>();
            rb.sleepThreshold = 0f;
            rb.useGravity = false;
            EnableGravity(true);
            EnableControl(true);
        }

        public void EnableGravity(bool hasGravity) => this.hasGravity = hasGravity;
        public void EnableControl(bool controllable) => this.controllable = controllable;
        public void LimitSpeed() => rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, topSpeed);
        public void AddForce(Vector3 velocityChange) => rb.AddForce(velocityChange, ForceMode.VelocityChange);

        public void Move(float vertical, float horizontal, float interval)
        {
            if (!controllable)
            {
                vertical = 0f;
                horizontal = 0f;
            }
            
            bool isGrounded = GetIsGrounded();
            float currAccel = isGrounded ? acceleration : acceleration * multiplier;

            Vector3 movement = (transform.forward * vertical) + (transform.right * horizontal);
            movement.Normalize();

            if (hasGravity)
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;

            float magnitude = velocity.magnitude;
            if (magnitude < speed)
            {
                movement = Vector3.ClampMagnitude(currAccel * interval * movement, speed - magnitude);
            }
            else if (isGrounded)
            {
                movement = Vector3.ClampMagnitude(currAccel * interval * -velocity.normalized, magnitude - speed);
            }

            rb.AddForce(movement, ForceMode.VelocityChange);

            Vector3 counterMovement = currAccel * interval * multiplier * -(velocity.normalized - movement);
            if (magnitude != 0f && counterMovement.magnitude > magnitude)
                counterMovement = -velocity;

            rb.AddForce(counterMovement, ForceMode.VelocityChange);
        }

        private bool GetIsGrounded()
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius,
                Vector3.down, offset, mask, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.distance > 0f && Vector3.Angle(Vector3.up, hit.normal) <= slopeLimit)
                    return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            Color color = Color.Lerp(Color.green, Color.white, 0.5f);
            color.a = 0.3f;
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position + (Vector3.down * offset), radius);
        }
    }
}