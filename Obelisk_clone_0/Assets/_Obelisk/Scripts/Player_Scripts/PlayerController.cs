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
            enabled = false;
            return;
        }
    }

    // =========================
    // UNITY CALLBACKS
    // =========================
    private void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        shield = GetComponentInChildren<ShieldController>();

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
    }
}
