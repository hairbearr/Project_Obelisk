using Combat; // for IWeaponController + weapon controllers
using Combat.AbilitySystem;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        private Vector2 moveInput;

        [Header("References")]
        [SerializeField] private SwordController sword;
        [SerializeField] private ShieldController shield;
        [SerializeField] private GrapplingHookController grapplingHook;

        [Header("Testing")] // Delete this after you get a loadout screen set up
        [SerializeField] private SigilDefinition testSwordSigil;
        [SerializeField] private SigilDefinition testShieldSigil;
        [SerializeField] private SigilDefinition testGrappleSigil;

        private void Awake()
        {
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

            if (IsOwner)
            {
                ApplyLoadout(testSwordSigil, testShieldSigil, testGrappleSigil); // delete this after you get a loadout screen set up
            }
        }

        private void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y) * (moveSpeed * Time.deltaTime);
            transform.Translate(move, Space.World);

            if (move != Vector3.zero)
                transform.forward = move.normalized;
        }

        // ========= New Input System Callbacks =========

        public void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (!context.performed || sword == null) return;

            Vector2 attackDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : Vector2.up;
            sword.RequestUseAbility(attackDir);
        }

        public void OnBlock(InputAction.CallbackContext context)
        {
            if (shield == null) return;
            shield.HandleBlockInput(context);
        }

        public void OnGrapple(InputAction.CallbackContext context)
        {
            if (!context.performed || grapplingHook == null) return;

            Vector2 aimDir = moveInput.sqrMagnitude > 0.01f ? moveInput.normalized : Vector2.up;
            grapplingHook.RequestFireGrapple(aimDir);
        }

        public void ApplyLoadout(SigilDefinition swordSigil, SigilDefinition shieldSigil, SigilDefinition grappleSigil)
        {
            sword.ApplyVisualSet(swordSigil.visualSet);
            shield.ApplyVisualSet(shieldSigil.visualSet);
            grapplingHook.ApplyVisualSet(grappleSigil.visualSet);

            // store equipped sigils so abilities scale correctly
            sword.SetEquippedSigil(swordSigil);
            shield.SetEquippedSigil(shieldSigil);
            grapplingHook.SetEquippedSigil(grappleSigil);
        }

    }
}
