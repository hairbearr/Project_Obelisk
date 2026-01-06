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
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Vector2 moveInput;
        private Rigidbody2D rb2D;

        [Header("References")]
        [SerializeField] private SwordController sword;
        [SerializeField] private ShieldController shield;
        [SerializeField] private GrapplingHookController grapplingHook;

        [Header("Testing Loadout")] // remove later when you have a real loadout UI
        [SerializeField] private SigilDefinition testSwordSigil;
        [SerializeField] private SigilDefinition testShieldSigil;
        [SerializeField] private SigilDefinition testGrappleSigil;

        public Vector2 CurrentMoveInput => moveInput;

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();

            if (sword == null) sword = GetComponentInChildren<SwordController>();
            if (shield == null) shield = GetComponentInChildren<ShieldController>();
            if (grapplingHook == null) grapplingHook = GetComponentInChildren<GrapplingHookController>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            // Temp: auto-apply test sigils on spawn
            ApplyLoadout(testSwordSigil, testShieldSigil, testGrappleSigil);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            if (Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame)
            {
                Debug.Log("W pressed (raw keyboard check).");
            }

            Vector2 delta = moveInput * (moveSpeed * Time.fixedDeltaTime);
            rb2D.MovePosition(rb2D.position + delta);
        }

        // -------- New Input System Callbacks --------

        public void OnMovementInput(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed || sword == null) return;
            Vector2 attackDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : Vector2.up;
            sword.RequestUseAbility(attackDir);
            print("Attacking");
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (shield == null) return;
            shield.HandleBlockInput(context);
            print("Blocking");
        }

        public void OnGrapple(InputAction.CallbackContext context)
        {
            if (!context.performed || grapplingHook == null) return;
            Vector2 aimDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : Vector2.up;
            grapplingHook.RequestFireGrapple(aimDir);
            print("Grappling");
        }

        public void ApplyLoadout(SigilDefinition swordSigil, SigilDefinition shieldSigil, SigilDefinition grappleSigil)
        {
            if (swordSigil != null)
            {
                sword.ApplyVisualSet(swordSigil.visualSet);
                sword.SetEquippedSigil(swordSigil);
            }

            if (shieldSigil != null)
            {
                shield.ApplyVisualSet(shieldSigil.visualSet);
                shield.SetEquippedSigil(shieldSigil);
            }

            if (grappleSigil != null)
            {
                grapplingHook.ApplyVisualSet(grappleSigil.visualSet);
                grapplingHook.SetEquippedSigil(grappleSigil);
            }
        }
    }
}
