using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Sigilspire.Player
{
    /// <summary>
    /// Handles player movement, body animations, and delegates attacks to weapon controllers.
    /// Integrates Netcode for GameObjects for multiplayer authority.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 5f;
        private Vector2 moveInput;
        private Rigidbody2D rb;

        [Header("Animation")]
        public Animator bodyAnimator; // Animator for the player's body
        private Direction currentDirection;

        // Allow other scripts to get/set the current facing direction
        public Direction CurrentDirection { get => currentDirection; set => currentDirection = value; }

        [Header("Weapon Controllers")]
        public PlayerSwordController swordController;
        public PlayerShieldController shieldController;
        public PlayerGrapplingHookController grappleController;

        [Header("Input Actions")]
        public InputAction moveAction;
        public InputAction attackAction;
        public InputAction shieldAction;
        public InputAction grappleAction;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                // Only the owning client processes input
                enabled = false;
            }
        }

        private void OnEnable()
        {
            moveAction.Enable();
            attackAction.Enable();
            shieldAction.Enable();
            grappleAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            attackAction.Disable();
            shieldAction.Disable();
            grappleAction.Disable();
        }

        private void Update()
        {
            // Update movement input
            moveInput = moveAction.ReadValue<Vector2>();

            // Update direction for animations and weapon controllers
            UpdateDirection(moveInput);

            // Update body animator for movement
            bodyAnimator.SetFloat("Horizontal", moveInput.x);
            bodyAnimator.SetFloat("Vertical", moveInput.y);
            bodyAnimator.SetFloat("Speed", moveInput.sqrMagnitude);

            // Handle attack inputs
            if (IsAttackPressed())
            {
                PerformSwordAttackServerRpc();
            }

            if (shieldAction.triggered)
            {
                PerformShieldAttackServerRpc();
            }

            if (grappleAction.triggered)
            {
                PerformGrappleServerRpc();
            }
        }

        private void FixedUpdate()
        {
            // Move the player using Rigidbody2D for physics integration
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }

        /// <summary>
        /// Helper function to check if the attack button is pressed.
        /// Abstracts input system handling.
        /// </summary>
        public bool IsAttackPressed()
        {
            return attackAction.triggered;
        }

        /// <summary>
        /// Updates the player's facing direction based on input.
        /// Sets CurrentDirection for weapon controllers and body animator.
        /// </summary>
        private void UpdateDirection(Vector2 input)
        {
            if (input == Vector2.zero) return;

            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f; // Normalize angle to [0,360)

            // Map angle to Direction enum (8 directions)
            if (angle >= 337.5f || angle < 22.5f) currentDirection = Direction.East;
            else if (angle >= 22.5f && angle < 67.5f) currentDirection = Direction.NorthEast;
            else if (angle >= 67.5f && angle < 112.5f) currentDirection = Direction.North;
            else if (angle >= 112.5f && angle < 157.5f) currentDirection = Direction.NorthWest;
            else if (angle >= 157.5f && angle < 202.5f) currentDirection = Direction.West;
            else if (angle >= 202.5f && angle < 247.5f) currentDirection = Direction.SouthWest;
            else if (angle >= 247.5f && angle < 292.5f) currentDirection = Direction.South;
            else currentDirection = Direction.SouthEast;

            // Update body animator's direction parameter
            bodyAnimator.SetFloat("Direction", (float)currentDirection);
        }

        // -------------------------------
        // SERVER RPCs for attacks
        // -------------------------------

        [ServerRpc]
        private void PerformSwordAttackServerRpc(ServerRpcParams rpcParams = default)
        {
            // SwordController handles actual attack logic, damage, cooldown
            swordController.PerformAttack(currentDirection);

            // Notify all clients to play visual/audio feedback
            PlaySwordAttackClientRpc(currentDirection);
        }

        [ClientRpc]
        private void PlaySwordAttackClientRpc(Direction dir, ClientRpcParams rpcParams = default)
        {
            swordController.PlayAttackAnimation(dir);
        }

        [ServerRpc]
        private void PerformShieldAttackServerRpc(ServerRpcParams rpcParams = default)
        {
            shieldController.StartBlock(); // Server-authoritative start of block
            PlayShieldAttackClientRpc(currentDirection);
        }

        [ClientRpc]
        private void PlayShieldAttackClientRpc(Direction dir, ClientRpcParams rpcParams = default)
        {
            shieldController.shieldAnimator.SetFloat("Direction", (float)dir);
            shieldController.shieldAnimator.SetBool("IsBlocking", true);
        }

        [ServerRpc]
        private void PerformGrappleServerRpc(ServerRpcParams rpcParams = default)
        {
            grappleController.PerformGrapple(currentDirection);
            PlayGrappleClientRpc(currentDirection);
        }

        [ClientRpc]
        private void PlayGrappleClientRpc(Direction dir, ClientRpcParams rpcParams = default)
        {
            grappleController.PlayGrappleAnimationClientRpc(dir);
        }
    }
}

