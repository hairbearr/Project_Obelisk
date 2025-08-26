using UnityEngine;
using Unity.Netcode;
using Sigilspire.Combat;

namespace Sigilspire.Player
{
    /// <summary>
    /// Handles sword attacks for the player.
    /// Reads data from the Attack ScriptableObject.
    /// Server-authoritative for multiplayer; clients only play visuals/audio.
    /// </summary>
    public class PlayerSwordController : NetworkBehaviour
    {
        [Header("Sword Attack Settings")]
        public Attack currentAttack; // The Attack ScriptableObject currently equipped
        public Animator swordAnimator; // Animator for the sword sprite

        private float lastUsedTime = -Mathf.Infinity; // Tracks last attack time for cooldown

        private PlayerController playerController;

        private void Awake()
        {
            playerController = GetComponentInParent<PlayerController>();
        }

        /// <summary>
        /// Determines if the sword attack can currently be used.
        /// Checks cooldown timer.
        /// </summary>
        public bool IsReady()
        {
            return Time.time >= lastUsedTime + currentAttack.cooldown;
        }

        /// <summary>
        /// Called by PlayerController to perform an attack.
        /// Only the server should execute actual damage/knockback logic.
        /// </summary>
        public void PerformAttack(Direction dir)
        {
            if (!IsServer) return; // Server-authoritative logic

            if (!IsReady()) return;

            lastUsedTime = Time.time;

            // Apply attack logic here: damage & knockback
            DealDamage(dir);

            // Notify all clients to play animation/effects
            PlayAttackAnimationClientRpc(dir);
        }

        /// <summary>
        /// Deals damage and knockback to hit targets.
        /// Should only run on the server.
        /// </summary>
        private void DealDamage(Direction dir)
        {
            // For simplicity, we use a 2D boxcast in the facing direction
            Vector2 attackDir = DirectionToVector(dir);
            Vector2 origin = (Vector2)playerController.transform.position + attackDir * 0.5f;

            float attackRange = 1f; // Adjust range as needed
            LayerMask hitMask = LayerMask.GetMask("Enemy");

            RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, new Vector2(1f, 1f), 0f, attackDir, attackRange, hitMask);
            foreach (var hit in hits)
            {
                Health enemyHealth = hit.collider.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    // Apply damage
                    if (IsServer)
                    {
                        enemyHealth.TakeDamageServerRpc(currentAttack.damage, currentAttack.knockback, transform.position);
                    }
                }
            }
        }

        /// <summary>
        /// Converts Direction enum to normalized Vector2.
        /// </summary>
        private Vector2 DirectionToVector(Direction dir)
        {
            return dir switch
            {
                Direction.North => Vector2.up,
                Direction.NorthEast => (Vector2.up + Vector2.right).normalized,
                Direction.East => Vector2.right,
                Direction.SouthEast => (Vector2.down + Vector2.right).normalized,
                Direction.South => Vector2.down,
                Direction.SouthWest => (Vector2.down + Vector2.left).normalized,
                Direction.West => Vector2.left,
                Direction.NorthWest => (Vector2.up + Vector2.left).normalized,
                _ => Vector2.right
            };
        }

        /// <summary>
        /// Plays the sword animation for the given direction.
        /// Can be called locally on clients.
        /// </summary>
        public void PlayAttackAnimation(Direction dir)
        {
            if (swordAnimator == null) return;

            AnimationClip clip = currentAttack.GetAttackAnimation(dir);
            if (clip != null)
            {
                swordAnimator.Play(clip.name);
            }
        }

        // -------------------------------
        // CLIENT RPC to sync animations
        // -------------------------------

        [ClientRpc]
        private void PlayAttackAnimationClientRpc(Direction dir, ClientRpcParams rpcParams = default)
        {
            PlayAttackAnimation(dir);
        }
    }
}

