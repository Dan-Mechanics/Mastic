using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public interface IMovement
    {
        bool GetIsGrounded();
        void Move(float interval, InputMessage inputMessage);
        void Teleport(StateMessage stateMessage);
    }
    
    /// <summary>
    /// In theory I would like to make this an interface.
    /// </summary>
    public class PhysicsMovement : MonoBehaviour, IMovement
    {
        public bool isSmiting;
        public bool isStunned;
        
        public float SpeedLimit => speedLimit;
        public SimpleMovementAbility JumpAbility => jumpAbility;
        public SimpleMovementAbility DashAbility => dashAbility;

        [Header("References")]

        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private Transform eyes = null;
        [SerializeField] private NetworkMovement networkPhysicsMovement = null;
       //  [SerializeField] private Smite smite = null;

        [Header("Movement Settings")]
        [SerializeField] private float walkingSpeed = 0f;
        [SerializeField] private float acceleration = 0f;
        [SerializeField] private float multiplier = 0f;
        [SerializeField] private float speedLimit = 0f;

        [Header("Grounded Settings")]
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private float radius = default;
        [SerializeField] private float offset = default;
        [SerializeField] private float slopeLimit = default;

        [Header("Magic Settings")]

        [SerializeField] private SimpleMovementAbility jumpAbility = null;
        [SerializeField] private SimpleMovementAbility dashAbility = null;
        //[SerializeField] private SimpleMovementAbility smiteAbility = null;

        public void Setup()
        {
            rb.sleepThreshold = 0f;
            jumpAbility.OnPerform += Jump;
            dashAbility.OnPerform += Dash;
        }

        private void Dash()
        {
            Vector3 force = eyes.forward * dashAbility.Speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        private void Jump()
        {
            Vector3 force = Vector3.up * jumpAbility.Speed;
            if (rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        public void LimitSpeed() => rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, speedLimit);

        public void Move(float interval, InputMessage input)
        {
            bool isGrounded = GetIsGrounded();

            float accel = isGrounded ? acceleration : acceleration * multiplier;

            Vector3 movement = Vector3.zero;

            if (!isStunned) 
            {
                movement = transform.right * input.GetHorizontalInput() + transform.forward * input.GetVerticalInput();
                movement.Normalize();
            }

            if (!isSmiting) { rb.AddForce(Physics.gravity, ForceMode.Acceleration); }

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;

            float mag = velocity.magnitude;

            if (mag < walkingSpeed)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * interval * movement, walkingSpeed - mag), ForceMode.VelocityChange);
            }
            else if (isGrounded)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * interval * -velocity.normalized, mag - walkingSpeed), ForceMode.VelocityChange);
            }

            Vector3 counterMovement = accel * interval * multiplier * -(velocity.normalized - movement);

            if (mag != 0f && counterMovement.magnitude > mag) { counterMovement = -velocity; }

            rb.AddForce(counterMovement, ForceMode.VelocityChange);
            
            if (isGrounded) 
            {
                jumpAbility.Try(networkPhysicsMovement, input.tick);
            }

            dashAbility.Try(networkPhysicsMovement, input.tick);
        }

        public void Teleport(StateMessage stateMessage)
        {
            transform.position = stateMessage.position;
            rb.linearVelocity = stateMessage.velocity;
        }

        public bool GetIsGrounded()
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

        public void CleanTicks(int tick) 
        {
            jumpAbility.CleanRequests(tick);
            dashAbility.CleanRequests(tick);
        }

        private void OnDrawGizmosSelected()
        {
            Color color = Color.Lerp(Color.green, Color.white, 0.5f);
            color.a = 0.5f;
            Gizmos.color = color;
            Gizmos.DrawSphere(transform.position + (Vector3.down * offset), radius);
        }
    }
}