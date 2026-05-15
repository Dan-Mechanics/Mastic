using UnityEngine;
using Mirror;
using UnityEngine.UI;

namespace Mastic
{
    public interface IDamagable
    {
        void Damage(float amount);
    }
    
    /// <summary>
    /// Note to self: while respawning, all bullet will reconsile because the local client
    /// cannot predict WHEN he will die. This makes sense because you cannot shoot bullets while you are dead
    /// and this is one of many "acceptable reconsile noregs".
    /// </summary>
    public class PlayerHealth : NetworkBehaviour, IDamagable
    {
        [SerializeField] private CharacterController controller = null;
        //[SerializeField] private Transform respawn = null; // Add rotation to respawn ??
        [SerializeField] private Image healthBar = null;
        [SerializeField] private Text healthText = null;

        public const float MAX_HEALTH = 100f;

        [SyncVar] [SerializeField] private float health = 0f;

        private Transform respawn;

        private void Awake()
        {
            respawn = GameObject.FindWithTag("Respawn").transform;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            health = MAX_HEALTH;
        }

        private void FixedUpdate()
        {
            if (isLocalPlayer) 
            {
                healthBar.fillAmount = health / MAX_HEALTH;
                healthText.text = Mathf.Ceil(health).ToString(); // because if you have 0.5 health your are still alive so ur 1 hp.
            }
        }

        [Server]
        public void Damage(float amoumt) 
        {
            if(amoumt <= 0f) { return; }

            health -= amoumt;

            if (health <= 0f) 
            {
                health = MAX_HEALTH;

                controller.enabled = false;
                transform.position = respawn.position;
                controller.enabled = true;

                Debug.Log($"{gameObject.name} died!");
            }
        }
    }
}