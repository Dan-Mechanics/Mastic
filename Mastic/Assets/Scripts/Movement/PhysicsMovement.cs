using System.Collections.Generic;
using UnityEngine;

namespace Mastic
{
    public class PhysicsMovement : MonoBehaviour
    {
        public bool isSmiting;
        public bool isStunned;
        
        public float SpeedCap => speedCap;
        public SimpleMovementAbility JumpAbility => jumpAbility;
        public SimpleMovementAbility DashAbility => dashAbility;

        [Header("References")]

        [SerializeField] private Rigidbody rb = null;
        [SerializeField] private Transform eyes = null;
        [SerializeField] private NetworkPhysicsMovement networkPhysicsMovement = null;
        [SerializeField] private Smite smite = null;

        [Header("Movement Settings")]

        [SerializeField] private float walkingSpeed = 0f;
        [SerializeField] private float acceleration = 0f;
        [SerializeField] private float accelerationMult = 0f;
        [SerializeField] private float speedCap = 0f;

        [Header("Grounded Settings")]

        [SerializeField] private LayerMask groundMask = 0;
        [SerializeField] private float groundColliderRadius = 0f;
        [SerializeField] private float groundColliderDownward = 0f;
        [SerializeField] private float maxGroundSurfaceAngle = 0f;

        [Header("Magic Settings")]

        [SerializeField] private SimpleMovementAbility jumpAbility = null;
        [SerializeField] private SimpleMovementAbility dashAbility = null;
        //[SerializeField] private SimpleMovementAbility smiteAbility = null;

        private void Awake()
        {
            jumpAbility.OnPerform += Jump;
            dashAbility.OnPerform += Dash;
            //smiteAbility.OnPerform += smite.TrySmite;
        }

        private void Start()
        {
            rb.sleepThreshold = 0f;
        }

        private void Dash()
        {
            Vector3 velAdd = eyes.forward * dashAbility.Speed;
            if (velAdd.y >= 0f && rb.linearVelocity.y < 0f) { velAdd.y -= rb.linearVelocity.y; }

            rb.AddForce(velAdd, ForceMode.VelocityChange);
        }

        private void Jump()
        {
            Vector3 velAdd = Vector3.up * jumpAbility.Speed;
            if (rb.linearVelocity.y < 0f) { velAdd.y -= rb.linearVelocity.y; }

            rb.AddForce(velAdd, ForceMode.VelocityChange);
        }

        public void Move(float deltaTime, NetworkPhysicsMovement.InputMessage input)
        {
            bool isGrounded = CheckGround();

            float accel = isGrounded ? acceleration : acceleration * accelerationMult;

            Vector3 movement = Vector3.zero;

            if (!isStunned) 
            {
                movement = transform.right * input.CalculateHorizontalInput() + transform.forward * input.CalculateVerticalInput();
                movement.Normalize();
            }

            if (!isSmiting) { rb.AddForce(Physics.gravity, ForceMode.Acceleration); }

            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;

            float mag = velocity.magnitude;

            if (mag < walkingSpeed)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * deltaTime * movement, walkingSpeed - mag), ForceMode.VelocityChange);
            }
            else if (isGrounded)
            {
                rb.AddForce(Vector3.ClampMagnitude(accel * deltaTime * -velocity.normalized, mag - walkingSpeed), ForceMode.VelocityChange);
            }

            Vector3 counterMovement = accel * deltaTime * accelerationMult * -(velocity.normalized - movement);

            if (mag != 0f && counterMovement.magnitude > mag) { counterMovement = -velocity; }

            rb.AddForce(counterMovement, ForceMode.VelocityChange);
            
            if (isGrounded) 
            {
                jumpAbility.Try(networkPhysicsMovement, input.tick);
            }

            dashAbility.Try(networkPhysicsMovement, input.tick);
        }

        public void Teleport(Vector3 position, Vector3 velocity)
        {
            transform.position = position;
            rb.linearVelocity = velocity;
        }

        private bool CheckGround()
        {
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, groundColliderRadius, Vector3.down, groundColliderDownward, groundMask, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                if (hit.distance == 0f) { continue; }

                if (Vector3.Angle(Vector3.up, hit.normal) <= maxGroundSurfaceAngle)
                {
                    return true;
                }
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
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position + (Vector3.down * groundColliderDownward), groundColliderRadius);
        }
    }
}