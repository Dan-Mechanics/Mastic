using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public class BurningWings : NetworkBehaviour, IMovement
    {
        public int tickDuration;
        public float speed;

        private NetworkMovement networkMovement;
        private CooldownHandler cooldownHandler;
        private Rigidbody rb;
        private Transform eyes;
        private int startingTick;

        public bool IsGrounded => false;

        private void Awake()
        {
            networkMovement = GetComponent<NetworkMovement>();
            cooldownHandler = GetComponent<CooldownHandler>();
            rb = GetComponent<Rigidbody>();
            eyes = transform.Find("eyes");
        }


        public bool DoLocalTick(int tick)
        {
            throw new System.NotImplementedException();
        }

        public void Move(float vert, float hori, float interval) => rb.linearVelocity = eyes.forward * speed;
        public void LimitSpeed() { }
        public void AddForce(Vector3 velocityChange) { }
    }
}
