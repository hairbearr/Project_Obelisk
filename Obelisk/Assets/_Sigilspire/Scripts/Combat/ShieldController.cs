using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Combat
{
    public class ShieldController : NetworkBehaviour, IWeaponController, IBlockProvider
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

        [Header("Player Reference")]
        [SerializeField] private Player.PlayerController playerController;

        #endregion

        #region Inspector - Shield Settings

        [Header("Shield Settings")]
        [SerializeField] private float baseMaxShieldEnergy = 100f;
        [SerializeField] private float baseRegenDelay = 2f;
        [SerializeField] private float baseRegenRate = 15f;

        [Header("Block Rules")]
        [SerializeField, Range(30f, 180f)] private float blockArcDegrees = 120f;
        [SerializeField] private float damageToEnergyRatio = 1f;   // 1 damage drains 1 energy
        [SerializeField] private float minEnergyToBlock = 0.5f;    // prevents “blocking” at 0

        // For gizmos + optional extra rule: how far away an attacker can be and still be blocked.
        // If you only care about arc (not distance), set this to something large like 10+.
        [SerializeField] private float blockRadius = 1.25f;

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

        public NetworkVariable<bool> IsBlockingNet = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #endregion

        #region Runtime State

        private GameObject blockVfxPrefab;
        private float lastHitTime;

        private bool localIsBlocking;  // owner-only input state for freezing anim

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            // NEW: Auto-find player controller
            if (playerController == null)
                playerController = GetComponentInParent<Player.PlayerController>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                ShieldEnergy.Value = GetEffectiveMaxShieldEnergy();
                IsBroken.Value = false;
                IsBlockingNet.Value = false;
            }
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

        public float GetEffectiveMaxShieldEnergy()
        {
            float modifier = baseAbility != null ? baseAbility.shieldEnergyModifier : 0f;
            return baseMaxShieldEnergy * (1f + modifier);
        }

        private float GetEffectiveRegenDelay() => baseRegenDelay;
        private float GetEffectiveRegenRate() => baseRegenRate;

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
            IsBlockingNet.Value = blocking;
        }

        #endregion

        #region IBlockProvider

        public bool TryBlock(Vector2 attackerWorldPos, float incomingDamage, out float damageAfterBlock)
        {
            damageAfterBlock = incomingDamage;

            if (!IsServer) return false;
            if (!IsBlockingNet.Value) return false;
            if (IsBroken.Value) return false;
            if (incomingDamage <= 0f) return false;
            if (ShieldEnergy.Value < minEnergyToBlock) return false;

            Vector2 forward = GetFacingForBlock();
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector2.up;

            Vector2 toAttacker = attackerWorldPos - (Vector2)transform.position;

            // DEBUG

            if (toAttacker.sqrMagnitude < 0.0001f) return false;


            if (blockRadius > 0f && toAttacker.sqrMagnitude > blockRadius * blockRadius)
                return false;

            float angle = Vector2.Angle(forward, toAttacker.normalized);


            if (angle > blockArcDegrees * 0.5f)
            {
                return false; 
            }

            // Spend energy
            lastHitTime = Time.time;

            float energyCost = incomingDamage * damageToEnergyRatio;
            float spend = Mathf.Min(ShieldEnergy.Value, energyCost);
            ShieldEnergy.Value -= spend;

            if (ShieldEnergy.Value <= 0f)
            {
                ShieldEnergy.Value = 0f;
                BreakShield();
            }

            // Block proportional to energy spent
            float blockedDamage = (energyCost <= 0.0001f) ? incomingDamage : incomingDamage * (spend / energyCost);
            damageAfterBlock = Mathf.Max(0f, incomingDamage - blockedDamage);


            return blockedDamage > 0f;
        }

        private Vector2 GetFacingForBlock()
        {
            // BEST: Use the player's last facing direction (most reliable)
            if (playerController != null)
            {
                Vector2 f = playerController.LastFacingDir;
                if (f.sqrMagnitude > 0.0001f)
                    return f.normalized;
            }

            // Fallback: animator floats (if player controller missing somehow)
            if (weaponAnimator != null)
            {
                float x = weaponAnimator.GetFloat("MoveX");
                float y = weaponAnimator.GetFloat("MoveY");
                Vector2 f = new Vector2(x, y);
                if (f.sqrMagnitude > 0.0001f)
                    return f.normalized;
            }

            // Last resort
            return Vector2.up;
        }

        private void BreakShield()
        {
            IsBroken.Value = true;
            IsBlockingNet.Value = false; // auto-drop block when broken (optional)
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

            if (stats.cooldown > 0f) return stats.cooldown;
            if (baseAbility != null && baseAbility.cooldown > 0f) return baseAbility.cooldown;

            return 0f;
        }

        public bool CanUseAbility()
        {
            if (!IsOwner) return false;
            if (baseAbility == null) return false;
            if (!enforceAbilityCooldown) return true;

            float cd = GetEffectiveAbilityCooldown();
            if (cd <= 0f) return false;

            return (Time.time - lastAbilityUseTimeLocal) >= cd;
        }

        public float GetCooldownRemaining()
        {
            if (!IsOwner) return 0f;
            if (!enforceAbilityCooldown) return 0f;

            float cd = GetEffectiveAbilityCooldown();
            if (cd <= 0f) return 0f;

            float elapsed = Time.time - lastAbilityUseTimeLocal;
            return Mathf.Max(0f, cd - elapsed);
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

        #region Gizmos (Editor)

        private void OnDrawGizmosSelected()
        {
            Vector2 origin = transform.position;

            // Use player controller's facing if available in play mode
            Vector2 forward = Vector2.up;
            if (Application.isPlaying && playerController != null)
            {
                forward = playerController.LastFacingDir;
            }
            else
            {
                forward = GetFacingForBlock();
            }

            // ---- Draw block radius ----
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f); // blue-ish
            if (blockRadius > 0f)
                Gizmos.DrawWireSphere(origin, blockRadius);

            // ---- Draw block arc ----
            float halfArc = blockArcDegrees * 0.5f;
            int steps = 24;

            Vector3 prevPoint = origin + Rotate(forward, -halfArc) * blockRadius;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = Mathf.Lerp(-halfArc, halfArc, t);
                Vector3 nextPoint = origin + Rotate(forward, angle) * blockRadius;

                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // ---- Draw forward line ----
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + forward * blockRadius);
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);

            return new Vector2(
                cos * v.x - sin * v.y,
                sin * v.x + cos * v.y
            );
        }

        #endregion
    }
}
