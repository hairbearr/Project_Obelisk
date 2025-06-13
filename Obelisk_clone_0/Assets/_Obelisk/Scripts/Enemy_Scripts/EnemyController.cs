using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float directionInRadian, movementSpeed, direction;
    [SerializeField] GameObject player, weapon;
    private Rigidbody2D rb;
    private Animator enemyAnimator, weaponAnimator;
    [SerializeField] bool playerIsInAggroRange, playerIsInAttackRange, isAttacking, specialAttacking, isPatrolling, canPatrol, isChasing;
    [SerializeField] Vector3 patrolStart, patrolEnd;
    public EnemySpawner enemySpawner;

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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GetComponent<AIDestinationSetter>().target != null)
        {
            directionInRadian = Mathf.Atan2(transform.position.y - GetComponent<AIDestinationSetter>().target.position.y, transform.position.x - GetComponent<AIDestinationSetter>().target.position.x);

            float directionInDegrees = directionInRadian * Mathf.Rad2Deg * -1;

            if (directionInDegrees >= 67.5 && directionInDegrees <= 112.5)
            {
                direction = 1; // north
            }
            else if (directionInDegrees < 67.5f && directionInDegrees > 22.5)
            {
                direction = 2; // northeast
            }
            else if (directionInDegrees <= 22.5 && directionInDegrees >= -22.5)
            {
                direction = 0; // east
            }
            else if (directionInDegrees < -22.5 && directionInDegrees > -67.5)
            {
                direction = 5; // southeast
            }
            else if (directionInDegrees <= -67.5 && directionInDegrees >= -112.5)
            {
                direction = 4; // south
            }
            else if (directionInDegrees < -112.5 && directionInDegrees > -157.5)
            {
                direction = 6; // southwest
            }
            else if (directionInDegrees <= 157.5 && directionInDegrees >= 157.5)
            {
                direction = 7; // west
            }
            else if (directionInDegrees < 157.5 && directionInDegrees > 112.5)
            {
                direction = 3; // northwest
            }
        }
    }

    private void Animate()
    {
        
    }

}
