using UnityEngine;
using Unity.Netcode;
using Sigilspire.Combat;
using Sigilspire.Enemy;

namespace Sigilspire.Player
{
    /// <summary>
    /// Handles grappling hook attacks and movement mechanics for the player.
    /// Reads data from AttackSO, performs damage (if applicable), and handles grappling logic.
    /// Server-authoritative; ClientRpc triggers animations and effects.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerGrapplingHookController : NetworkBehaviour
    {
        [Header("References")]
        public Animator hookAnimator; // Animator for the grappling hook sprite
        public Attack currentGrapplingHookAbility; // AttackSO for the grappling hook
        public PlayerController playerController;

        private Rigidbody2D rb;
        private bool isGrappling;
        private Transform grappleTarget; // Enemy or object being grappled

        private void Awake()
        {
            rb = playerController.GetComponent<Rigidbody2D>();
        }

        // -------------------------------
        // Attack Interface
        // -------------------------------

        /// <summary>
        /// Initiates a grappling attack in a given direction.
        /// </summary>
        public void PerformGrapple(Direction dir)
        {
            if (!IsServer) return; // Only server validates attack

            // Determine the grapple direction vector
            Vector2 grappleDir = DirectionToVector(dir);

            // Raycast to find target
            RaycastHit2D hit = Physics2D.Raycast(rb.position, grappleDir, currentGrapplingHookAbility.grapplingHookMaxDistance, currentGrapplingHookAbility.grapplingHookLayerMask);

            if (hit.collider != null)
            {
                grappleTarget = hit.collider.transform;

                // Check if the target is an enemy
                Health enemyHealth = hit.collider.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    bool canBeGrappled = hit.collider.GetComponent<EnemyController>()?.IsGrappleable.Value ?? true;

                    if (canBeGrappled)
                    {
                        // Pull enemy towards player
                        StartCoroutine(PullEnemyToPlayer(enemyHealth));
                        enemyHealth.TakeDamageServerRpc(currentGrapplingHookAbility.grapplingHookDamage, 0f, transform.position); // small damage
                    }
                    else
                    {
                        // Pull player to enemy
                        StartCoroutine(PullPlayerToTarget(hit.collider.transform));
                    }
                }
                else
                {
                    // Pull player to grapple point
                    StartCoroutine(PullPlayerToTarget(hit.collider.transform));
                }
            }

            // Trigger all clients to play grapple animation
            PlayGrappleAnimationClientRpc(dir);
        }

        // -------------------------------
        // Client Visuals
        // -------------------------------

        [ClientRpc]
        public void PlayGrappleAnimationClientRpc(Direction dir)
        {
            // Set hook animation for direction
            hookAnimator.Play(currentGrapplingHookAbility.GetGrappleAnimation(dir).name);
        }

        // -------------------------------
        // Grappling Movement Logic
        // -------------------------------

        private System.Collections.IEnumerator PullPlayerToTarget(Transform target)
        {
            isGrappling = true;
            while (Vector2.Distance(rb.position, target.position) > 0.1f)
            {
                Vector2 direction = ((Vector2)target.position - rb.position).normalized;
                rb.MovePosition(rb.position + direction * currentGrapplingHookAbility.grapplingHookPullSpeed * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }
            isGrappling = false;
        }

        private System.Collections.IEnumerator PullEnemyToPlayer(Health enemyHealth)
        {
            Transform enemyTransform = enemyHealth.transform;
            isGrappling = true;
            while (Vector2.Distance(rb.position, enemyTransform.position) > 0.1f)
            {
                Vector2 direction = (rb.position - (Vector2)enemyTransform.position).normalized;
                enemyTransform.position += (Vector3)(direction * currentGrapplingHookAbility.grapplingHookPullSpeed * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }
            isGrappling = false;
        }

        // -------------------------------
        // Utility
        // -------------------------------

        /// <summary>
        /// Converts a Direction enum to a normalized Vector2.
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
                _ => Vector2.zero
            };
        }
    }
}
