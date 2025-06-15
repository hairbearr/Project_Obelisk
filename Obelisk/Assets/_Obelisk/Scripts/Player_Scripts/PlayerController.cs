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
    [SerializeField] private float swingSpeed, swordAttackType, direction, attackComboTimer, jumpTimer;
    [SerializeField] private bool attackCooldown, movementDisabled;
    [SerializeField] private float isMoving, isAttacking, isBlocking, isGrappling, isJumping, isClimbing, isDrinkingPotion, isGettingHit, isInteracting, isShooting, isUsingItem;
    [SerializeField] private GameObject sword, shield, grapplingHook;
    [SerializeField] private string activeSwordAbility;

    public float Direction
    {
        get { return direction; }
        set { direction = value; }
    }
    public float IsMoving
    {
        get { return isMoving; }
        set { isMoving = value; }
    }
    public float IsAttacking
    {
        get { return isAttacking; }
        set { isAttacking = value; }
    }
    public float SwordAttackType
    {
        get { return swordAttackType; }
        set { swordAttackType = value; }
    }
    public float IsBlocking
    {
        get { return isBlocking; }
        set { isBlocking = value; }
    }
    public float IsClimbing
    {
        get { return isClimbing; }
        set { isClimbing = value; }
    }
    public float IsDrinkingPotion
    {
        get { return isDrinkingPotion; }
        set { isDrinkingPotion = value; }
    }
    public float IsGettingHit
    {
        get { return isGettingHit; }
        set { isGettingHit = value; }
    }
    public float IsInteracting
    {
        get { return isInteracting; }
        set { isInteracting = value; }
    }
    public float IsJumping
    {
        get { return isJumping; }
        set { isJumping = value; }
    }
    public float IsGrappling
    {
        get { return isGrappling; }
        set { isGrappling = value; }
    }
    public float IsShooting
    {
        get { return isShooting; }
        set { isShooting = value; }
    }
    public float IsUsingItem
    {
        get { return isUsingItem; }
        set { isUsingItem = value; }
    }
    public float MovementSpeed
    {
        get { return movementSpeed; }
        set { movementSpeed = value; }
    }




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
        shieldAnimator = GetComponentInChildren<ShieldController>().GetComponent<Animator>();
        swordAnimator = GetComponentInChildren<SwordController>().GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        
        Move();
        Animate();
        Controls();

        if(swordAttackType > 1)
        {
            attackComboTimer -= Time.deltaTime;
        }
        if(attackComboTimer <= 0)
        {
            swordAttackType = 1;
            attackComboTimer = 10;
        }
    }

    private void StopJump()
    {
        isJumping = 0;
    }
    private void Jump()
    {

    }
    private void SwingSword()
    {
        movementDisabled = true;
        attackComboTimer = 10;
        if (swordAttackType < 1)
        {
            swordAttackType = 1;
        }
        attackCooldown = true;
    }

    private void SheatheSword()
    {
        swordAttackType++;
        if (swordAttackType > 3)
        {
            swordAttackType = 1;
        }
        isAttacking = 0;
        attackCooldown = false;
        movementDisabled = false;
    }
    
    private void Controls()
    {
        // Swing Sword
        if (Input.GetMouseButtonDown(0)&& !attackCooldown)
        {
            isAttacking = 1;
        }
        

        // Shield Block
        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = 1;
            print("Actively Blocking");
            movementDisabled = true;
            // play block animation(s)
            // if there's an ability, use the ability here
        }
        if (Input.GetMouseButtonUp(1))
        {
            movementDisabled = false;
            isBlocking = 0;
            print("No Longer Blocking");
        }

        // Fire Grappling Hook
        if (Input.GetMouseButtonDown(2))
        {
            movementDisabled = true;
            isShooting = 1;
            print("Grappling Hook Fire");
            // play grappling hook animation
            // fire grappling hook projectile, which does all the grappling hook stuffs, including the abilities
        }
        if (Input.GetMouseButtonUp(2))
        {
            movementDisabled = false;
            isShooting = 0;
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

        if (!movementDisabled)
        {
            isMoving = 1;
            rb.linearVelocity = movementInput * movementSpeed * movementSpeedMultiplier * Time.fixedDeltaTime;
        }
        else
        {
            rb.linearVelocity = movementInput * 0f;
            isMoving = 0;
        }
    }

    private void Animate()
    {
        playerAnimator.SetFloat("Direction", direction);
        playerAnimator.SetFloat("IsMoving", isMoving);
        playerAnimator.SetFloat("IsAttacking", isAttacking);
        playerAnimator.SetFloat("SwordAttackType", swordAttackType);
        playerAnimator.SetFloat("IsBlocking", isBlocking);
        playerAnimator.SetFloat("IsClimbing", isClimbing);
        playerAnimator.SetFloat("IsDrinkingPotion", isDrinkingPotion);
        playerAnimator.SetFloat("IsGettingHit", isGettingHit);
        playerAnimator.SetFloat("IsInteracting", isInteracting);
        playerAnimator.SetFloat("IsJumping", isJumping);
        playerAnimator.SetFloat("IsGrappling", isGrappling);
        playerAnimator.SetFloat("IsShooting", isShooting);
        playerAnimator.SetFloat("IsUsingItem", isUsingItem);
        
        if (grapplingHook.gameObject.activeSelf)
        {
            grapplingHookAnimator.SetFloat("Direction", direction);
            grapplingHookAnimator.SetFloat("IsMoving", isMoving);
            grapplingHookAnimator.SetFloat("IsAttacking", isAttacking);
            grapplingHookAnimator.SetFloat("SwordAttackType", swordAttackType);
            grapplingHookAnimator.SetFloat("IsBlocking", isBlocking);
            grapplingHookAnimator.SetFloat("IsClimbing", isClimbing);
            grapplingHookAnimator.SetFloat("IsDrinkingPotion", isDrinkingPotion);
            grapplingHookAnimator.SetFloat("IsGettingHit", isGettingHit);
            grapplingHookAnimator.SetFloat("IsInteracting", isInteracting);
            grapplingHookAnimator.SetFloat("IsJumping", isJumping);
            grapplingHookAnimator.SetFloat("IsGrappling", isGrappling);
            grapplingHookAnimator.SetFloat("IsShooting", isShooting);
            grapplingHookAnimator.SetFloat("IsUsingItem", isUsingItem);
        }
        
    }
}
