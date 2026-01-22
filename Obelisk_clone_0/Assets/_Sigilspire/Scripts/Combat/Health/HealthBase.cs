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

        // NEW: everyone can query this
        public bool Initialized => initialized;
        private bool initialized;

        public override void OnNetworkSpawn()
        {
            // Everyone listens so clients can know when the first real value arrives
            CurrentHealth.OnValueChanged += OnHealthChanged;

            if (IsServer)
            {
                CurrentHealth.Value = maxHealth; // will replicate
                initialized = true;
            }
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float oldVal, float newVal)
        {
            // Client-side: first time we receive a replicated health value, mark initialized.
            // (Also runs on server, harmless.)
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
