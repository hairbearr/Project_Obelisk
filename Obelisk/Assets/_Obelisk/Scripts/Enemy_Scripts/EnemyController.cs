using UnityEngine;
using System.Collections;
using System.Collections.Generic;
<<<<<<< HEAD
using Pathfinding;
using System;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float directionInRadian, movementSpeed, waitTime = 10f;
    [SerializeField] GameObject player, weapon;
    [SerializeField] Direction direction;
    private Rigidbody2D rb;
    private Animator animator;
    private AIDestinationSetter destinationSetter;
    [SerializeField] bool playerIsInAggroRange, playerIsInAttackRange, isAttacking, specialAttacking, isPatrolling, canPatrol, isRunning, isWalking, isChasing, isGettingHit, isDead, isDisabled, returnToStartPoint = false;
    [SerializeField] Vector3 patrolStart, patrolEnd;
    [SerializeField] Transform startPosition;
=======

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float directionInRadian;
    [SerializeField] GameObject player;
    private Rigidbody2D rb;
    private Animator animator;
    [SerializeField] bool playerIsInAggroRange, playerIsInAttackRange, isAttacking, specialAttacking, isPatrolling, canPatrol, isChasing;
    [SerializeField] Vector3 patrolStart, patrolEnd;
    [SerializeField] float direction;
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
    public EnemySpawner enemySpawner;

    public Transform StartPosition
    {
        get { return startPosition; }
        set { startPosition = value; }
    }
    public bool IsDisabled
    {
        get { return isDisabled; }
        set { isDisabled = value; }
    }
    public bool IsReturningToStartPoint
    {
        get { return returnToStartPoint; }
        set { returnToStartPoint = value; }
    }

    public bool IsDead
    {
        get{ return isDead; }
        set{ isDead = value; }
    }
    public bool IsInAggroRange
    {
        get { return playerIsInAggroRange; }
        set { playerIsInAggroRange = value; }
    }
    public bool IsInAttackRange
    {
        get { return playerIsInAttackRange; }
        set { playerIsInAttackRange = value; }
    }
    public bool IsAttacking
    {
        get { return isAttacking; }
        set { isAttacking = value; }
    }
    public bool IsGettingHit
    {
        get { return isGettingHit; }
        set { isGettingHit = value; }
    }
    public bool IsRunning
    {
        get { return isRunning; }
        set { isRunning = value; }
    }
    public bool IsWalking
    {
        get { return isWalking; }
        set { isWalking = value; }
    }
    public Direction Direction
    {
        get { return direction; }
        set {  direction = value; }
    }
    public bool SpecialAttack
    {
        get { return specialAttacking; }
        set { specialAttacking = value; }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        destinationSetter = GetComponent<AIDestinationSetter>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        startPosition = enemySpawner.transform;
    }

    // Update is called once per frame
    void Update()
    {
<<<<<<< HEAD
        if(startPosition == null) {  return; }

        if (!IsDisabled)
        {
            if (destinationSetter.target != null)
            {
                Movement();
            }

            if (!playerIsInAggroRange && Vector2.Distance(startPosition.position, transform.position) > 0.05f)
            {
                returnToStartPoint = true;
            }

            if (IsReturningToStartPoint)
            {
                if (Vector2.Distance(startPosition.position, transform.position) <= 0.05f)
                {
                    transform.position = startPosition.position;
                    destinationSetter.target = null;
                    IsReturningToStartPoint = false;
                }
            }

            Combat();
            Patrol();
        }
        Animate();
    }

    private void Patrol()
    {
        if (!playerIsInAggroRange && canPatrol)
        {
            isPatrolling = true;
            //if position != patrol point i, set destination to patrol point i.
            // when you hit patrol point i, set destination to patrol point i+1
            // if i > patrol points, set i to 0;
        }
=======
        
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
    }

    private void Movement()
    {
<<<<<<< HEAD
        var target = destinationSetter.target;
        if (target == null) return;

        directionInRadian = Mathf.Atan2(transform.position.y - target.position.y, transform.position.x - target.position.x);
        float directionInDegrees = directionInRadian * Mathf.Rad2Deg * -1;

        if (directionInDegrees > 67.5f && directionInDegrees <= 112.5f) direction = Direction.North;   // N
        else if (directionInDegrees > 22.5f && directionInDegrees <= 67.5f) direction = Direction.NorthEast;  // NE
        else if (directionInDegrees > -22.5f && directionInDegrees <= 22.5f) direction = Direction.East;  // E
        else if (directionInDegrees > -67.5f && directionInDegrees <= -22.5f) direction = Direction.SouthEast; // SE
        else if (directionInDegrees > -112.5f && directionInDegrees <= -67.5f) direction = Direction.South; // S
        else if (directionInDegrees > -157.5f && directionInDegrees <= -112.5f) direction = Direction.SouthWest; // SW
        else if (directionInDegrees > 157.5f || directionInDegrees <= -157.5f) direction = Direction.West;  // W
        else if (directionInDegrees > 112.5f && directionInDegrees <= 157.5f) direction = Direction.NorthWest;  // NW

=======
        switch (direction)
        {
            case 0: // do the east stuff
                break;
            case 1: // do the north stuff
                break;
            case 2: // do the northeast stuff
                break;
            case 3: // do the northwest stuff
                break;
            case 4: // do the south stuff
                break;
            case 5: // do the southEast stuff
                break;
            case 6: // do the southwest stuff
                break;
            case 7: // do the west stuff
                break;
        }
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
    }

    private void Combat()
    {
        if (playerIsInAttackRange)
        {
            isAttacking = true;
            rb.linearVelocity = Vector2.zero;
        }

        if (!playerIsInAttackRange && playerIsInAggroRange)
        {
            isAttacking = false;
        }
    }
    private void Animate()
    {
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsSpecialAttacking", specialAttacking);
        animator.SetBool("IsRunning", isRunning);
        animator.SetBool("IsWalking", isWalking);
        animator.SetBool("IsDead", isDead);
        animator.SetFloat("Direction", (float)direction);
    }
}
