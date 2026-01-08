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

        private NetworkObject serverPulledEnemy;
        private Rigidbody2D serverPulledEnemyRb;
        private bool serverPullEnemyToPlayer;
        private Vector2 localTargetPoint;

        [SerializeField] private Rigidbody2D playerRb;
        [SerializeField] private Player.PlayerController playerController;

        private void Awake()
        {
            if (playerRb == null) playerRb = GetComponentInParent<Rigidbody2D>();

            if(playerController == null) playerController = GetComponentInParent<Player.PlayerController>();

            if (sigilInventory == null)
                sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;

            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null)
                lineRenderer.enabled = false;

            Debug.Log("LineRenderer found: " + (lineRenderer != null));
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

            if (playerController != null) playerController.SetMovementLocked(true);

            if(lineRenderer == null) { lineRenderer = GetComponent<LineRenderer>(); }

            Debug.Log("Grapple pressed, showing line");
            ShowLocalCastLine(dir);

            FireGrappleServerRpc(dir);
        }

        private void ShowLocalCastLine(Vector2 dir)
        {
            if (lineRenderer == null)
            {
                Debug.Log("ShowLocalCastLine: lineRenderer is null");
                return; 
            }


            Debug.Log("LR instance id: " + lineRenderer.GetInstanceID());

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;

            Debug.Log("ShowLocalCastLine: enabled=" + lineRenderer.enabled + " count=" + lineRenderer.positionCount);

            Vector3 start = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            Vector3 end = start + new Vector3(dir.x, dir.y, 0f) * maxDistance;

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        public void RequestUseAbility(Vector2 inputDirection)
        {
            RequestFireGrapple(inputDirection);
        }

        [ServerRpc]
        private void FireGrappleServerRpc(Vector2 direction)
        {
            Vector2 origin = vfxSpawnPoint != null ? (Vector2)vfxSpawnPoint.position : (Vector2)transform.position;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxDistance, grappleLayers);
            if (!hit)
            {
                GrappleMissClientRpc();
                return;
            }

            IsGrappling.Value = true;
            serverTargetPoint = hit.point;


            serverPulledEnemy = null;
            serverPulledEnemyRb = null;
            serverPullEnemyToPlayer = false;

            // if we hit an enemy that can be pulled, pull it to the player.
            // otherwise, pull the player to the hit point.

            var pullable = hit.collider.GetComponentInParent<Combat.DamageInterfaces.IGrapplePullable>();
            if(pullable != null && pullable.ShouldPullToPlayer())
            {
                serverPullEnemyToPlayer = true;
                serverPulledEnemyRb = hit.collider.GetComponentInParent<Rigidbody2D>();

                var no = hit.collider.GetComponentInParent<NetworkObject>();
                if (no != null) serverPulledEnemy = no;
            }

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
        private void GrappleMissClientRpc()
        {
            // Hide line
            if (lineRenderer  != null) { lineRenderer.enabled = false; }

            // play retract animation
            if (weaponAnimator != null) { weaponAnimator.SetTrigger("GrappleRetract"); }

            // restore movement for the owner
            var pc = GetComponentInParent<Player.PlayerController>();
            if (IsOwner && pc != null)
            {
                pc.SetMovementLocked(false);
            }
        }

        [ClientRpc]
        private void StartGrappleClientRpc(Vector2 targetPoint)
        {
            localTargetPoint = targetPoint;

            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                Vector3 start = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
                lineRenderer.SetPosition(0, start);
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

            if(IsOwner && playerController != null)
            {
                playerController.SetMovementLocked(false);
            }

        }

        private void Update()
        {
            if (!IsServer) return;
            if (!IsGrappling.Value) return;

            var stats = GetCurrentStats();
            float pullSpeed = GetEffectivePullSpeed(stats);

            if (serverPullEnemyToPlayer && serverPulledEnemyRb != null && playerRb != null)
            {
                Vector2 enemyPos = serverPulledEnemyRb.position;
                Vector2 playerPos = playerRb.position;

                Vector2 toPlayer = playerPos - enemyPos;
                float dist = toPlayer.magnitude;
                float step = pullSpeed * Time.deltaTime;

                if(dist <= minDistanceToStop || dist <= step)
                {
                    serverPulledEnemyRb.MovePosition(playerPos);
                    IsGrappling.Value = false;
                    StopGrappleClientRpc();
                }
                else
                {
                    Vector2 newPos = enemyPos + toPlayer.normalized * step;
                    serverPulledEnemyRb.MovePosition(newPos);
                }

                return;
            }

            // Default: pull the player to the target point (enemy or environment point)
            if (playerRb == null) return;

            Vector2 currentPos = playerRb.position;
            Vector2 toTarget = serverTargetPoint - currentPos;
            float dist2 = toTarget.magnitude;
            float step2 = pullSpeed * Time.deltaTime;

            if (dist2 <= minDistanceToStop || dist2 <= step2)
            {
                playerRb.MovePosition(serverTargetPoint);
                IsGrappling.Value = false;
                StopGrappleClientRpc();
            }
            else
            {
                Vector2 newPos = currentPos + toTarget.normalized * step2;
                playerRb.MovePosition(newPos);
            }
        }

        private void LateUpdate()
        {
            if (lineRenderer == null) return;

            if (!IsGrappling.Value) return;

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;

            Vector3 start = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            Vector3 end = new Vector3(localTargetPoint.x, localTargetPoint.y, 0f);

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        public bool IsCurrentlyGrapplingLocal => IsGrappling.Value;
    }
}
