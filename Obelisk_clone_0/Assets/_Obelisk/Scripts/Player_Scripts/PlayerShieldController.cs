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

        [Header("References")]
        public Animator shieldAnimator;           // Animator controlling shield animations
        public Attack currentShieldAbility;             // AttackSO representing the block ability
        private Health playerHealth;

        private float currentShieldEnergy;
        private bool isBlocking;
        private bool isStunned;

        private void Awake()
        {
            playerHealth = GetComponent<Health>();
            currentShieldEnergy = currentShieldAbility.shieldMaxEnergy; // use the ability's energy data
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Regenerate shield if not blocking or stunned
            if (!isBlocking && !isStunned)
            {
                currentShieldEnergy += currentShieldAbility.shieldRegenRate * Time.deltaTime;
                currentShieldEnergy = Mathf.Min(currentShieldEnergy, currentShieldAbility.shieldMaxEnergy);
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
        [ServerRpc]
        public void AbsorbDamageServerRpc(float amount, float knockBack, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            float leftoverDamage = amount;

            if (isBlocking && currentShieldEnergy > 0)
            {
                currentShieldEnergy -= amount;

                knockBack *= (1f - currentShieldAbility.shieldKnockBackReduction); // percentage knockback reduction while blocking

                playerHealth.TakeDamageServerRpc(0f, knockBack, transform.position);

                if (currentShieldEnergy <= 0)
                {
                    leftoverDamage -= currentShieldEnergy; // remaining damage
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
                playerHealth.TakeDamageServerRpc(leftoverDamage, knockBack, transform.position);
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

            yield return new WaitForSeconds(currentShieldAbility.shieldBreakStunDuration);
            isStunned = false;
            currentShieldEnergy = currentShieldAbility.shieldMaxEnergy * 0.2f; // small regen after break
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
