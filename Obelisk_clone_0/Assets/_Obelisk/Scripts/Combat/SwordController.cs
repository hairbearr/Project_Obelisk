using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Combat
{
    public class SwordController : NetworkBehaviour, IWeaponController
    {
        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer swordRenderer;
        [SerializeField] private Transform vfxSpawnPoint;

        private GameObject attackVfxPrefab;

        private float lastAttackTime;

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;
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

        private EffectiveAbilityStats GetCurrentStats()
        {
            SigilProgressData progress = null;

            if (equippedSigil != null &&
                sigilInventory != null &&
                !string.IsNullOrEmpty(equippedSigilId))
            {
                progress = sigilInventory.GetOrCreateProgress(equippedSigilId);
            }

            return SigilEvaluator.GetEffectiveStats(baseAbility, equippedSigil, progress);
        }

        public void SetEquippedSigil(SigilDefinition sigil)
        {
            equippedSigil = sigil;

            if (sigil != null)
                equippedSigilId = sigil.id;
            else
                equippedSigilId = string.Empty;
        }


        public void RequestUseAbility(Vector2 inputDirection)
        {
            if (!IsOwner)
                return;

            var stats = GetCurrentStats();
            float cooldown = stats.cooldown > 0f ? stats.cooldown : baseAbility.cooldown;

            if (Time.time - lastAttackTime < cooldown)
                return;

            lastAttackTime = Time.time;

            Vector3 worldDir = new Vector3(inputDirection.x, 0f, inputDirection.y);
            if (worldDir.sqrMagnitude < 0.01f)
                worldDir = transform.forward;

            weaponAnimator.SetTrigger("SwordSlash");

            UseAbilityServerRpc(worldDir.normalized);
        }

        [ServerRpc]
        private void UseAbilityServerRpc(Vector3 worldDirection)
        {
            var stats = GetCurrentStats();

            // Damage logic omitted for clarity
            PlayAttackVfxClientRpc(worldDirection);
        }

        [ClientRpc]
        private void PlayAttackVfxClientRpc(Vector3 worldDirection)
        {
            if (attackVfxPrefab == null)
                return;

            Vector3 pos = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;

            GameObject vfx = Object.Instantiate(attackVfxPrefab, pos, Quaternion.identity);
            Object.Destroy(vfx, 2f);
        }
    }
}
