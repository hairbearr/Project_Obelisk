using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using System;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float directionInRadian, movementSpeed, direction, waitTime = 10f;
    [SerializeField] GameObject player, weapon;
    private Rigidbody2D rb;
    private Animator animator;
    [SerializeField] bool playerIsInAggroRange, playerIsInAttackRange, isAttacking, specialAttacking, isPatrolling, canPatrol, isRunning, isWalking, isChasing, isGettingHit, isDead, isDisabled, returnToStartPoint = false;
    [SerializeField] Vector3 patrolStart, patrolEnd;
    [SerializeField] Transform startPosition;
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
    public float Direction
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
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        startPosition = enemySpawner.transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(startPosition == null) {  return; }

        if (!IsDisabled)
        {
            if (GetComponent<AIDestinationSetter>().target != null)
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
                    GetComponent<AIDestinationSetter>().target = null;
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
    }

    private void Movement()
    {
        var target = GetComponent<AIDestinationSetter>().target;
        if (target == null) return;

        directionInRadian = Mathf.Atan2(transform.position.y - target.position.y, transform.position.x - target.position.x);
        float directionInDegrees = directionInRadian * Mathf.Rad2Deg * -1;

        if (directionInDegrees >= 67.5 && directionInDegrees <= 112.5) direction = 1; // N
        else if (directionInDegrees < 67.5f && directionInDegrees > 22.5) direction = 2; // NE
        else if (directionInDegrees <= 22.5 && directionInDegrees >= -22.5) direction = 0; // E
        else if (directionInDegrees < -22.5 && directionInDegrees > -67.5) direction = 5; // SE
        else if (directionInDegrees <= -67.5 && directionInDegrees >= -112.5) direction = 4; // S
        else if (directionInDegrees < -112.5 && directionInDegrees > -157.5) direction = 6; // SW
        else if (directionInDegrees <= 157.5 && directionInDegrees >= 157.5) direction = 7; // W
        else if (directionInDegrees < 157.5 && directionInDegrees > 112.5) direction = 3; // NW
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
        animator.SetFloat("Direction", direction);
    }
}
