using UnityEngine;

namespace Mastic
{
    public class PhysicsMovement : MonoBehaviour, IMovement
    {
        public bool IsGrounded => isGrounded;
        
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

        private bool isGrounded;
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
            isGrounded = GetIsGrounded();
            float accel = isGrounded ? acceleration : acceleration * multiplier;

            Vector3 movement = Vector3.zero;
            if (controllable)
            {
                movement = (transform.forward * vertical) + (transform.right * horizontal);
                movement.Normalize();
            }

            if (hasGravity)
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;

            float mag = velocity.magnitude;
            if (mag < speed)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * interval * movement, speed - mag), ForceMode.VelocityChange);
            }
            else if (isGrounded)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * interval * -velocity.normalized, mag - speed), ForceMode.VelocityChange);
            }

            Vector3 counterMovement = accel * interval * multiplier * -(velocity.normalized - movement);

            if (mag != 0f && counterMovement.magnitude > mag)
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