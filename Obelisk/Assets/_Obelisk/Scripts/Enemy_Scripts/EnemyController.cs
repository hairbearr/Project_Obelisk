using UnityEngine;
using Pathfinding;

public class EnemyController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator animator;
    private AIDestinationSetter destinationSetter;

    [SerializeField] private Direction direction;
    [SerializeField] private bool playerIsInAggroRange, playerIsInAttackRange, isAttacking, isDead;
    [SerializeField] private Transform startPosition;

    public Direction Direction { get => direction; set => direction = value; }
    public bool IsInAggroRange { get => playerIsInAggroRange; set => playerIsInAggroRange = value; }
    public bool IsInAttackRange { get => playerIsInAttackRange; set => playerIsInAttackRange = value; }
    public bool IsDead { get => isDead; set => isDead = value; }
    public bool IsAttacking { get => isAttacking; set => isAttacking = value; }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        destinationSetter = GetComponent<AIDestinationSetter>();
    }

    void Update()
    {
        if (IsDead) return;
        HandleMovement();
        Animate();
    }

    private void HandleMovement()
    {
        if (!IsInAggroRange || IsInAttackRange) return;
        if (destinationSetter.target == null) return;

        Vector3 dir = destinationSetter.target.position - transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg * -1;

        // Convert angle to 8-direction enum
        if (angle > 67.5f && angle <= 112.5f) direction = Direction.North;
        else if (angle > 22.5f && angle <= 67.5f) direction = Direction.NorthEast;
        else if (angle > -22.5f && angle <= 22.5f) direction = Direction.East;
        else if (angle > -67.5f && angle <= -22.5f) direction = Direction.SouthEast;
        else if (angle > -112.5f && angle <= -67.5f) direction = Direction.South;
        else if (angle > -157.5f && angle <= -112.5f) direction = Direction.SouthWest;
        else if (angle > 157.5f || angle <= -157.5f) direction = Direction.West;
        else if (angle > 112.5f && angle <= 157.5f) direction = Direction.NorthWest;
    }

    private void Animate()
    {
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsDead", isDead);
        animator.SetFloat("Direction", (float)direction);
    }
}