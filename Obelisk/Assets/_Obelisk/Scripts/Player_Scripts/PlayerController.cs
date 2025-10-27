using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float movementSpeed =5;
    [SerializeField] private Animator animator;
    private Rigidbody2D rb;
    private float movementSpeedMultiplier =1f;

    private Vector2 cachedMovementInput = Vector2.zero;

    [SerializeField] private Direction direction;

    [SerializeField] private float isMoving, isAttacking, isBlocking, isGrappling, isJumping, isClimbing, isDrinkingPotion, isGettingHit, isInteracting, isShooting, isUsingItem, isDead, swordAttackType, attackComboTimer;
    [SerializeField] private bool attackCooldown, movementDisabled, cantShield, isDisabled;
    [SerializeField] private GameObject sword, grapplingHook;
    [SerializeField] ShieldController shield;
    //[SerializeField] private string activeSwordAbility;


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
        shield = GetComponentInChildren<ShieldController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsDisabled)
        {
            Controls();
            ReadMovementInput();
        }
        Animate();
        
    }

    private void StopJump()
    {
        isJumping = 0;
    }
    private void Jump()
    {
        // input jump mechanics here
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

            if (shield != null)
            {
                StartCoroutine(Delay(shield.ShieldCooldownTime));
            }
            cantShield = false;
        }
    }

    private void ReadMovementInput()
    {
        
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if(input.sqrMagnitude < 0.1f) // deadzone threshold
        {
            cachedMovementInput = Vector2.zero;
            return;
        }
        input.Normalize();

        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if(angle >= 337.5f || angle < 22.5f)
        {
            direction = Direction.East;
            cachedMovementInput = Vector2.right;
        }
        else if(angle >= 22.5f && angle < 67.5f)
        {
            direction = Direction.NorthEast;
            cachedMovementInput = new Vector2(1, 1).normalized;
        }
        else if(angle >= 67.5f && angle < 112.5f)
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

    private void FixedUpdate()
    {
        if (!movementDisabled)
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

    private void Animate()
    {
        animator.SetFloat("Direction", (float)direction);
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
