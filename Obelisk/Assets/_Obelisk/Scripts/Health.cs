using UnityEngine;
using Unity.Netcode;
using System.Collections;
using Pathfinding;
using Sigilspire.Player;
using Sigilspire.Combat;

namespace Sigilspire.Combat
{
    /// <summary>
    /// Handles health, damage, knockback, shield absorption, death, and UI for both players and enemies.
    /// Server-authoritative for damage and shield absorption; clients receive visual updates via NetworkVariables and RPCs.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Health : NetworkBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;

        // Tracks current health and syncs automatically across clients
        private NetworkVariable<float> currentHealth = new NetworkVariable<float>();

        [Header("Knockback Settings")]
        [SerializeField] private float knockBackDelayTime = 0.15f;

        private Rigidbody2D rb;
        private AIPath enemyAIPath;
        private PlayerShieldController shieldController;

        public float CurrentHealth => currentHealth.Value;
        public float MaxHealth => maxHealth;

        public bool IsDead { get; private set; } = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            // Try to get enemy AIPath if it exists
            enemyAIPath = GetComponent<AIPath>();

            // Try to get shield if player
            shieldController = GetComponent<PlayerShieldController>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Initialize health on server
                currentHealth.Value = maxHealth;
            }

            // Subscribe to health changes for client-side UI updates
            currentHealth.OnValueChanged += OnHealthChanged;
        }

        public override void OnDestroy()
        {
            base.OnDestroy(); // call NetworkBehaviour's OnDestroy
            currentHealth.OnValueChanged -= OnHealthChanged;
        }


        /// <summary>
        /// Called to apply damage to this entity.
        /// Server-authoritative. Handles shield absorption, leftover damage, knockback, and death.
        /// </summary>
        /// <param name="amount">Damage amount</param>
        /// <param name="knockbackForce">Knockback force</param>
        /// <param name="sourcePosition">Position of the damage source for knockback direction</param>
        [ServerRpc]
        public void TakeDamageServerRpc(float amount, float knockbackForce, Vector3 sourcePosition, ServerRpcParams rpcParams = default)
        {
            if (IsDead) return;

            float remainingDamage = amount;

            // -------------------------------
            // Shield Absorption (if player has shield)
            // -------------------------------
            if (shieldController != null && shieldController.CanBlock())
            {
                shieldController.AbsorbDamageServerRpc(amount);
                remainingDamage = Mathf.Max(0f, amount - shieldController.CurrentShieldEnergy);
            }

            // -------------------------------
            // Apply remaining damage to health
            // -------------------------------
            if (remainingDamage > 0f)
            {
                currentHealth.Value -= remainingDamage;

                // -------------------------------
                // Apply knockback
                // -------------------------------
                if (rb != null)
                {
                    StartCoroutine(KnockbackRoutine(knockbackForce, sourcePosition));
                }

                // -------------------------------
                // Check for death
                // -------------------------------
                if (currentHealth.Value <= 0f)
                {
                    Die();
                }
            }
        }

        /// <summary>
        /// Knockback coroutine disables AI temporarily, applies force, and stops it after delay.
        /// </summary>
        private IEnumerator KnockbackRoutine(float force, Vector3 sourcePos)
        {
            // Disable enemy AI movement temporarily
            if (enemyAIPath != null) enemyAIPath.canMove = false;

            // Apply knockback force
            rb.AddForce((rb.position - (Vector2)sourcePos) * force, ForceMode2D.Impulse);

            yield return new WaitForSeconds(knockBackDelayTime);

            // Stop velocity
            rb.linearVelocity = Vector2.zero;

            // Re-enable AI if present
            if (enemyAIPath != null) enemyAIPath.canMove = true;
        }

        /// <summary>
        /// Called when health changes on clients; use to update UI.
        /// </summary>
        private void OnHealthChanged(float oldValue, float newValue)
        {
            // TODO: Update health bars, HUD, or floating numbers
            // Example: HealthUI.Instance.UpdateHealthBar(this, newValue / maxHealth);
        }

        /// <summary>
        /// Handles death logic for both player and enemies.
        /// </summary>
        private void Die()
        {
            IsDead = true;

            // Disable colliders
            var colliders = GetComponents<Collider2D>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            // Disable movement
            if (enemyAIPath != null) enemyAIPath.canMove = false;

            // Notify client-side for animations / VFX
            PlayDeathClientRpc();

            // Optional: handle player respawn logic elsewhere
        }

        [ClientRpc]
        private void PlayDeathClientRpc(ClientRpcParams rpcParams = default)
        {
            // Play death animation if Animator exists
            var animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Death");
            }
        }

        /// <summary>
        /// Fully heal entity (useful for respawn or health potions).
        /// Server-authoritative.
        /// </summary>
        [ServerRpc]
        public void HealServerRpc(float amount, ServerRpcParams rpcParams = default)
        {
            if (IsDead) return;

            currentHealth.Value = Mathf.Min(currentHealth.Value + amount, maxHealth);
        }
    }
}
