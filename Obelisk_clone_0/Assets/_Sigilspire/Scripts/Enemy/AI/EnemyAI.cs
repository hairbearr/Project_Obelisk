using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;
using Combat.Health;

namespace Enemy
{
    /// <summary>
    /// 2D Enemy AI with locomotion + attack animation support.
    /// Server authoritative. Uses Rigidbody2D and Physics2D only.
    /// </summary>
    public class EnemyAI : NetworkBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float stoppingDistance = 1.2f;

        [Header("Attack")]
        [SerializeField] private Ability primaryAbility;
        [SerializeField] private float attackRange = 1.6f;

        private Transform currentTarget;
        private Rigidbody2D rb2D;
        private EnemyAnimationDriver animDriver;
        private HealthBase health;

        private float lastAttackTime;

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();
            animDriver = GetComponentInChildren<EnemyAnimationDriver>();
            health = GetComponent<HealthBase>();
        }

        private void Update()
        {
            if (!IsServer) return;

            if (health != null && health.CurrentHealth.Value <= 0f)
            {
                if (animDriver != null)
                {
                    animDriver.SetMovement(Vector2.zero);
                }
                return;
            }

            if (currentTarget == null)
            {
                FindTarget();
            }

            if (currentTarget != null)
            {
                HandleMovement();
                TryAttack();
            }
            else
            {
                if (animDriver != null)
                {
                    animDriver.SetMovement(Vector2.zero);
                }
            }
        }

        private void FindTarget()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayers);
            if (hits.Length == 0)
            {
                currentTarget = null;
                return;
            }

            currentTarget = hits[0].transform;
        }

        private void HandleMovement()
        {
            if (currentTarget == null) return;

            Vector2 toTarget = (Vector2)currentTarget.position - rb2D.position;
            float distance = toTarget.magnitude;

            if (distance <= stoppingDistance)
            {
                if (animDriver != null)
                {
                    animDriver.SetMovement(Vector2.zero);
                }
                return;
            }

            Vector2 direction = toTarget.normalized;
            rb2D.MovePosition(rb2D.position + direction * (moveSpeed * Time.deltaTime));

            if (animDriver != null)
            {
                animDriver.SetMovement(direction);
            }
        }

        private void TryAttack()
        {
            if (primaryAbility == null || currentTarget == null)
                return;

            if (Time.time - lastAttackTime < primaryAbility.cooldown)
                return;

            Vector2 toTarget = (Vector2)currentTarget.position - rb2D.position;
            float distance = toTarget.magnitude;

            if (distance > attackRange)
                return;

            lastAttackTime = Time.time;
            PerformAbilityAttack(currentTarget, primaryAbility);
        }

        private void PerformAbilityAttack(Transform target, Ability ability)
        {
            Vector2 attackDir = ((Vector2)target.position - rb2D.position).normalized;

            if (animDriver != null)
            {
                animDriver.PlayAttack(attackDir);
            }

            IDamageable dmg = target.GetComponent<IDamageable>();
            if (dmg != null && ability.damage > 0f)
            {
                dmg.TakeDamage(ability.damage);
            }

            if (ability.vfxPrefab != null)
            {
                GameObject vfx = GameObject.Instantiate(
                    ability.vfxPrefab,
                    target.position,
                    Quaternion.identity
                );
                GameObject.Destroy(vfx, 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
