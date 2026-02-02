using Combat;
using Combat.AbilitySystem;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private PlayerAnimationDriver animationDriver;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Vector2 moveInput;
        private Rigidbody2D rb2D;
        private bool isShieldingLocal;
        private Vector2 lastFacingDir = Vector2.up;

        [Header("Loadout")]
        [SerializeField] private PlayerLoadout loadout;

        [Header("Testing Loadout - TEMP")]
        [SerializeField] private SigilDefinition testSwordSigil;
        [SerializeField] private SigilDefinition testShieldSigil;
        [SerializeField] private SigilDefinition testGrappleSigil;


        [Header("References")]
        [SerializeField] private SwordController sword;
        [SerializeField] private ShieldController shield;
        [SerializeField] private GrapplingHookController grapplingHook;

        public Vector2 CurrentMoveInput => moveInput;

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();

            if (animationDriver == null) animationDriver = GetComponent<PlayerAnimationDriver>();
            if (sword == null) sword = GetComponentInChildren<SwordController>();
            if (shield == null) shield = GetComponentInChildren<ShieldController>();
            if (grapplingHook == null) grapplingHook = GetComponentInChildren<GrapplingHookController>();

            if(loadout == null) loadout = GetComponentInParent<PlayerLoadout>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            // Temp: auto-apply test sigils on spawn (remove when you have UI)
            if (loadout != null && (testSwordSigil != null || testShieldSigil != null || testGrappleSigil != null))
            {
                loadout.EquipSigilsForTesting(testSwordSigil, testShieldSigil, testGrappleSigil);
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (isShieldingLocal)
            {
                // hard stop input so animations also go idle
                moveInput = Vector2.zero;
                return;
            }

            Vector2 delta = moveInput * (moveSpeed * Time.fixedDeltaTime);
            rb2D.MovePosition(rb2D.position + delta);
        }

        // -------- New Input System Callbacks --------

        public void OnMovementInput(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();

            if (moveInput.sqrMagnitude > 0.01f) lastFacingDir = moveInput.normalized;
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            Vector2 dir = LastFacingDir;

            if (isShieldingLocal)
            {
                if (shield == null) return;

                if (!shield.CanUseAbility())
                {
                    float rem = shield.GetCooldownRemaining();
                    return;
                }

                shield.RequestUseAbility(dir);
                // Play a distinct shield ability animation (Not Raise/Lower)
                // if (animationDrive != null) animationDriver.PlayShieldAbility();
                return;
            }

            // Normal sword attack
            if (sword == null) return;

            if (!sword.CanUseAbility())
            {
                float rem = sword.GetCooldownRemaining();
                return;
            }

            sword.RequestUseAbility(dir);

            if (animationDriver != null) animationDriver.PlaySwordSlash();
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (shield == null) return;

            if (context.performed)
            {
                SetMovementLocked(true);

                if (animationDriver != null)
                {
                    animationDriver.SetShielding(true);
                    animationDriver.PlayRaiseShield();
                }
            }
            else if (context.canceled)
            {
                SetMovementLocked(false);

                if (animationDriver != null)
                {
                    animationDriver.SetShielding(false);
                    animationDriver.PlayLowerShield();
                }
            }


            shield.HandleBlockInput(context);
        }

        public void OnGrapple(InputAction.CallbackContext context)
        {
            if (!context.performed) return;

            // No grapple while shielding
            if (isShieldingLocal) return;

            if (grapplingHook == null) return;

            if (!grapplingHook.CanUseAbility())
            {
                return;
            }

            grapplingHook.RequestFireGrapple(LastFacingDir);

            if (animationDriver != null) animationDriver.PlayGrappleCast();
        }

        public void SetMovementLocked(bool locked)
        {
            isShieldingLocal = locked;
            if (locked) moveInput = Vector2.zero;
        }

        public Vector2 LastFacingDir
        {
            get { return lastFacingDir; }
        }
    }
}
