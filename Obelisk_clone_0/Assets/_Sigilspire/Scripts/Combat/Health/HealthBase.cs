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
            // If someone calls this overload, we can’t do directional blocking.
            // You should migrate all callers to TakeDamage(amount, attackerId).
            TakeDamage(amount, 0);
        }

        public virtual void TakeDamage(float amount, ulong attackerId)
        {
            if (!IsServer) return;
            if (!initialized) return;
            if (amount <= 0f) return;

            Debug.Log($"[DMG] {name} took {amount} from {attackerId} at t={Time.time}\n{System.Environment.StackTrace}");


            // Look up attacker position (for directional block)
            Vector2 attackerPos = Vector2.zero;
            if (attackerId != 0 && NetworkManager != null)
            {
                var sm = NetworkManager.SpawnManager;
                if (sm != null && sm.SpawnedObjects.TryGetValue(attackerId, out var attackerObj))
                    attackerPos = attackerObj.transform.position;
            }

            // Shield gate (child or same GO)
            var blocker = GetComponentInChildren<IBlockProvider>();
            if (blocker != null && attackerId != 0)
            {
                if (blocker.TryBlock(attackerPos, amount, out float afterBlock))
                    amount = afterBlock;
            }

            if (amount <= 0f) return;

            CurrentHealth.Value -= amount;

            var threat = GetComponentInParent<IThreatReceiver>();
            if (threat != null && attackerId != 0)
                threat.AddThreat(attackerId, amount);

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

        public void ServerSetFullHealth()
        {
            if (!IsServer) return;
            CurrentHealth.Value = maxHealth;
        }

    }
}
