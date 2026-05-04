using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Mastic
{
    public interface IMovable
    {
        void Move(float vert, float hori, float interval);
    }

    public interface IMovementAbility
    {
        bool DoTick(int tick);
    }

    public class BurningWings : NetworkBehaviour, IMovable, IMovementAbility
    {
        public int tickDuration;
        public float speed;

        private NetworkMovement networkMovement;
        private CooldownHandler cooldownHandler;
        private Rigidbody rb;
        private Transform eyes;
        private int startingTick;

        private void Awake()
        {
            networkMovement = GetComponent<NetworkMovement>();
            cooldownHandler = GetComponent<CooldownHandler>();
            rb = GetComponent<Rigidbody>();
            eyes = transform.Find("eyes");
        }


        public bool DoTick(int tick)
        {
            throw new System.NotImplementedException();
        }

        public void Move(float vert, float hori, float interval)
        {
            rb.linearVelocity = eyes.forward * speed;
        }
    }
}
