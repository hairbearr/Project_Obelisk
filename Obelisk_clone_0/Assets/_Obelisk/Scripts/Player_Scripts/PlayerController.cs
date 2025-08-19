using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Central player controller handling input, movement, and coordinating body and weapon animations.
/// Works with all PlayerWeaponControllers (Sword, Shield, Grappling Hook).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Vector2 movement;

    [Header("Animation")]
    [SerializeField] private Animator bodyAnimator; // Player body animator
    [SerializeField] private Direction direction;   // Current facing direction

    [Header("Weapon Controllers")]
    public PlayerSwordController swordController;
    public PlayerShieldController shieldController;
    public PlayerGrapplingHookController grapplingHookController;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!IsOwner) return; // Only process input for the owning client

        HandleInput();
        UpdateDirection();
        UpdateAnimations();
        HandleWeapons();
    }

    /// <summary>
    /// Reads input axes and buttons for movement and actions.
    /// </summary>
    private void HandleInput()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Normalize to prevent faster diagonal movement
        if (movement.sqrMagnitude > 1)
            movement.Normalize();

        // Attack input
        if (Input.GetButtonDown("Fire1"))
        {
            swordController.PerformAttack(direction);
        }

        // Shield input
        if (Input.GetButton("Fire2"))
        {
            shieldController.PerformBlock(direction);
        }

        // Grappling hook input
        if (Input.GetButtonDown("Fire3"))
        {
            grapplingHookController.PerformGrapple(direction);
        }

        // Additional inputs like use item, potion, interact can be added similarly
    }

    /// <summary>
    /// Updates the player's facing direction based on movement.
    /// </summary>
    private void UpdateDirection()
    {
        if (movement != Vector2.zero)
        {
            float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
            // Convert angle to Direction enum
            if (angle > 67.5f && angle <= 112.5f) direction = Direction.North;
            else if (angle > 22.5f && angle <= 67.5f) direction = Direction.NorthEast;
            else if (angle > -22.5f && angle <= 22.5f) direction = Direction.East;
            else if (angle > -67.5f && angle <= -22.5f) direction = Direction.SouthEast;
            else if (angle > -112.5f && angle <= -67.5f) direction = Direction.South;
            else if (angle > -157.5f && angle <= -112.5f) direction = Direction.SouthWest;
            else if (angle > 112.5f && angle <= 157.5f) direction = Direction.NorthWest;
            else direction = Direction.West;
        }
    }

    /// <summary>
    /// Moves the player rigidbody based on input.
    /// </summary>
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        rb.linearVelocity = movement * moveSpeed;
    }

    /// <summary>
    /// Updates the body animator for all animation states based on movement and actions.
    /// </summary>
    private void UpdateAnimations()
    {
        // Movement
        bodyAnimator.SetFloat("Horizontal", movement.x);
        bodyAnimator.SetFloat("Vertical", movement.y);
        bodyAnimator.SetFloat("Speed", movement.sqrMagnitude);

        // Pass direction for directional animations
        bodyAnimator.SetFloat("Direction", (float)direction);

        // Example: idle if no movement
        bodyAnimator.SetBool("IsMoving", movement.sqrMagnitude > 0);
    }

    /// <summary>
    /// Calls update functions for each weapon controller to manage their animations and abilities.
    /// </summary>
    private void HandleWeapons()
    {
        swordController.UpdateWeaponAnimator(direction);
        shieldController.UpdateWeaponAnimator(direction);
        grapplingHookController.UpdateWeaponAnimator(direction);
    }
}
