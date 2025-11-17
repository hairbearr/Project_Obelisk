using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Enemy
{
    /// <summary>
    /// Simple minion AI skeleton that owns its own Ability.
    /// Runs only on the server.
    /// </summary>
    public class EnemyAI : NetworkBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float stoppingDistance = 1.5f;

        [Header("Attack")]
        [SerializeField] private Ability primaryAbility;  // uses damage/cooldown/vfx of this
        [SerializeField] private float attackRange = 1.8f;

        private Transform _currentTarget;
        private float _lastAttackTime;

        private void Update()
        {
            if (!IsServer) return;

            if (_currentTarget == null)
            {
                FindTarget();
            }

            if (_currentTarget != null)
            {
                HandleMovement();
                TryAttack();
            }
        }

        protected virtual void FindTarget()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayers);
            if (hits.Length == 0)
            {
                _currentTarget = null;
                return;
            }

            // TODO: better target selection (closest, aggro, etc.)
            _currentTarget = hits[0].transform;
        }

        protected virtual void HandleMovement()
        {
            if (_currentTarget == null) return;

            Vector3 toTarget = _currentTarget.position - transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance <= stoppingDistance) return;

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (moveSpeed * Time.deltaTime);

            if (direction != Vector3.zero)
                transform.forward = direction;
        }

        protected virtual void TryAttack()
        {
            if (primaryAbility == null) return;
            if (_currentTarget == null) return;

            if (Time.time - _lastAttackTime < primaryAbility.cooldown) return;

            Vector3 toTarget = _currentTarget.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            if (distance > attackRange) return;

            _lastAttackTime = Time.time;
            PerformAbilityAttack(_currentTarget, primaryAbility);
        }

        /// <summary>
        /// Uses the given Ability on the target: damage + optional VFX.
        /// </summary>
        protected virtual void PerformAbilityAttack(Transform target, Ability ability)
        {
            // TODO: add windup, animations, etc.

            // Apply damage
            IDamageable dmg = target.GetComponent<IDamageable>();
            if (dmg != null && ability.damage > 0f)
            {
                dmg.TakeDamage(ability.damage);
            }

            // Spawn VFX at target or between enemy & target
            if (ability.vfxPrefab != null)
            {
                Vector3 spawnPos = target.position;
                Quaternion rot = Quaternion.LookRotation((target.position - transform.position).normalized);
                var vfx = Instantiate(ability.vfxPrefab, spawnPos, rot);
                Destroy(vfx, 2f);
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


