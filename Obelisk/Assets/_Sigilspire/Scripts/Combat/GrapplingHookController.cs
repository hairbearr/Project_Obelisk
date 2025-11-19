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
        [SerializeField] private float minDistanceToStop = 0.2f;

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer grappleRenderer;
        [SerializeField] private Transform vfxSpawnPoint;
        [SerializeField] private LineRenderer lineRenderer;

        public NetworkVariable<bool> IsGrappling = new NetworkVariable<bool>();

        private GameObject grappleVfxPrefab;
        private Vector2 serverTargetPoint;
        private float lastGrappleTime;

        private void Awake()
        {
            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;

            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
                lineRenderer.enabled = false;
        }

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

            if (grappleRenderer != null && set.idleSprite != null)
                grappleRenderer.sprite = set.idleSprite;

            grappleVfxPrefab = set.attackVfx;
        }

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

        // -------- Public API for PlayerController --------

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

            Vector2 dir = inputDirection.sqrMagnitude > 0.01f
                ? inputDirection.normalized
                : Vector2.up;

            if (weaponAnimator != null)
                weaponAnimator.SetTrigger("GrappleCast");

            FireGrappleServerRpc(dir);
        }

        public void RequestUseAbility(Vector2 inputDirection)
        {
            RequestFireGrapple(inputDirection);
        }

        [ServerRpc]
        private void FireGrappleServerRpc(Vector2 direction)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, maxDistance, grappleLayers);
            if (!hit) return;

            IsGrappling.Value = true;
            serverTargetPoint = hit.point;

            var stats = GetCurrentStats();
            float damage = GetEffectiveDamage(stats);

            var dmg = hit.collider.GetComponent<IDamageable>();
            if (dmg != null && damage > 0f)
            {
                dmg.TakeDamage(damage);
            }

            StartGrappleClientRpc(serverTargetPoint);
        }

        [ClientRpc]
        private void StartGrappleClientRpc(Vector2 targetPoint)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, new Vector3(targetPoint.x, targetPoint.y, 0f));
            }

            if (grappleVfxPrefab != null && vfxSpawnPoint != null)
            {
                var vfx = Object.Instantiate(grappleVfxPrefab, vfxSpawnPoint.position, Quaternion.identity);
                Object.Destroy(vfx, 2f);
            }
        }

        [ClientRpc]
        private void StopGrappleClientRpc()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }

            if (weaponAnimator != null)
            {
                weaponAnimator.SetTrigger("GrappleRetract");
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            if (!IsGrappling.Value) return;

            var stats = GetCurrentStats();
            float pullSpeed = GetEffectivePullSpeed(stats);

            Vector2 currentPos = transform.position;
            Vector2 toTarget = serverTargetPoint - currentPos;
            float dist = toTarget.magnitude;
            float step = pullSpeed * Time.deltaTime;

            if (dist <= minDistanceToStop || dist <= step)
            {
                transform.position = new Vector3(serverTargetPoint.x, serverTargetPoint.y, 0f);
                IsGrappling.Value = false;
                StopGrappleClientRpc();
            }
            else
            {
                Vector2 newPos = currentPos + toTarget.normalized * step;
                transform.position = new Vector3(newPos.x, newPos.y, 0f);
            }
        }

        public bool IsCurrentlyGrapplingLocal => IsGrappling.Value;
    }
}
