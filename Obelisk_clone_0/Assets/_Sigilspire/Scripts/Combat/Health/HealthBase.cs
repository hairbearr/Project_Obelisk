using Combat.DamageInterfaces;
using Enemy;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Combat.Health
{
    public class HealthBase : NetworkBehaviour, IDamageable
    {
        [SerializeField] protected float maxHealth = 100f;
        [SerializeField] protected Color flashColor = Color.white;
        public float MaxHealth => maxHealth;
        public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public bool Initialized => initialized;
        private bool initialized;

        [Header("Healthbar")]
        [SerializeField] protected Slider slider;

        public override void OnNetworkSpawn()
        {
            // Everyone listens so clients can know when the first real value arrives
            CurrentHealth.OnValueChanged += OnHealthChanged;

            initialized = true;

            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
            }

            if(slider != null)
            {
                if (!slider.enabled) slider.enabled = true;
            }

            UpdateHealthBar(CurrentHealth.Value, maxHealth);
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        private void OnHealthChanged(float oldVal, float newVal)
        {
            if (!initialized)
                initialized = true;

            UpdateHealthBar(newVal, maxHealth);
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

            // flash white when damage, but not when dead
            if(CurrentHealth.Value > 0f)
            {
                FlashColorClientRpc(flashColor);
            }

            var threat = GetComponentInParent<IThreatReceiver>();
            if (threat != null && attackerId != 0)
                threat.AddThreat(attackerId, amount);


            if (CurrentHealth.Value <= 0f)
            {
                CurrentHealth.Value = 0f;
                OnDeath();
                if (slider!=null)
                    slider.enabled = false;
            }

        }

        public void UpdateHealthBar(float currentValue, float maxValue)
        {
            if (slider == null) return;
            if(maxValue <=0f) { slider.value = 0f; return; }

            slider.value = currentValue / maxValue;
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

        [ClientRpc]
        private void FlashColorClientRpc(Color color)
        {
            Debug.Log($"[Flash] FlashColorClientRpc called with color: {color}");

            StartCoroutine(FlashRoutine(color));
        }

        private IEnumerator FlashRoutine(Color color)
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite == null) {
                Debug.LogWarning($"[Flash] No SpriteRenderer found on {gameObject.name}!");

                yield break; }

            Debug.Log($"[Flash] Found sprite: {sprite.name}, original color: {sprite.color}");

            Color og = sprite.color;
            sprite.color = color;

            Debug.Log($"[Flash] Changed to flash color: {color}");


            yield return new WaitForSeconds(0.1f);

            if(sprite != null)
            {
                sprite.color = og;
                Debug.Log($"[Flash] Restored original color: {og}");

            }
        }

    }
}
