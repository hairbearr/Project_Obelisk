using UnityEngine;
using Unity.Netcode;
using System;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed =5;

    [SerializeField] private Animator playerAnimator, shieldAnimator, swordAnimator, grapplingHookAnimator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier =1f;
    [SerializeField] private Vector2 movementInput;
    [SerializeField] private float direction, isMoving, isAttacking, isBlocking, isGrappling, isJumping, swingSpeed;
    [SerializeField] private GameObject sword, shield, grapplingHook;
    [SerializeField] private string activeSwordAbility;


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
        
        playerAnimator = GetComponent<Animator>();
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

    private void UseSwordAbility(string activeSwordAbility)
    {
        throw new NotImplementedException();
    }

    private void SheatheSword()
    {
        
    }

    private void TurnOffComponents(GameObject obj)
    {
        obj.SetActive(false);
        obj.GetComponent<BoxCollider2D>().enabled = false;
    }

    private void TurnOnComponents(GameObject gameObject)
    {
        gameObject.SetActive(true);
        gameObject.GetComponent<BoxCollider2D>().enabled = true;
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
        // east
        if ( horizontal == 1  && vertical == 0   )
        { 
            direction = 0;
        }
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
        playerAnimator.SetFloat("MovementX", movementInput.x);
        playerAnimator.SetFloat("MovementY", movementInput.y);
        playerAnimator.SetFloat("Direction", direction);
        playerAnimator.SetFloat("IsMoving", isMoving);
        playerAnimator.SetFloat("IsAttacking", isAttacking);
        playerAnimator.SetFloat("IsBlocking", isBlocking);
        playerAnimator.SetFloat("IsGrappling", isGrappling);
        playerAnimator.SetFloat("IsJumping", isJumping);
        shieldAnimator.SetFloat("MovementX", movementInput.x);
        shieldAnimator.SetFloat("MovementY", movementInput.y);
        shieldAnimator.SetFloat("Direction", direction);
        shieldAnimator.SetFloat("IsMoving", isMoving);
        shieldAnimator.SetFloat("IsAttacking", isAttacking);
        shieldAnimator.SetFloat("IsBlocking", isBlocking);
        shieldAnimator.SetFloat("IsGrappling", isGrappling);
        shieldAnimator.SetFloat("IsJumping", isJumping);
        swordAnimator.SetFloat("MovementX", movementInput.x);
        swordAnimator.SetFloat("MovementY", movementInput.y);
        swordAnimator.SetFloat("Direction", direction);
        swordAnimator.SetFloat("IsMoving", isMoving);
        swordAnimator.SetFloat("IsAttacking", isAttacking);
        swordAnimator.SetFloat("IsBlocking", isBlocking);
        swordAnimator.SetFloat("IsGrappling", isGrappling);
        swordAnimator.SetFloat("IsJumping", isJumping);
        grapplingHookAnimator.SetFloat("MovementX", movementInput.x);
        grapplingHookAnimator.SetFloat("MovementY", movementInput.y);
        grapplingHookAnimator.SetFloat("Direction", direction);
        grapplingHookAnimator.SetFloat("IsMoving", isMoving);
        grapplingHookAnimator.SetFloat("IsAttacking", isAttacking);
        grapplingHookAnimator.SetFloat("IsBlocking", isBlocking);
        grapplingHookAnimator.SetFloat("IsGrappling", isGrappling);
        grapplingHookAnimator.SetFloat("IsJumping", isJumping);
    }
}
