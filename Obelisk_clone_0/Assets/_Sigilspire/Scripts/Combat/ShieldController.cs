using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Combat.AbilitySystem;

namespace Combat
{
    public class ShieldController : NetworkBehaviour, IWeaponController
    {
        #region Inspector - Animator Names

        [Header("Animator Names")]
        [SerializeField] private string raiseStateName = "RaiseShield";
        [SerializeField] private string raiseTriggerName = "RaiseShield";
        [SerializeField] private string lowerTriggerName = "LowerShield";

        #endregion

        #region Inspector - Ability / Sigil

        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [SerializeField] private bool enforceAbilityCooldown = true;
        private float lastAbilityUseTimeLocal = -9999f;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;

        #endregion

        #region Inspector - Shield Settings

        [Header("Shield Settings")]
        [SerializeField] private float baseMaxShieldEnergy = 100f;
        [SerializeField] private float baseRegenDelay = 2f;
        [SerializeField] private float baseRegenRate = 15f;

        #endregion

        #region Inspector - Visual References

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer shieldRenderer;
        [SerializeField] private Transform vfxSpawnPoint;

        #endregion

        #region Network Variables

        public NetworkVariable<float> ShieldEnergy = new NetworkVariable<float>();
        public NetworkVariable<bool> IsBroken = new NetworkVariable<bool>();

        #endregion

        #region Runtime State

        private GameObject blockVfxPrefab;

        private float lastHitTime;

        private bool isBlocking;       // authoritative (server)
        private bool localIsBlocking;  // owner-only input state for freezing anim

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;
        }

        private void Start()
        {
            if (!IsServer) return;

            float maxEnergy = GetEffectiveMaxShieldEnergy();
            ShieldEnergy.Value = maxEnergy;
            IsBroken.Value = false;
        }

        private void Update()
        {
            if (!IsServer) return;

            if (IsBroken.Value) return;

            if (Time.time - lastHitTime <= GetEffectiveRegenDelay()) return;

            float maxEnergy = GetEffectiveMaxShieldEnergy();
            if (ShieldEnergy.Value >= maxEnergy) return;

            ShieldEnergy.Value += GetEffectiveRegenRate() * Time.deltaTime;
            if (ShieldEnergy.Value > maxEnergy)
                ShieldEnergy.Value = maxEnergy;
        }

        private void LateUpdate()
        {
            // Owner-only: freeze the raise animation on the last frame while held.
            if (!IsOwner) return;
            if (weaponAnimator == null) return;
            if (!localIsBlocking) return;

            AnimatorStateInfo s = weaponAnimator.GetCurrentAnimatorStateInfo(0);

            if (s.IsName(raiseStateName) && s.normalizedTime >= 1f)
            {
                weaponAnimator.speed = 0f;
                weaponAnimator.Play(raiseStateName, 0, 1f);
            }
        }

        #endregion

        #region Visual Set / Sigil

        public void ApplyVisualSet(WeaponVisualSet set)
        {
            if (set == null) return;

            if (weaponAnimator != null && set.overrideController != null)
                weaponAnimator.runtimeAnimatorController = set.overrideController;

            if (shieldRenderer != null && set.idleSprite != null)
                shieldRenderer.sprite = set.idleSprite;

            blockVfxPrefab = set.specialVfx;
        }

        public void SetEquippedSigil(SigilDefinition sigil)
        {
            equippedSigil = sigil;
            equippedSigilId = sigil != null ? sigil.id : string.Empty;
        }

        #endregion

        #region Effective Stats

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

        #endregion

        #region Blocking Input (Owner -> Server)

        public void HandleBlockInput(InputAction.CallbackContext context)
        {
            if (!IsOwner) return;

            if (context.performed)
            {
                localIsBlocking = true;

                if (weaponAnimator != null)
                {
                    weaponAnimator.speed = 1f;
                    weaponAnimator.ResetTrigger(lowerTriggerName);
                    weaponAnimator.SetTrigger(raiseTriggerName);
                }

                SetBlockingServerRpc(true);
            }
            else if (context.canceled)
            {
                localIsBlocking = false;

                SetBlockingServerRpc(false);

                if (weaponAnimator != null)
                {
                    weaponAnimator.speed = 1f;
                    weaponAnimator.ResetTrigger(raiseTriggerName);
                    weaponAnimator.SetTrigger(lowerTriggerName);
                }
            }
        }

        [ServerRpc]
        private void SetBlockingServerRpc(bool blocking)
        {
            isBlocking = blocking;
        }

        #endregion

        #region Damage Application (Server)

        public void ApplyIncomingDamage(float amount)
        {
            if (!IsServer) return;

            if (!isBlocking) return;
            if (IsBroken.Value) return;

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

        #endregion

        #region Abilities / Sigil

        public void RequestUseAbility(Vector2 inputDirection)
        {
            if (!IsOwner) return;
            if (!CanUseAbility()) return;

            ConsumeAbilityCooldownLocal();
            UseAbilityServerRpc();
        }

        private float GetEffectiveAbilityCooldown()
        {
            var stats = GetCurrentStats();

            if(stats.cooldown >0f) return stats.cooldown;
            if(baseAbility!=null && baseAbility.cooldown > 0f) return baseAbility.cooldown;

            return 0f;
        }

        public bool CanUseAbility()
        {
            if (!IsOwner) return false;
            if (baseAbility == null) return false;
            if (!enforceAbilityCooldown) return true;

            float cd = GetEffectiveAbilityCooldown();
            if(cd <= 0f) return false;

            return (Time.time - lastAbilityUseTimeLocal) >= cd;
        }

        public float GetCooldownRemaining()
        {
            if (!IsOwner) return 0f;
            if (!enforceAbilityCooldown) return 0f;

            float cd = GetEffectiveAbilityCooldown();
            if (cd <= 0f) return 0f;

            float elapsed = Time.time - lastAbilityUseTimeLocal;
            return Mathf.Max(0f, cd -  elapsed);
        }

        private void ConsumeAbilityCooldownLocal()
        {
            lastAbilityUseTimeLocal = Time.time;
        }

        [ServerRpc]
        private void UseAbilityServerRpc()
        {
            // Implement shield bash or other shield ability here
        }

        #endregion
    }
}
