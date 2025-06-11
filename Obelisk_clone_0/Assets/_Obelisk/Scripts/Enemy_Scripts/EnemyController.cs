using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour
{
    [SerializeField] private float directionInRadian;
    [SerializeField] GameObject player;
    private Rigidbody2D rb;
    private Animator animator;
    [SerializeField] bool playerIsInAggroRange, playerIsInAttackRange, isAttacking, specialAttacking, isPatrolling, canPatrol, isChasing;
    [SerializeField] Vector3 patrolStart, patrolEnd;
    [SerializeField] float direction;
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
        
    }

    private void Animate()
    {
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
    }

}
