using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed =5;

    private Animator animator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier =1f;
    [SerializeField]private Vector2 movementInput;
    [SerializeField] private float direction, isMoving, isAttacking, isBlocking, isGrappling, isJumping;

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
        Animate();
        Controls();
    }

    private void Jump()
    {
        
    }
    private void SwingSword()
    {
        
    }

    private void ShieldBlock()
    {
        
    }

    private void FireGrapplingHook()
    {
        
    }

    private void DealDamage()
    {

    }

    private void CastSwordAbility()
    {
        print("Sword Ability");
    }
    
    private void Controls()
    {
        // Swing Sword
        if (Input.GetMouseButtonDown(0))
        {
            isAttacking = 1;
            print("Swinging Sword");
            // play attack animation()
            // if there's an ability use the ability here
        }
        if (Input.GetMouseButtonUp(0))
        {
            isAttacking = 0;
            print("No Longer Attacking");
        }

        // Shield Block
        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = 1;
            print("Actively Blocking");
            // play block animation(s)
            // if there's an ability, use the ability here
        }
        if (Input.GetMouseButtonUp(1))
        {
            isBlocking = 0;
            print("No Longer Blocking");
        }

        // Fire Grappling Hook
        if (Input.GetMouseButtonDown(2))
        {
            isGrappling = 1;
            print("Grappling Hook Fire");
            // play grappling hook animation
            // fire grappling hook projectile, which does all the grappling hook stuffs, including the abilities
        }
        if (Input.GetMouseButtonUp(2))
        {
            isGrappling = 0;
            print("Grappling Hook No Longer Firing");
            //If (GrapplingHookConnected){
            // if(Connection == enemy){
            // pull enemy to player;}
            // if (Connection == grapplePoint){
            // Pull player to grapplePoint;}
        }

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            isJumping = 1;
            print("Jumping");
            // play jump animation
            // do jump mechanics?
        }
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
             if ( horizontal == 1  && vertical == 0   ) { direction = 0; } // east
        else if ( horizontal == 1  && vertical == 1   ) { direction = 1; } // northEast
        else if ( horizontal == 0  && vertical == 1   ) { direction = 2; } // north
        else if ( horizontal == -1 && vertical == 1   ) { direction = 3; } // northWest
        else if ( horizontal == -1 && vertical == 0   ) { direction = 4; } // west
        else if ( horizontal == -1 && vertical == -1  ) { direction = 5; } // southWest
        else if ( horizontal == 0  && vertical == -1  ) { direction = 6; } // south
        else if ( horizontal == 1  && vertical == -1  ) { direction = 7; } // southEast

        isMoving = 1;
        rb.linearVelocity = movementInput * movementSpeed * movementSpeedMultiplier * Time.fixedDeltaTime;
    }

    private void Animate()
    {
        animator.SetFloat("MovementX", movementInput.x);
        animator.SetFloat("MovementY", movementInput.y);
        animator.SetFloat("Direction", direction);
        animator.SetFloat("IsMoving", isMoving);
        animator.SetFloat("IsAttacking", isAttacking);
        animator.SetFloat("IsBlocking", isBlocking);
        animator.SetFloat("IsGrappling", isGrappling);
        animator.SetFloat("IsJumping", isJumping);
    }
}
