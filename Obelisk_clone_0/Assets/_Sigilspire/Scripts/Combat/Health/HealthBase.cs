using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

namespace Combat.Health
{
    public class HealthBase : NetworkBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 100f;
        public float MaxHealth => maxHealth;
        public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>();

        protected virtual void Start()
        {
            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
            }
        }

        public virtual void TakeDamage(float amount)
        {
            if (!IsServer) return;

            CurrentHealth.Value -= amount;
            if (CurrentHealth.Value <= 0f)
            {
                CurrentHealth.Value = 0f;
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            // Override in specific implementations
        }
    }
}
