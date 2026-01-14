using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Combat
{
    public class SwordController : NetworkBehaviour, IWeaponController
    {
        #region Inspector - Ability / Sigil

        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;

        #endregion

        #region Inspector - Visual References

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer swordRenderer;
        [SerializeField] private Transform vfxSpawnPoint;

        #endregion

        #region Inspector - Hitbox Settings

        [Header("Hitbox Settings")]
        [SerializeField] private float hitRange = 1.5f;
        [SerializeField] private float hitRadius = 1.0f;
        [SerializeField] private LayerMask hitLayers;

        #endregion

        #region Runtime State

        [SerializeField] private bool enforceAbilityCooldown = true;
        private float lastAbilityUseTimeLocal = -9999f;
        private GameObject attackVfxPrefab;

        #endregion

        #region Unity Callbacks

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            // Gizmo uses Up as a simple preview direction.
            Vector2 dir = Vector2.up;
            Vector2 origin = (Vector2)transform.position + dir * (hitRange * 0.5f);

            Gizmos.DrawWireSphere(origin, hitRadius);
        }

        #endregion

        #region Visual Set / Sigil

        public void SetEquippedSigil(SigilDefinition sigil)
        {
            equippedSigil = sigil;
            equippedSigilId = sigil != null ? sigil.id : string.Empty;
        }

        public void ApplyVisualSet(WeaponVisualSet set)
        {
            if (set == null)
                return;

            if (weaponAnimator != null && set.overrideController != null)
                weaponAnimator.runtimeAnimatorController = set.overrideController;

            if (swordRenderer != null && set.idleSprite != null)
                swordRenderer.sprite = set.idleSprite;

            attackVfxPrefab = set.attackVfx;
        }

        #endregion

        #region Effective Stats

        private EffectiveAbilityStats GetCurrentStats()
        {
            if (baseAbility == null) return default;

            SigilProgressData progress = null;

            if (equippedSigil != null &&
                sigilInventory != null &&
                !string.IsNullOrEmpty(equippedSigilId))
            {
                progress = sigilInventory.GetOrCreateProgress(equippedSigilId);
            }

            return SigilEvaluator.GetEffectiveStats(baseAbility, equippedSigil, progress);
        }

        #endregion

        #region Public API - IWeaponController

        public void RequestUseAbility(Vector2 inputDirection)
        {
            if (!IsOwner)
                return;

            if (baseAbility == null)
                return;

            if (!CanUseAbility()) return;

            ConsumeAbilityCooldownLocal();

            Vector2 dir = inputDirection.sqrMagnitude > 0.01f
                ? inputDirection.normalized
                : Vector2.up;

            if (weaponAnimator != null)
            {
                Debug.Log("SwordController: weaponAnimator = " + (weaponAnimator != null ? weaponAnimator.name : "null"));
                weaponAnimator.SetTrigger("SwordSlash");
            }

            UseAbilityServerRpc(dir);
        }

        private float GetEffectiveAbilityCooldown()
        {
            var stats = GetCurrentStats();

            if (stats.cooldown > 0f) return stats.cooldown;
            if(baseAbility != null && baseAbility.cooldown > 0f) return baseAbility.cooldown;

            return 0f;
        }

        public bool CanUseAbility()
        {
            if (!IsOwner) return false;
            if (baseAbility == null) return false;
            if (!enforceAbilityCooldown) return true;

            float cd = GetEffectiveAbilityCooldown();
            if (cd <= 0f) return true;

            return (Time.time - lastAbilityUseTimeLocal) >= cd;
        }

        public float GetCooldownRemaining()
        {
            if (!enforceAbilityCooldown) return 0f;

            float cd = GetEffectiveAbilityCooldown();
            if(cd<=0f) return 0f;

            float elapsed = Time.time - lastAbilityUseTimeLocal;
            return Mathf.Max(0f, cd- elapsed);
        }

        private void ConsumeAbilityCooldownLocal()
        {
            lastAbilityUseTimeLocal = Time.time;
        }

        #endregion

        #region Networking - Server Ability Execution

        [ServerRpc]
        private void UseAbilityServerRpc(Vector2 direction)
        {
            var stats = GetCurrentStats();

            float damage = stats.damage;
            if (damage <= 0f && baseAbility != null)
                damage = baseAbility.damage;

            float knockback = stats.knockbackForce;
            if (knockback <= 0f && baseAbility != null)
                knockback = baseAbility.knockbackForce;

            Vector2 origin = (Vector2)transform.position + direction.normalized * (hitRange * 0.5f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, hitRadius, hitLayers);

            foreach (var hit in hits)
            {
                if (hit.transform == transform)
                    continue;

                var dmg = hit.GetComponent<IDamageable>();
                if (dmg != null && damage > 0f)
                {
                    dmg.TakeDamage(damage);
                }

                var kb = hit.GetComponent<IKnockbackable>();
                if (kb != null && knockback > 0f)
                {
                    Vector2 toTarget = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    kb.ApplyKnockback(toTarget, knockback);
                }
            }

            PlayAttackVfxClientRpc(direction);
        }

        [ClientRpc]
        private void PlayAttackVfxClientRpc(Vector2 direction)
        {
            if (attackVfxPrefab == null)
                return;

            Vector3 pos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            GameObject vfx = Object.Instantiate(attackVfxPrefab, pos, Quaternion.identity);
            Object.Destroy(vfx, 2f);
        }

        #endregion
    }
}

