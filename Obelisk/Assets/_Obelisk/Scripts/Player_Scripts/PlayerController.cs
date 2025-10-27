using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : NetworkBehaviour
{
    // =========================
    // GAMEPLAY VARIABLES
    // =========================
    [Header("Gameplay Variables")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private bool isDisabled = false;
    [SerializeField] private bool isMovementLocked = false;
    [SerializeField] private bool attackCooldown = false;
    [SerializeField] private float attackComboTimer = 10f;

    // Public accessors
    public float MovementSpeed { get => movementSpeed; set => movementSpeed = value; }
    public bool IsDisabled { get => isDisabled; set => isDisabled = value; }
    public bool IsMovementLocked { get => isMovementLocked; set => isMovementLocked = value; }
    public bool AttackCooldown { get => attackCooldown; set => attackCooldown = value; }
    public float AttackComboTimer { get => attackComboTimer; set => attackComboTimer = value; }

    // =========================
    // ANIMATION VARIABLES
    // =========================
    [Header("Animation Variables")]
    [SerializeField] private Animator animator;
    private Rigidbody2D rb;
<<<<<<< HEAD
=======
    private float movementSpeedMultiplier =1f;
    [SerializeField] private Vector2 movementInput;
    [SerializeField] private float swingSpeed, swordAttackType, direction, attackComboTimer, jumpTimer;
    [SerializeField] private bool attackCooldown;
    [SerializeField] private float isMoving, isAttacking, isBlocking, isGrappling, isJumping, isClimbing, isDrinkingPotion, isGettingHit, isInteracting, isShooting, isUsingItem;
    [SerializeField] private GameObject sword, shield, grapplingHook;
    [SerializeField] private string activeSwordAbility;
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)

    private Vector2 cachedMovementInput = Vector2.zero;
    private Direction direction;

    // Animation state floats
    private float isMoving, isAttacking, swordAttackType, isBlocking,
                  isClimbing, isDrinkingPotion, isInteracting, isJumping,
                  isGrappling, isShooting, isUsingItem, isDead;

    // Cache previous animation values to avoid redundant SetFloat calls
    private float prevDirection = -1f, prevIsMoving = -1f, prevIsAttacking = -1f,
                  prevSwordAttackType = -1f, prevIsBlocking = -1f, prevIsClimbing = -1f,
                  prevIsDrinkingPotion = -1f, prevIsInteracting = -1f, prevIsJumping = -1f,
                  prevIsGrappling = -1f, prevIsShooting = -1f, prevIsUsingItem = -1f,
                  prevIsDead = -1f;

    // Shield reference (optional)
    [SerializeField] private ShieldController shield;

    // Interactables
    private InteractableObject currentInteractable;
    private bool canInteract = false;

    // Animator parameters dictionary
    private Dictionary<string, object> animatorParams = new Dictionary<string, object>();

    // =========================
    // NETWORK
    // =========================
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
<<<<<<< HEAD
            enabled = false;
=======
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
        attackComboTimer = 10;
        if (swordAttackType < 1)
        {
            swordAttackType = 1;
        }
        attackCooldown = true;
    }

    private void UseSwordAbility(string activeSwordAbility)
    {
        throw new NotImplementedException();
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
        if (Input.GetMouseButtonDown(0)&& !attackCooldown)
        {
            isAttacking = 1;
        }
        

        // Shield Block
        if (Input.GetMouseButtonDown(1))
        {
            isBlocking = 1;
            print("Actively Blocking");
<<<<<<< HEAD
=======
            movementDisabled = true;
>>>>>>> parent of 5b3f190 (Enemy Attacks)
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
<<<<<<< HEAD
            isGrappling = 1;
=======
            movementDisabled = true;
            isShooting = 1;
>>>>>>> parent of 5b3f190 (Enemy Attacks)
            print("Grappling Hook Fire");
            // play grappling hook animation
            // fire grappling hook projectile, which does all the grappling hook stuffs, including the abilities
        }
        if (Input.GetMouseButtonUp(2))
        {
<<<<<<< HEAD
            isGrappling = 0;
=======
            movementDisabled = false;
            isShooting = 0;
>>>>>>> parent of 5b3f190 (Enemy Attacks)
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
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
            return;
        }
<<<<<<< HEAD
=======

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
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
    }

    // =========================
    // UNITY CALLBACKS
    // =========================
    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponentInChildren<ShieldController>();

<<<<<<< HEAD
        // Initialize dictionary with all animator parameters
        animatorParams["Direction"] = 0f;
        animatorParams["IsMoving"] = 0f;
        animatorParams["IsAttacking"] = 0f;
        animatorParams["SwordAttackType"] = 1f;
        animatorParams["IsBlocking"] = 0f;
        animatorParams["IsClimbing"] = 0f;
        animatorParams["IsDrinkingPotion"] = 0f;
        animatorParams["IsInteracting"] = 0f;
        animatorParams["IsJumping"] = 0f;
        animatorParams["IsGrappling"] = 0f;
        animatorParams["IsShooting"] = 0f;
        animatorParams["IsUsingItem"] = 0f;
        animatorParams["IsDead"] = 0f;
    }

    private void Update()
    {
        Animate();

        // Example shield cooldown logic
        if (shield != null && isBlocking > 0)
        {
            StartCoroutine(Delay(shield.ShieldCooldownTime));
        }
    }

    private void FixedUpdate()
    {
        if (!isMovementLocked)
        {
            rb.linearVelocity = cachedMovementInput * movementSpeed;
            SetParameterFloat("IsMoving", cachedMovementInput.sqrMagnitude > 0 ? 1 : 0);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            SetParameterFloat("IsMoving", 0);
        }
=======
        isMoving = 1;
        rb.linearVelocity = movementInput * movementSpeed * movementSpeedMultiplier * Time.fixedDeltaTime;
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
    }

    // =========================
    // INPUT
    // =========================
    public void OnMove(InputAction.CallbackContext context)
    {
        if (isDisabled) return;

        Vector2 input = context.ReadValue<Vector2>();
        if (input.sqrMagnitude < 0.1f) { cachedMovementInput = Vector2.zero; return; }

        input.Normalize();
        cachedMovementInput = input;

        // Snap to 8 directions
        float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if (angle >= 337.5f || angle < 22.5f) direction = Direction.East;
        else if (angle >= 22.5f && angle < 67.5f) direction = Direction.NorthEast;
        else if (angle >= 67.5f && angle < 112.5f) direction = Direction.North;
        else if (angle >= 112.5f && angle < 157.5f) direction = Direction.NorthWest;
        else if (angle >= 157.5f && angle < 202.5f) direction = Direction.West;
        else if (angle >= 202.5f && angle < 247.5f) direction = Direction.SouthWest;
        else if (angle >= 247.5f && angle < 292.5f) direction = Direction.South;
        else if (angle >= 292.5f && angle < 337.5f) direction = Direction.SouthEast;

        SetParameterFloat("Direction", (float)direction);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (isDisabled || attackCooldown) return;
        if (context.performed) SetParameterFloat("IsAttacking", 1);
    }

    public void OnBlock(InputAction.CallbackContext context)
    {
        if (isDisabled) return;
        if (context.started)
        {
            SetParameterFloat("IsBlocking", 1);
            isMovementLocked = true;
        }
        else if (context.canceled)
        {
            SetParameterFloat("IsBlocking", 0);
            isMovementLocked = false;
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (isDisabled || !canInteract || currentInteractable == null) return;
        currentInteractable.Interact();
    }

    // =========================
    // ANIMATION HANDLING
    // =========================
    private void Animate()
    {
<<<<<<< HEAD
<<<<<<< HEAD
        foreach (var param in animatorParams)
        {
            if (param.Value is float f)
                animator.SetFloat(param.Key, f);
            else if (param.Value is bool b)
                animator.SetBool(param.Key, b);
        }
    }

    // =========================
    // PARAMETER DICTIONARY HANDLING
    // =========================
    public void SetParameterFloat(string key, float value) => animatorParams[key] = value;
    public float GetParameterFloat(string key) => animatorParams.ContainsKey(key) ? (float)animatorParams[key] : 0f;

    public void SetParameterBool(string key, bool value) => animatorParams[key] = value;
    public bool GetParameterBool(string key) => animatorParams.ContainsKey(key) ? (bool)animatorParams[key] : false;

    public Dictionary<string, object> GetAnimatorParameters() => animatorParams;

    // =========================
    // COROUTINES
    // =========================
    public IEnumerator DelayedDisable(float disableTime)
    {
        isDisabled = true;
        yield return new WaitForSeconds(disableTime);
        isDisabled = false;
    }

    private IEnumerator Delay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
    }

    // =========================
    // INTERACTABLES
    // =========================
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out InteractableObject interactable))
        {
            canInteract = true;
            currentInteractable = interactable;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out InteractableObject interactable) && interactable == currentInteractable)
        {
            canInteract = false;
            currentInteractable = null;
        }
=======
=======
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
        playerAnimator.SetFloat("MovementX", movementInput.x);
        playerAnimator.SetFloat("MovementY", movementInput.y);
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
        swordAnimator.SetFloat("MovementX", movementInput.x);
        swordAnimator.SetFloat("MovementY", movementInput.y);
        swordAnimator.SetFloat("Direction", direction);
        swordAnimator.SetFloat("IsMoving", isMoving);
        swordAnimator.SetFloat("IsAttacking", isAttacking);
        swordAnimator.SetFloat("SwordAttackType", swordAttackType);
        swordAnimator.SetFloat("IsBlocking", isBlocking);
        swordAnimator.SetFloat("IsClimbing", isClimbing);
        swordAnimator.SetFloat("IsDrinkingPotion", isDrinkingPotion);
        swordAnimator.SetFloat("IsGettingHit", isGettingHit);
        swordAnimator.SetFloat("IsInteracting", isInteracting);
        swordAnimator.SetFloat("IsJumping", isJumping);
        swordAnimator.SetFloat("IsGrappling", isGrappling);
        swordAnimator.SetFloat("IsShooting", isShooting);
        swordAnimator.SetFloat("IsUsingItem", isUsingItem);
        shieldAnimator.SetFloat("MovementX", movementInput.x);
        shieldAnimator.SetFloat("MovementY", movementInput.y);
        shieldAnimator.SetFloat("Direction", direction);
        shieldAnimator.SetFloat("IsMoving", isMoving);
        shieldAnimator.SetFloat("IsAttacking", isAttacking);
        shieldAnimator.SetFloat("SwordAttackType", swordAttackType);
        shieldAnimator.SetFloat("IsBlocking", isBlocking);
        shieldAnimator.SetFloat("IsClimbing", isClimbing);
        shieldAnimator.SetFloat("IsDrinkingPotion", isDrinkingPotion);
        shieldAnimator.SetFloat("IsGettingHit", isGettingHit);
        shieldAnimator.SetFloat("IsInteracting", isInteracting);
        shieldAnimator.SetFloat("IsJumping", isJumping);
        shieldAnimator.SetFloat("IsGrappling", isGrappling);
        shieldAnimator.SetFloat("IsShooting", isShooting);
        shieldAnimator.SetFloat("IsUsingItem", isUsingItem);
        grapplingHookAnimator.SetFloat("MovementX", movementInput.x);
        grapplingHookAnimator.SetFloat("MovementY", movementInput.y);
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
<<<<<<< HEAD
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
=======
>>>>>>> parent of 0509b8a (Started enemy pathfinding and scripts)
    }
}
