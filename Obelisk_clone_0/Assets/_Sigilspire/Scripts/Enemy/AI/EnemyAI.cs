using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Enemy
{
    /// <summary>
    /// Simple server-authoritative 2D enemy AI.
    /// Uses Physics2D for detection and Rigidbody2D for movement.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : NetworkBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float stoppingDistance = 1.5f;

        [Header("Attack")]
        [SerializeField] private Ability primaryAbility;
        [SerializeField] private float attackRange = 1.8f;

        private Transform _currentTarget;
        private float _lastAttackTime;
        private Rigidbody2D _rb2D;

        private void Awake()
        {
            _rb2D = GetComponent<Rigidbody2D>();
        }

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
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayers);
            if (hits.Length == 0)
            {
                _currentTarget = null;
                return;
            }

            _currentTarget = hits[0].transform;
        }

        protected virtual void HandleMovement()
        {
            if (_currentTarget == null) return;

            Vector2 currentPos = _rb2D.position;
            Vector2 toTarget = (Vector2)_currentTarget.position - currentPos;
            float distance = toTarget.magnitude;

            if (distance <= stoppingDistance) return;

            Vector2 direction = toTarget.normalized;
            Vector2 newPos = currentPos + direction * (moveSpeed * Time.deltaTime);
            _rb2D.MovePosition(newPos);
        }

        protected virtual void TryAttack()
        {
            if (primaryAbility == null) return;
            if (_currentTarget == null) return;

            if (Time.time - _lastAttackTime < primaryAbility.cooldown) return;

            Vector2 toTarget = (Vector2)_currentTarget.position - (Vector2)transform.position;
            float distance = toTarget.magnitude;

            if (distance > attackRange) return;

            _lastAttackTime = Time.time;
            PerformAbilityAttack(_currentTarget, primaryAbility);
        }

        protected virtual void PerformAbilityAttack(Transform target, Ability ability)
        {
            var dmg = target.GetComponent<IDamageable>();
            if (dmg != null && ability.damage > 0f)
            {
                dmg.TakeDamage(ability.damage);
            }

            if (ability.vfxPrefab != null)
            {
                Vector3 spawnPos = target.position;
                var vfx = Object.Instantiate(ability.vfxPrefab, spawnPos, Quaternion.identity);
                Object.Destroy(vfx, 2f);
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
