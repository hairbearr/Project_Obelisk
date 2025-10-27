using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed =5;
    [SerializeField] private Animator animator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier =1f;

    private Vector2 cachedMovementInput = Vector2.zero;

    [SerializeField] private Direction direction;

    [SerializeField] private float isMoving, isAttacking, isBlocking, isGrappling, isJumping, isClimbing, isDrinkingPotion, isGettingHit, isInteracting, isShooting, isUsingItem, isDead, swordAttackType, attackComboTimer;
    [SerializeField] private bool attackCooldown, isMovementLocked, cantShield, isDisabled, canInteract = false;
    [SerializeField] private GameObject sword, grapplingHook;
    [SerializeField] ShieldController shield;
    private InteractableObject currentInteractable;
    //[SerializeField] private string activeSwordAbility;

    // cached Animation variables
    private float prevDirection = -1f;
    private float prevIsMoving = -1f;
    private float prevIsAttacking = -1f;
    private float prevSwordAttackType = -1f;
    private float prevIsBlocking = -1f;
    private float prevIsClimbing = -1f;
    private float prevIsDrinkingPotion = -1f;
    private float prevIsInteracting = -1f;
    private float prevIsJumping = -1f;
    private float prevIsGrappling = -1f;
    private float prevIsShooting = -1f;
    private float prevIsUsingItem = -1f;
    private float prevIsDead = -1f;


    public  float MovementSpeedMultiplier
    {
        get { return movementSpeedMultiplier; }
        set { movementSpeedMultiplier = value; }
    }
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
    public Direction Direction
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
    public bool IsMovementLocked
    {
        get { return isMovementLocked; }
        set { isMovementLocked = value; }
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
        shield = GetComponentInChildren<ShieldController>();
    }

    // Update is called once per frame
    void Update()
    {
        Animate();

        if (cantShield)
        {
            isBlocking = 0;

            if (shield != null)
            {
                StartCoroutine(Delay(shield.ShieldCooldownTime));
            }
            cantShield = false;
        }
    }

    private void FixedUpdate()
    {
        if (!isMovementLocked)
        {
            rb.linearVelocity = cachedMovementInput * movementSpeed * movementSpeedMultiplier;
            isMoving = cachedMovementInput.sqrMagnitude > 0 ? 1 : 0;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = 0;
        }
    }

    private void StopJump()
    {
        isJumping = 0;
    }
    private void Jump()
    {
        // input jump mechanics here
    }
    
    public void OnMove(InputAction.CallbackContext context)
    {
        if (IsDisabled) return;

        Vector2 input = context.ReadValue<Vector2>();

        if(input.sqrMagnitude <0.1f) // deadzone threshold
        {
            cachedMovementInput = Vector2.zero;
            return;
        }

        input.Normalize(); // normalize input

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg; // calculate angle in degrees
        if (angle < 0) angle += 360f;

        // snap to 8 directions
        if (angle >= 337.5f || angle < 22.5f)
        {
            direction = Direction.East;
            cachedMovementInput = Vector2.right;
        }
        else if (angle >= 22.5f && angle < 67.5f)
        {
            direction = Direction.NorthEast;
            cachedMovementInput = new Vector2(1, 1).normalized;
        }
        else if (angle >= 67.5f && angle < 112.5f)
        {
            direction = Direction.North;
            cachedMovementInput = Vector2.up;
        }
        else if (angle >= 112.5f && angle < 157.5f)
        {
            direction = Direction.NorthWest;
            cachedMovementInput = new Vector2(-1, 1).normalized;
        }
        else if (angle >= 157.5f && angle < 202.5f)
        {
            direction = Direction.West;
            cachedMovementInput = Vector2.left;
        }
        else if (angle >= 202.5f && angle < 247.5f)
        {
            direction = Direction.SouthWest;
            cachedMovementInput = new Vector2(-1, -1).normalized;
        }
        else if (angle >= 247.5f && angle < 292.5f)
        {
            direction = Direction.South;
            cachedMovementInput = Vector2.down;
        }
        else if (angle >= 292.5f && angle < 337.5f)
        {
            direction = Direction.SouthEast;
            cachedMovementInput = new Vector2(1, -1).normalized;
        }

    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if(IsDisabled) return;

        if (context.performed && !attackCooldown)
        {
            isAttacking = 1;
        }
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (IsDisabled) return;

        if (context.started && !cantShield)
        {
            isBlocking = 1;
            isMovementLocked = true;
            Debug.Log("Actively Blocking");
        }
        else if (context.canceled)
        {
            isBlocking = 0;
            isMovementLocked = false;
            Debug.Log("No Longer Blocking");
        }
    }

    public void OnShoot(InputAction.CallbackContext context)
    {
        if (IsDisabled) return;
        
        if (context.started)
        {
            isShooting = 1;
            Debug.Log("Grappling Hook Fire");
        }
        else if (context.canceled)
        {
            isShooting = 0;
            Debug.Log("Grappling Hook No Longer Firing");
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (IsDisabled) return;
        
        if (context.performed)
        {
            isJumping = 1;
            Debug.Log("Jumping");
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (IsDisabled) return;

        if(canInteract && currentInteractable != null)
        {
            currentInteractable.Interact();
        }

    }
    private void Animate()
    {
        if (prevDirection != (float)direction)
        {
            animator.SetFloat("Direction", (float)direction);
            prevDirection = (float)direction;
        }
        if (prevIsMoving != isMoving)
        {
            animator.SetFloat("IsMoving", isMoving);
            prevIsMoving = isMoving;
        }
        if (prevIsAttacking != isAttacking)
        {
            animator.SetFloat("IsAttacking", isAttacking);
            prevIsAttacking = isAttacking;
        }
        if (prevSwordAttackType != swordAttackType)
        {
            animator.SetFloat("SwordAttackType", swordAttackType);
            prevSwordAttackType = swordAttackType;
        }
        if (prevIsBlocking != isBlocking)
        {
            animator.SetFloat("IsBlocking", isBlocking);
            prevIsBlocking = isBlocking;
        }
        if (prevIsClimbing != isClimbing)
        {
            animator.SetFloat("IsClimbing", isClimbing);
            prevIsClimbing = isClimbing;
        }
        if (prevIsDrinkingPotion != isDrinkingPotion)
        {
            animator.SetFloat("IsDrinkingPotion", isDrinkingPotion);
            prevIsDrinkingPotion = isDrinkingPotion;
        }
        if (prevIsInteracting != isInteracting)
        {
            animator.SetFloat("IsInteracting", isInteracting);
            prevIsInteracting = isInteracting;
        }
        if (prevIsJumping != isJumping)
        {
            animator.SetFloat("IsJumping", isJumping);
            prevIsJumping = isJumping;
        }
        if (prevIsGrappling != isGrappling)
        {
            animator.SetFloat("IsGrappling", isGrappling);
            prevIsGrappling = isGrappling;
        }
        if (prevIsShooting != isShooting)
        {
            animator.SetFloat("IsShooting", isShooting);
            prevIsShooting = isShooting;
        }
        if (prevIsUsingItem != isUsingItem)
        {
            animator.SetFloat("IsUsingItem", isUsingItem);
            prevIsUsingItem = isUsingItem;
        }
        if (prevIsDead != isDead)
        {
            animator.SetFloat("IsDead", isDead);
            prevIsDead = isDead;
        }
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Interactables
        if(collision.TryGetComponent(out InteractableObject interactable))
        {
            canInteract = true;
            currentInteractable = interactable;
            Debug.Log("Press Interact to use: " + interactable.name);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // Interactables
        if(collision.TryGetComponent(out InteractableObject interactable) && interactable == currentInteractable)
        {
            canInteract = false;
            currentInteractable = null;
        }
    }

}



//private void Controls()
//{
//    // Swing Sword
//    if (Input.GetMouseButtonDown(0)&& !attackCooldown)
//    {
//        isAttacking = 1;
//    }


//    // Shield Block
//    if (Input.GetMouseButtonDown(1)&& !cantShield)
//    {
//        isBlocking = 1;
//        print("Actively Blocking");
//        movementDisabled = true;
//    }
//    if (Input.GetMouseButtonUp(1))
//    {
//        movementDisabled = false;
//        isBlocking = 0;
//        print("No Longer Blocking");
//    }

//    // Fire Grappling Hook
//    if (Input.GetMouseButtonDown(2))
//    {
//        isShooting = 1;
//        print("Grappling Hook Fire");
//    }
//    if (Input.GetMouseButtonUp(2))
//    {
//        isShooting = 0;
//        print("Grappling Hook No Longer Firing");
//    }

//    // Jump
//    if (Input.GetButtonDown("Jump"))
//    {
//        isJumping = 1;
//        print("Jumping");
//        // play jump animation
//        // do jump mechanics?
//    }

//    if (cantShield)
//    {

//        isBlocking = 0;

//        if (shield != null)
//        {
//            StartCoroutine(Delay(shield.ShieldCooldownTime));
//        }
//        cantShield = false;
//    }
//}

//private void ReadMovementInput()
//{

//    Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

//    if(input.sqrMagnitude < 0.1f) // deadzone threshold
//    {
//        cachedMovementInput = Vector2.zero;
//        return;
//    }
//    input.Normalize();

//    float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
//    if (angle < 0) angle += 360f;

//    if(angle >= 337.5f || angle < 22.5f)
//    {
//        direction = Direction.East;
//        cachedMovementInput = Vector2.right;
//    }
//    else if(angle >= 22.5f && angle < 67.5f)
//    {
//        direction = Direction.NorthEast;
//        cachedMovementInput = new Vector2(1, 1).normalized;
//    }
//    else if(angle >= 67.5f && angle < 112.5f)
//    {
//        direction = Direction.North;
//        cachedMovementInput = Vector2.up;
//    }
//    else if (angle >= 112.5f && angle < 157.5f)
//    {
//        direction = Direction.NorthWest;
//        cachedMovementInput = new Vector2(-1, 1).normalized;
//    }
//    else if (angle >= 157.5f && angle < 202.5f)
//    {
//        direction = Direction.West;
//        cachedMovementInput = Vector2.left;
//    }
//    else if (angle >= 202.5f && angle < 247.5f)
//    {
//        direction = Direction.SouthWest;
//        cachedMovementInput = new Vector2(-1, -1).normalized;
//    }
//    else if (angle >= 247.5f && angle < 292.5f)
//    {
//        direction = Direction.South;
//        cachedMovementInput = Vector2.down;
//    }
//    else if (angle >= 292.5f && angle < 337.5f)
//    {
//        direction = Direction.SouthEast;
//        cachedMovementInput = new Vector2(1, -1).normalized;
//    }

//}


