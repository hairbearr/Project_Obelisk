using UnityEngine;
using Unity.Netcode;
using Sigilspire.Combat;

namespace Sigilspire.Player
{
    /// <summary>
    /// Handles shield attacks and blocking for the player.
    /// Converts incoming damage into shield energy absorption.
    /// Server-authoritative for damage calculations and shield state.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerShieldController : NetworkBehaviour
    {
        [Header("Shield Settings")]
        public float maxShieldEnergy = 50f;      // Maximum shield energy
        public float shieldRegenRate = 5f;       // Energy regenerated per second
        public float shieldBreakStunDuration = 1.5f; // Time stunned when shield breaks

        [Header("References")]
        public Animator shieldAnimator;           // Animator controlling shield animations
        public Attack blockAttackSO;             // AttackSO representing the block ability
        private Health playerHealth;

        private float currentShieldEnergy;
        private bool isBlocking;
        private bool isStunned;

        private void Awake()
        {
            playerHealth = GetComponent<Health>();
            currentShieldEnergy = maxShieldEnergy;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Regenerate shield if not blocking or stunned
            if (!isBlocking && !isStunned)
            {
                currentShieldEnergy += shieldRegenRate * Time.deltaTime;
                currentShieldEnergy = Mathf.Min(currentShieldEnergy, maxShieldEnergy);
            }

            // Example: update animations for client visuals
            shieldAnimator.SetBool("IsBlocking", isBlocking);
        }

        /// <summary>
        /// Call to start blocking.
        /// </summary>
        public void StartBlock()
        {
            if (isStunned) return;
            isBlocking = true;
        }

        /// <summary>
        /// Call to stop blocking.
        /// </summary>
        public void StopBlock()
        {
            isBlocking = false;
        }

        /// <summary>
        /// Applies damage to the shield first, leftover goes to health.
        /// Should be called from server-side for Netcode.
        /// </summary>
        public void AbsorbDamageServerRpc(float amount, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            float leftoverDamage = 0f;

            if (isBlocking && currentShieldEnergy > 0)
            {
                currentShieldEnergy -= amount;
                if (currentShieldEnergy <= 0)
                {
                    leftoverDamage = -currentShieldEnergy; // remaining damage
                    currentShieldEnergy = 0;
                    StartCoroutine(BreakShieldRoutine());
                }
            }
            else
            {
                leftoverDamage = amount;
            }

            // Apply leftover damage to player's health
            if (IsServer && leftoverDamage > 0f)
            {
                playerHealth.TakeDamageServerRpc(leftoverDamage, 0f, transform.position);
            }

            // Send client visual updates
            PlayBlockHitClientRpc();
        }

        [ClientRpc]
        private void PlayBlockHitClientRpc(ClientRpcParams rpcParams = default)
        {
            // Play block hit effects / animations
            shieldAnimator.SetTrigger("Hit");
        }

        private System.Collections.IEnumerator BreakShieldRoutine()
        {
            isBlocking = false;
            isStunned = true;

            // Play shield break animation
            shieldAnimator.SetTrigger("Break");

            yield return new WaitForSeconds(shieldBreakStunDuration);
            isStunned = false;
            currentShieldEnergy = maxShieldEnergy * 0.2f; // small regen after break
        }

        /// <summary>
        /// Returns whether the shield can currently block.
        /// </summary>
        public bool CanBlock() => !isStunned && currentShieldEnergy > 0;

        /// <summary>
        /// Current shield energy (0 to max).
        /// </summary>
        public float CurrentShieldEnergy => currentShieldEnergy;
    }
}
