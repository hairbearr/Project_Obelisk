using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed =5;

    [SerializeField] private Animator animator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier =1f;
    [SerializeField] private Vector2 movementInput;
    [SerializeField] private float swingSpeed, swordAttackType, direction, attackComboTimer, jumpTimer, isDead;
    [SerializeField] private bool attackCooldown, movementDisabled, cantShield, isDisabled;
    [SerializeField] private float isMoving, isAttacking, isBlocking, isGrappling, isJumping, isClimbing, isDrinkingPotion, isGettingHit, isInteracting, isShooting, isUsingItem;
    [SerializeField] private GameObject sword, shield, grapplingHook;
    [SerializeField] private string activeSwordAbility;

    public float IsDead
    {
        get { return isDead; }
        set { isDead = value; }
    }
    public bool CantShield
    {
        get { return cantShield; }
        set { cantShield = value; }
    }
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
    public bool IsDisabled
    {
        get { return isDisabled; }
        set { isDisabled = value; }
    }
    public bool IsMovementDisabled
    {
        get { return movementDisabled; }
        set { movementDisabled = value; }
    }
    public float AttackComboTimer
    {
        get { return attackComboTimer; }
        set { attackComboTimer = value; }
    }
    public bool AttackCooldown
    {
        get { return attackCooldown; }
        set { attackCooldown = value; }
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
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsDisabled)
        {
            Controls();
            Move();
        }
        Animate();
        
    }

    private void StopJump()
    {
        isJumping = 0;
    }
    private void Jump()
    {

    }
    
    private void Controls()
    {
        // Swing Sword
        if (Input.GetMouseButtonDown(0)&& !attackCooldown)
        {
            isAttacking = 1;
        }
        

        // Shield Block
        if (Input.GetMouseButtonDown(1)&& !cantShield)
        {
            isBlocking = 1;
            print("Actively Blocking");
            movementDisabled = true;
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
            isShooting = 1;
            print("Grappling Hook Fire");
        }
        if (Input.GetMouseButtonUp(2))
        {
            isShooting = 0;
            print("Grappling Hook No Longer Firing");
        }

        // Jump
        if (Input.GetButtonDown("Jump"))
        {
            isJumping = 1;
            print("Jumping");
            // play jump animation
            // do jump mechanics?
        }

        if (cantShield)
        {
            isBlocking = 0;
            Delay(shield.GetComponent<ShieldController>().ShieldCooldownTime);
            cantShield = false;
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
        animator.SetFloat("Direction", direction);
        animator.SetFloat("IsMoving", isMoving);
        animator.SetFloat("IsAttacking", isAttacking);
        animator.SetFloat("SwordAttackType", swordAttackType);
        animator.SetFloat("IsBlocking", isBlocking);
        animator.SetFloat("IsClimbing", isClimbing);
        animator.SetFloat("IsDrinkingPotion", isDrinkingPotion);
        animator.SetFloat("IsInteracting", isInteracting);
        animator.SetFloat("IsJumping", isJumping);
        animator.SetFloat("IsGrappling", isGrappling);
        animator.SetFloat("IsShooting", isShooting);
        animator.SetFloat("IsUsingItem", isUsingItem);
        animator.SetFloat("IsDead", isDead);
    }

    public virtual IEnumerator DelayedDisable(float disableTime)
    {
        IsDisabled = true;
        yield return new WaitForSeconds(disableTime);
        IsDisabled = false;
    }

    IEnumerator Delay (float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
    }
}
