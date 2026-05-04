using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    /// <summary>
    /// TODO: make a good interface here.
    /// </summary>
    public class PhysicsMovement : MonoBehaviour
    {
        public MovementAbility JumpAbility => jumpAbility;
        public MovementAbility DashAbility => dashAbility;

        [Header("References")]
        [SerializeField] private Rigidbody rb = default;
        [SerializeField] private Transform eyes = default;

        // REMOVE THIS !!
        [SerializeField] private NetworkMovement networkPhysicsMovement = default;

        [Header("Movement Settings")]
        [SerializeField] private float speed = default;
        [SerializeField] private float acceleration = default;
        [SerializeField] private float multiplier = default;
        [SerializeField] private float topSpeed = default;

        [Header("Grounded Settings")]
        [SerializeField] private LayerMask mask = default;
        [SerializeField] private float radius = default;
        [SerializeField] private float offset = default;
        [SerializeField] private float slopeLimit = default;

        [Header("Magic Settings")]
        [SerializeField] private MovementAbility jumpAbility = default;
        [SerializeField] private MovementAbility dashAbility = default;
        private bool controllable;
        private bool hasGravity;

        public void Setup()
        {
            rb.sleepThreshold = 0f;
            EnableGravity(true);
            EnableControl(true);
            jumpAbility.OnCast += Jump;
            dashAbility.OnCast += Dash;
        }

        public void EnableGravity(bool hasGravity) => this.hasGravity = hasGravity;
        public void EnableControl(bool controllable) => this.controllable = controllable;

        private void Dash()
        {
            Vector3 force = eyes.forward * dashAbility.speed;
            if (force.y >= 0f && rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        private void Jump()
        {
            Vector3 force = Vector3.up * jumpAbility.speed;
            if (rb.linearVelocity.y < 0f)
                force.y -= rb.linearVelocity.y;

            rb.AddForce(force, ForceMode.VelocityChange);
        }

        /// <summary>
        /// Without this the max speed of the player
        /// isn't deterministic and that causes bad reconsiles.
        /// </summary>
        public void LimitSpeed() => rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, topSpeed);
        
        /// <summary>
        /// Todo: remove tick here !!.
        /// </summary>
        public void Move(float vertical, float horizontal, float interval, int tick)
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

            Vector3 velocity = Flatten(rb.linearVelocity);
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
            
            // MAGIC ====

            if (isGrounded)
                jumpAbility.CastOnTick(tick, networkPhysicsMovement);

            dashAbility.CastOnTick(tick, networkPhysicsMovement);
        }

        public void Teleport(Vector3 position, Vector3 velocity)
        {
            transform.position = position;
            rb.linearVelocity = velocity;
        }

        private Vector3 Flatten(Vector3 vec)
        {
            vec.y = 0f;
            return vec;
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