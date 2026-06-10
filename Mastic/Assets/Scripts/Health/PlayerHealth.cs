using Mirror;
using System;
using UnityEngine;

namespace Mastic
{
    public class PlayerHealth : NetworkBehaviour, IDamagable, IHealable
    {
        public Action OnDie;
        public Action OnRespawn;
        public Action<float, float> OnHealthChanged;

        [SyncVar(hook = nameof(OnHealthChangedHook))] private float health;
        private float maxHealth;
        private float respawnDelay;
        private Transform respawn;

        private void Awake()
        {
            respawn = GameObject.FindWithTag("Respawn").transform;
            EasySettings easySettings = EasySettings.Current;
            respawnDelay = easySettings.Get<float>(nameof(respawnDelay));
            maxHealth = easySettings.Get<float>(nameof(maxHealth));
            syncInterval = easySettings.Get<float>(nameof(syncInterval));
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Invoke(nameof(Respawn), respawnDelay);
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

        [ContextMenu(nameof(DoAdminDamage))]
        public void DoAdminDamage() => Damage(maxHealth * 0.5f);

        [Server]
        public void Heal(float amount)
        {
            if (amount <= 0f)
                return;

            health += amount;
            if (health > maxHealth)
                health = maxHealth;
        }

        [Client]
        private void OnHealthChangedHook(float oldValue, float newValue) 
            => OnHealthChanged?.Invoke(oldValue, newValue);

        [Server]
        public void Die()
        {
            health = 0f;
            OnDie?.Invoke();
            RpcDie();
            Respawn();
        }

        [Server]
        public void Respawn()
        {
            health = maxHealth;
            transform.position = respawn.GetChild((int)netId % 2).position;
            OnRespawn?.Invoke();
            RpcRespawn();
        }

        [ClientRpc]
        private void RpcRespawn() => OnRespawn?.Invoke();

        [ClientRpc]
        private void RpcDie() => OnDie?.Invoke();
    }
}