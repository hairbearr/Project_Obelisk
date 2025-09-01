using UnityEngine;
using Unity.Netcode;
using Pathfinding;

namespace Sigilspire.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(AIDestinationSetter))]
    [RequireComponent(typeof(Animator))]
    public class EnemyController : NetworkBehaviour
    {
        [Header("References")]
        private Rigidbody2D rb;
        private Animator animator;
        private AIDestinationSetter destinationSetter;

        [Header("State")]
        private Direction direction;
        private bool isAttacking;
        private bool isDead;

        private Transform currentTarget;
        private float lastAggroTime;

        public NetworkVariable<bool> IsInAggroRange = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> IsInAttackRange = new NetworkVariable<bool>(false);
        public NetworkVariable<bool> IsGrappleable = new NetworkVariable<bool>(true);

        public EnemySpawner enemySpawner;

        public Direction Direction { get => direction; private set => direction = value; }
        public bool IsAttacking { get => isAttacking; private set => isAttacking = value; }
        public bool IsDead { get => isDead; private set => isDead = value; }
        public Transform CurrentTarget => currentTarget;

        [Header("AI Settings")]
        public Transform startPosition;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            destinationSetter = GetComponent<AIDestinationSetter>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false; // Only server controls enemy behavior
            }
        }

        private void Update()
        {
            if (!IsServer || IsDead) return;

            HandleMovement();
            UpdateAnimationClientRpc(direction, isAttacking, isDead);
        }

        private void HandleMovement()
        {
            if (!IsInAggroRange.Value || IsInAttackRange.Value) return;
            if (destinationSetter.target == null) return;

            Vector3 dirVector = destinationSetter.target.position - transform.position;
            UpdateDirection(dirVector);

            rb.MovePosition(transform.position + dirVector.normalized * Time.deltaTime);
        }

        private void UpdateDirection(Vector3 dirVector)
        {
            if (dirVector == Vector3.zero) return;

            float angle = Mathf.Atan2(dirVector.y, dirVector.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;

            if (angle >= 337.5f || angle < 22.5f) direction = Direction.East;
            else if (angle >= 22.5f && angle < 67.5f) direction = Direction.NorthEast;
            else if (angle >= 67.5f && angle < 112.5f) direction = Direction.North;
            else if (angle >= 112.5f && angle < 157.5f) direction = Direction.NorthWest;
            else if (angle >= 157.5f && angle < 202.5f) direction = Direction.West;
            else if (angle >= 202.5f && angle < 247.5f) direction = Direction.SouthWest;
            else if (angle >= 247.5f && angle < 292.5f) direction = Direction.South;
            else direction = Direction.SouthEast;
        }

        // -------------------------------
        // Animation RPC
        // -------------------------------
        [ClientRpc]
        public void UpdateAnimationClientRpc(Direction dir, bool attacking, bool dead, ClientRpcParams rpcParams = default)
        {
            animator.SetFloat("Direction", (float)dir);
            animator.SetBool("IsAttacking", attacking);
            animator.SetBool("IsDead", dead);
        }

        // -------------------------------
        // Server Methods
        // -------------------------------

        public void SetAttackingState(bool attacking)
        {
            isAttacking = attacking;
        }

        public void Die()
        {
            if (!IsServer) return;
            isDead = true;
            rb.linearVelocity = Vector2.zero;
            destinationSetter.target = null;
        }

        // Aggro management
        public void SetAggroTarget(Transform target)
        {
            if (currentTarget == null)
            {
                currentTarget = target;
                lastAggroTime = Time.time;
                IsInAggroRange.Value = true;
                destinationSetter.target = currentTarget;
            }
        }

        public void ClearAggroTarget(Transform target)
        {
            if (currentTarget == target)
            {
                currentTarget = null;
                IsInAggroRange.Value = false;
                destinationSetter.target = startPosition;
                lastAggroTime = Time.time;
            }
        }
    }
}

