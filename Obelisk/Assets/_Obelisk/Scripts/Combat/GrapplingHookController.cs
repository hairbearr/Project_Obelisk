using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Combat
{
    public class GrapplingHookController : NetworkBehaviour, IWeaponController
    {
        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;

        [Header("Grapple Settings")]
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask grappleLayers;

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer grappleRenderer;
        [SerializeField] private Transform vfxSpawnPoint;

        private GameObject grappleVfxPrefab;

        public NetworkVariable<bool> IsGrappling = new NetworkVariable<bool>();

        private Vector3 serverTargetPoint;
        private float lastGrappleTime;

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;
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

        private float GetEffectiveCooldown(EffectiveAbilityStats stats)
        {
            if (stats.cooldown <= 0f && baseAbility != null)
                return baseAbility.cooldown;

            return stats.cooldown;
        }

        private float GetEffectivePullSpeed(EffectiveAbilityStats stats)
        {
            float baseForce = stats.grappleForce > 0f
                ? stats.grappleForce
                : (baseAbility != null ? baseAbility.grappleForce : 15f);

            return baseForce;
        }

        private float GetEffectiveDamage(EffectiveAbilityStats stats)
        {
            if (stats.damage > 0f)
                return stats.damage;

            return baseAbility != null ? baseAbility.damage : 0f;
        }

        // ---------------------------------------------------------
        // Public API for PlayerController
        // ---------------------------------------------------------

        public void RequestFireGrapple(Vector2 inputDirection)
        {
            if (!IsOwner) return;
            if (IsGrappling.Value) return;
            if (baseAbility == null) return;

            var stats = GetCurrentStats();
            float cooldown = GetEffectiveCooldown(stats);

            if (Time.time - lastGrappleTime < cooldown)
                return;

            lastGrappleTime = Time.time;

            Vector3 worldDir = new Vector3(inputDirection.x, 0f, inputDirection.y);
            if (worldDir.sqrMagnitude < 0.01f)
                worldDir = transform.forward;

            FireGrappleServerRpc(worldDir.normalized);
        }

        [ServerRpc]
        private void FireGrappleServerRpc(Vector3 worldDirection)
        {
            if (!Physics.Raycast(transform.position, worldDirection, out RaycastHit hit, maxDistance, grappleLayers))
                return;

            IsGrappling.Value = true;
            serverTargetPoint = hit.point;

            var stats = GetCurrentStats();
            float damage = GetEffectiveDamage(stats);

            IDamageable dmg = hit.collider.GetComponent<IDamageable>();
            if (dmg != null && damage > 0f)
            {
                dmg.TakeDamage(damage);
            }

            // Future implementation:
            // If hit is an enemy with a "pull to player" tag, pull enemy to player instead.
        }

        private void Update()
        {
            if (!IsServer) return;
            if (!IsGrappling.Value) return;

            var stats = GetCurrentStats();
            float pullSpeed = GetEffectivePullSpeed(stats);

            Vector3 currentPos = transform.position;
            Vector3 toTarget = serverTargetPoint - currentPos;
            float distanceThisFrame = pullSpeed * Time.deltaTime;

            if (toTarget.magnitude <= distanceThisFrame)
            {
                transform.position = serverTargetPoint;
                IsGrappling.Value = false;
            }
            else
            {
                transform.position += toTarget.normalized * distanceThisFrame;
            }
        }

        public bool IsCurrentlyGrapplingLocal
        {
            get { return IsGrappling.Value; }
        }

        public void RequestUseAbility(Vector2 inputDirection)
        {
            RequestFireGrapple(inputDirection);
        }

        public void ApplyVisualSet(WeaponVisualSet set)
        {
            if (set == null)
                return;

            if (weaponAnimator != null && set.overrideController != null)
                weaponAnimator.runtimeAnimatorController = set.overrideController;

            if (grappleRenderer != null && set.idleSprite != null)
                grappleRenderer.sprite = set.idleSprite;

            grappleVfxPrefab = set.attackVfx;
        }

        public void SetEquippedSigil(SigilDefinition sigil)
        {
            equippedSigil = sigil;

            if (sigil != null)
                equippedSigilId = sigil.id;
            else
                equippedSigilId = string.Empty;
        }

    }
}
