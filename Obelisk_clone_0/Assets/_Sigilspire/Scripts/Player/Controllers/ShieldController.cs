using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Combat.AbilitySystem;

namespace Combat
{
    public class ShieldController : NetworkBehaviour, IWeaponController
    {
        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;

        [Header("Shield Settings")]
        [SerializeField] private float baseMaxShieldEnergy = 100f;
        [SerializeField] private float baseRegenDelay = 2f;
        [SerializeField] private float baseRegenRate = 15f;

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer shieldRenderer;
        [SerializeField] private Transform vfxSpawnPoint;

        private GameObject blockVfxPrefab;

        public void ApplyVisualSet(WeaponVisualSet set)
        {
            if (set == null)
                return;

            if (weaponAnimator != null && set.overrideController != null)
                weaponAnimator.runtimeAnimatorController = set.overrideController;

            if (shieldRenderer != null && set.idleSprite != null)
                shieldRenderer.sprite = set.idleSprite;

            blockVfxPrefab = set.specialVfx;
        }


        private float lastHitTime;

        public NetworkVariable<float> ShieldEnergy = new NetworkVariable<float>();
        public NetworkVariable<bool> IsBroken = new NetworkVariable<bool>();

        private bool isBlocking;

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;
        }

        private void Start()
        {
            if (IsServer)
            {
                float maxEnergy = GetEffectiveMaxShieldEnergy();
                ShieldEnergy.Value = maxEnergy;
                IsBroken.Value = false;
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (!IsBroken.Value && Time.time - lastHitTime > GetEffectiveRegenDelay())
            {
                float maxEnergy = GetEffectiveMaxShieldEnergy();
                if (ShieldEnergy.Value < maxEnergy)
                {
                    ShieldEnergy.Value += GetEffectiveRegenRate() * Time.deltaTime;
                    if (ShieldEnergy.Value > maxEnergy)
                        ShieldEnergy.Value = maxEnergy;
                }
            }
        }

        public void SetEquippedSigil(SigilDefinition sigil)
        {
            equippedSigil = sigil;

            if (sigil != null)
                equippedSigilId = sigil.id;
            else
                equippedSigilId = string.Empty;
        }



        // ---------------------------------------------------------
        // Effective Stats
        // ---------------------------------------------------------

        private EffectiveAbilityStats GetCurrentStats()
        {
            if (baseAbility == null)
                return default;

            SigilProgressData progress = null;

            if (equippedSigil != null &&
                sigilInventory != null &&
                !string.IsNullOrEmpty(equippedSigilId))
            {
                progress = sigilInventory.GetOrCreateProgress(equippedSigilId);
            }

            return SigilEvaluator.GetEffectiveStats(baseAbility, equippedSigil, progress);
        }

        private float GetEffectiveMaxShieldEnergy()
        {
            float modifier = baseAbility != null ? baseAbility.shieldEnergyModifier : 0f;

            return baseMaxShieldEnergy * (1f + modifier);
        }

        private float GetEffectiveRegenDelay()
        {
            return baseRegenDelay;
        }

        private float GetEffectiveRegenRate()
        {
            return baseRegenRate;
        }

        // ---------------------------------------------------------
        // Blocking Input
        // ---------------------------------------------------------

        public void HandleBlockInput(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;

            if (context.performed)
                SetBlockingServerRpc(true);
            else if (context.canceled)
                SetBlockingServerRpc(false);
        }

        [ServerRpc]
        private void SetBlockingServerRpc(bool blocking)
        {
            isBlocking = blocking;
        }

        // ---------------------------------------------------------
        // Damage Application
        // ---------------------------------------------------------

        public void ApplyIncomingDamage(float amount)
        {
            if (!IsServer) return;

            if (!isBlocking || IsBroken.Value)
            {
                return;
            }

            lastHitTime = Time.time;
            ShieldEnergy.Value -= amount;

            if (ShieldEnergy.Value <= 0f)
            {
                ShieldEnergy.Value = 0f;
                BreakShield();
            }
        }

        private void BreakShield()
        {
            IsBroken.Value = true;
        }

        // ---------------------------------------------------------
        // Active Shield Ability (optional)
        // ---------------------------------------------------------

        public void RequestUseAbility(Vector2 inputDirection)
        {
            if (!IsOwner) return;

            UseAbilityServerRpc();
        }

        [ServerRpc]
        private void UseAbilityServerRpc()
        {
            // Implement shield bash or other shield ability here
        }
    }
}
