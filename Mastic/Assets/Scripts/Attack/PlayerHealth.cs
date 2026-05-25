using Mirror;
using System;

namespace Mastic
{
    public interface IDamagable
    {
        void Damage(float amount);
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
        public void Damage(float amount)
        {
            if (amount <= 0f)
                return;

            health -= amount;
            if (health <= 0f)
                Die();
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