using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

namespace Combat.Health
{
    public class HealthBase : NetworkBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 100f;
        public float MaxHealth => maxHealth;

        public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool Initialized => initialized;
        private bool initialized;

        public override void OnNetworkSpawn()
        {
            // Everyone listens so clients can know when the first real value arrives
            CurrentHealth.OnValueChanged += OnHealthChanged;

            initialized = true;

            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
            }
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float oldVal, float newVal)
        {
            if (!initialized)
                initialized = true;
        }

        public virtual void TakeDamage(float amount)
        {
            if (!IsServer) return;
            if (!initialized) return;
            if (amount <= 0f) return;

            CurrentHealth.Value -= amount;
            if (CurrentHealth.Value <= 0f)
            {
                CurrentHealth.Value = 0f;
                OnDeath();
            }
        }

        public virtual void TakeDamage(float amount, ulong attackerId)
        {
            if (!IsServer) return;
            if (!initialized) return;
            if (amount <= 0f) return;

            CurrentHealth.Value -= amount;

            var threat = GetComponentInParent<IThreatReceiver>();
            if (threat != null && attackerId != 0)
            {
                threat.AddThreat(attackerId, amount);
            }

            if (CurrentHealth.Value <= 0f)
            {
                CurrentHealth.Value = 0f;
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            // Override in derived classes
        }
    }
}
