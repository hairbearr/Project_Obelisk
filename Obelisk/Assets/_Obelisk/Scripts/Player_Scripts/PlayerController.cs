using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed =5;

    private Animator animator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier;
    [SerializeField]private Vector2 movementInput;
    [SerializeField]private float direction, isMoving;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false; return;
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        //Attack();
        Animate();
    }

    private void Attack()
    {
        throw new NotImplementedException();
    }

    private void Move()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal == 0 && vertical == 0)
        {
            rb.linearVelocity = new Vector2(0,0);
            isMoving = 0;
            return;
        }

        movementInput = new Vector2(horizontal, vertical);
             if ( horizontal == 1 && vertical == 0 )   { direction = 0; } // east
        else if ( horizontal == 1 && vertical == 1 )   { direction = 1; } // northEast
        else if ( horizontal == 0 && vertical == 1 )   { direction = 2; } // north
        else if ( horizontal == -1 && vertical == 1 )  { direction = 3; } // northWest
        else if ( horizontal == -1 && vertical == 0 )  { direction = 4; } // west
        else if ( horizontal == -1 && vertical == -1 ) { direction = 5; } // southWest
        else if ( horizontal == 0 && vertical == -1 )  { direction = 6; } // south
        else if ( horizontal == 1 && vertical == -1 )  { direction = 7; } // southEast

        isMoving = 1;
        rb.linearVelocity = movementInput * movementSpeed * movementSpeedMultiplier * Time.fixedDeltaTime;
    }

    private void Animate()
    {
        animator.SetFloat("MovementX", movementInput.x);
        animator.SetFloat("MovementY", movementInput.y);
        animator.SetFloat("Direction", direction);
        animator.SetFloat("IsMoving", isMoving);
        
    }
}
