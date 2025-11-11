using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour, InputMap.IPlayerActions
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

    private InputMap inputMap;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Initialize Input System
        inputMap = new InputMap();
        inputMap.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        inputMap.Player.Enable();
    }

    private void OnDisable()
    {
        inputMap.Player.Disable();
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
        if (movementInput == Vector2.zero)
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = 0;
            return;
        }

        float horizontal = Mathf.RoundToInt(movementInput.x);
        float vertical = Mathf.RoundToInt(movementInput.y);

        // 8-direction direction assignment (same as before)
        if (horizontal == 1 && vertical == 0) direction = 0;
        else if (horizontal == 1 && vertical == 1) direction = 1;
        else if (horizontal == 0 && vertical == 1) direction = 2;
        else if (horizontal == -1 && vertical == 1) direction = 3;
        else if (horizontal == -1 && vertical == 0) direction = 4;
        else if (horizontal == -1 && vertical == -1) direction = 5;
        else if (horizontal == 0 && vertical == -1) direction = 6;
        else if (horizontal == 1 && vertical == -1) direction = 7;

        if (!movementDisabled)
        {
            isMoving = 1;
            rb.linearVelocity = movementInput * movementSpeed * movementSpeedMultiplier * Time.fixedDeltaTime;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
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

    #region Input Callbacks (from InputMap.IPlayerActions)
    public void OnMovementInput(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started && !attackCooldown)
        {
            isAttacking = 1;
        }
        else if (context.canceled)
        {
            isAttacking = 0;
        }
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (context.started && !cantShield)
        {
            isBlocking = 1;
            movementDisabled = true;
            Debug.Log("Actively Blocking");
        }
        else if (context.canceled)
        {
            isBlocking = 0;
            movementDisabled = false;
            Debug.Log("No Longer Blocking");
        }
    }

    public void OnGrapple(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isShooting = 1;
            Debug.Log("Grappling Hook Fire");
        }
        else if (context.canceled)
        {
            isShooting = 0;
            Debug.Log("Grappling Hook Released");
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isJumping = 1;
            Debug.Log("Jump!");
        }
        else if (context.canceled)
        {
            isJumping = 0;
        }
    }

    public void OnMenuInput(InputAction.CallbackContext context)
    {
        // Optional: handle pause/menu toggle
        if (context.started)
        {
            Debug.Log("Menu Button Pressed");
        }
    }
    #endregion
}

