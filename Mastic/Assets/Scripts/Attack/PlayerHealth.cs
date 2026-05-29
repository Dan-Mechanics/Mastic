using Mirror;
using System;

namespace Mastic
{
    public interface IDamagable
    {
        /// <summary>
        /// Future: you could make this return a bool for if killed.
        /// </summary>
        bool Damage(float amount);
        void Die();
    }

    public class PlayerHealth : NetworkBehaviour, IDamagable
    {
        public Action OnDie;
        public Action OnRespawn;
        public Action<float, float> OnClientHealthChanged;

        [SyncVar(hook = nameof(OnHealthChanged))] private float health;
        private float maxHealth;

        public override void OnStartServer()
        {
            base.OnStartServer();
            maxHealth = EasySettings.Current.Get<float>(nameof(maxHealth));
            Respawn();
        }

        [Server]
        public bool Damage(float amount)
        {
            if (amount <= 0f)
                return false;

            health -= amount;
            if (health > 0f)
                return false;

            Die();
            return true;
        }

        [Client]
        private void OnHealthChanged(float oldValue, float newValue) => OnClientHealthChanged?.Invoke(oldValue, newValue);

        [Server]
        public void Die()
        {
            health = 0f;
            OnDie?.Invoke();
            Respawn();
        }

        [Server]
        private void Respawn()
        {
            health = maxHealth;
            OnRespawn?.Invoke();
        }
    }
}