using UnityEngine;

namespace Mastic
{
    public class PhysicsMovement : MonoBehaviour, IMovement
    {
        public byte Index { get; set; }
        public bool IsGrounded => isGrounded;

        [SerializeField] private MovementSettings settings = default;
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

        public void Move(float vert, float hori, float interval)
        {
            isGrounded = GetIsGrounded();
            float accel = isGrounded ? settings.acceleration : settings.acceleration * settings.multiplier;

            Vector3 movement = Vector3.zero;
            if (controllable)
            {
                movement = (transform.forward * vert) + (transform.right * hori);
                movement.Normalize();
            }

            if (hasGravity)
                rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            Vector3 vel = Utils.Flatten(rb.linearVelocity);
            float mag = vel.magnitude;
            if (mag < settings.speed)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * interval * movement, settings.speed - mag), ForceMode.VelocityChange);
            }
            else if (isGrounded)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * interval * -vel.normalized, mag - settings.speed), ForceMode.VelocityChange);
            }

            Vector3 counterMovement = accel * interval * settings.multiplier * -(vel.normalized - movement);

            if (mag != 0f && counterMovement.magnitude > mag)
                counterMovement = -vel;

            rb.AddForce(counterMovement, ForceMode.VelocityChange);
        }

        private bool GetIsGrounded()
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, settings.radius,
                Vector3.down, settings.offset, settings.mask, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.distance > 0f && Vector3.Angle(Vector3.up, hit.normal) <= settings.slopeLimit)
                    return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + (Vector3.down * settings.offset), settings.radius);
        }
    }
}