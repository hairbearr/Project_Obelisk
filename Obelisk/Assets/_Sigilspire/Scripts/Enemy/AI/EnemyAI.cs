using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;
using Combat.Health;
using System.Collections.Generic;
using System;

namespace Enemy
{
    public class EnemyAI : NetworkBehaviour
    {
        [Header("Targeting")]
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float stoppingDistance = 1.2f;
        [SerializeField] private Collider2D enemyCollider;

        [Header("Attack")]
        [SerializeField] private Ability primaryAbility;
        [SerializeField] private float attackRange = 1.6f;

        [Header("Threat")]
        [SerializeField] private EnemyThreatTracker threatTracker;
        [SerializeField] private float retargetInterval = 0.25f;
        private float lastRetargetTime;

        private Transform currentTarget;
        private ulong currentTargetId;

        private Rigidbody2D rb2D;
        private EnemyAnimationDriver animDriver;
        private HealthBase health;

        private float lastAttackTime;

        private enum AttackMode { Melee, RangedHitscan }
        [Header("Attack Mode")]
        [SerializeField] private AttackMode attackMode = AttackMode.Melee;

        [Header("Ranged (Hitscan)")]
        [SerializeField] private float rangedMaxRange = 8f;
        [SerializeField] private LayerMask lineOfSightMask; // walls/obstacles
        [SerializeField] private float rangedStopDistance = 5f; // keep-away distance

        [Header("Ranged Kiting")]
        [SerializeField] private float kiteDeadZone = 0.75f; // how much slack around rangedStopDistance
        [SerializeField] private float kiteDecisionCooldown = 0.8f; // how often we re-evaluate kiting
        [SerializeField] private float kiteMaxDuration = 0.9f; // max time we keep backing up once we start
        [SerializeField] private float kiteSpeedMultiplier = 1.0f; // potentially slightly increases speed while kiting
        private float nextKiteDecisiontime;
        private float kiteEndTime;
        private bool isKiting;

        // Reuse buffers to avoid GC allocations
        private readonly List<ulong> candidates = new List<ulong>(16);

        private void Awake()
        {
            rb2D = GetComponent<Rigidbody2D>();
            animDriver = GetComponentInChildren<EnemyAnimationDriver>();
            health = GetComponent<HealthBase>();
            if (enemyCollider == null) { enemyCollider = GetComponent<Collider2D>(); }
        }

        private void Update()
        {
            if (Time.frameCount % 60 == 0)
                Debug.Log($"[EnemyAI] IsServer={IsServer} IsHost={NetworkManager.Singleton?.IsHost} spawned={IsSpawned}", this);

            if (Time.frameCount % 60 == 0 && IsServer && health != null)
                Debug.Log($"[EnemyAI] HP={health.CurrentHealth.Value} max={health.MaxHealth}", this);


            if (!IsServer) return;

            if (health != null && health.Initialized && health.CurrentHealth.Value <= 0f)
            {
                if (animDriver != null) animDriver.SetMovement(Vector2.zero);
                return;
            }

            // Retarget periodically
            if (Time.time - lastRetargetTime >= retargetInterval)
            {
                lastRetargetTime = Time.time;
                FindTarget();
            }

            // If our transform target became invalid (despawn), try to resolve again
            if (currentTarget == null && currentTargetId != 0)
            {
                currentTarget = ResolveTargetTransform(currentTargetId);
                if (currentTarget == null)
                {
                    currentTargetId = 0;
                    if (threatTracker != null) threatTracker.SetCurrentTargetId(0);
                }
            }

            if (currentTarget != null)
            {
                HandleMovement();
                TryAttack();
            }
            else
            {
                if (animDriver != null) animDriver.SetMovement(Vector2.zero);
            }
        }

        private void FindTarget()
        {


            if (threatTracker == null || NetworkManager == null)
            {
                currentTarget = null;
                currentTargetId = 0;
                return;
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayers);
            Debug.Log($"Enemy sees {hits.Length} targets in radius.");

            if (hits == null || hits.Length == 0)
            {
                currentTarget = null;
                currentTargetId = 0;
                threatTracker.SetCurrentTargetId(0);
                return;
            }

            // Build unique candidate list (NetworkObjectIds) in range
            candidates.Clear();
            for (int i = 0; i < hits.Length; i++)
            {
                var no = hits[i].GetComponentInParent<NetworkObject>();
                if (no == null) continue;

                ulong id = no.NetworkObjectId;

                // Prevent duplicates if target has multiple colliders
                if (!candidates.Contains(id))
                    candidates.Add(id);
            }

            if (candidates.Count == 0)
            {
                currentTarget = null;
                currentTargetId = 0;
                threatTracker.SetCurrentTargetId(0);
                return;
            }


            threatTracker.PruneInvalidThreat();

            // if our current target isnt in range anymore, don't bias towards it
            ulong existing = candidates.Contains(currentTargetId) ? currentTargetId : 0;

            // Use the tracker’s hysteresis rule (aggroThreshold) to pick/keep target
            ulong bestId = threatTracker.PickBestTargetId(currentTargetId, candidates);


            // fallback behavior : if nobody has threat yet, aggro the closest target in range
            if (bestId == 0 || threatTracker.GetThreat(bestId) <= 0f)
            {
                bestId = PickClosestCandidateId(candidates);
            }
            

            currentTargetId = bestId;
            threatTracker.SetCurrentTargetId(bestId);

            currentTarget = ResolveTargetTransform(bestId);

            Debug.Log($"[EnemyAI] hits={hits.Length} cand={candidates.Count}", this);

        }

        private ulong PickClosestCandidateId(List<ulong> ids)
        {
            if (ids == null || ids.Count == 0) return 0;
            if (NetworkManager == null) return 0;

            var sm = NetworkManager.SpawnManager;
            if (sm == null) return 0;

            ulong bestId = 0;
            float bestDistSq = float.MaxValue;

            Vector2 self = rb2D != null ? rb2D.position : (Vector2)transform.position;

            for (int i = 0; i < ids.Count; i++)
            {
                ulong id = ids[i];
                if (!sm.SpawnedObjects.TryGetValue(id, out NetworkObject obj)) continue;

                Vector2 p = obj.transform.position;
                float dSq = (p - self).sqrMagnitude;

                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    bestId = id;
                }
            }

            return bestId;
        }


        private Transform ResolveTargetTransform(ulong id)
        {
            if (id == 0) return null;
            if (NetworkManager == null) return null;

            var sm = NetworkManager.SpawnManager;
            if (sm == null) return null;

            if (!sm.SpawnedObjects.TryGetValue(id, out NetworkObject obj)) return null;
            return obj.transform;
        }

        private void HandleMovement()
        {
            if (currentTarget == null || rb2D == null) return;

            Vector2 selfPos = rb2D.position;
            Vector2 targetPos = currentTarget.position;
            Vector2 toTarget = targetPos - selfPos;

            // Grab colliders (player/root collider per your setup)
            Collider2D targetCol = currentTarget.GetComponentInParent<Collider2D>();

            // Compute "surface distance" if possible, otherwise fallback to center distance
            bool hasColliderDistance = (enemyCollider != null && targetCol != null);
            ColliderDistance2D cd = default;
            float surfaceDistance = toTarget.magnitude;

            if (hasColliderDistance)
            {
                cd = enemyCollider.Distance(targetCol);
                // If overlapped, treat distance as 0 for decision making
                surfaceDistance = cd.isOverlapped ? 0f : cd.distance;
            }

            // --- Helper: move + animate ---
            void Move(Vector2 dir, float speedMult = 1f)
            {
                if (dir.sqrMagnitude < 0.0001f)
                {
                    animDriver?.SetMovement(Vector2.zero);
                    return;
                }

                float spd = moveSpeed * speedMult;
                rb2D.MovePosition(selfPos + dir.normalized * (spd * Time.deltaTime));
                animDriver?.SetMovement(dir.normalized);
            }

            // --- Overlap escape (prevents "trying to run through") ---
            // If we are overlapping the target, back off a bit to separate.
            // This makes melee stop cleanly and prevents constant pushing.
            if (hasColliderDistance && cd.isOverlapped)
            {
                // If separation is available, use it. Otherwise just move away from target center.
                Vector2 away =
                    cd.normal.sqrMagnitude > 0.0001f
                        ? cd.normal
                        : (-toTarget).sqrMagnitude > 0.0001f ? (-toTarget).normalized : Vector2.up;

                Move(away, 1.0f);
                return;
            }

            // -------------------------------
            // Melee - Chase until "stop distance" from collider surface
            // -------------------------------
            if (attackMode == AttackMode.Melee)
            {
                if (surfaceDistance <= stoppingDistance)
                {
                    animDriver?.SetMovement(Vector2.zero);
                    return;
                }

                // Move toward target
                Move(toTarget);
                return;
            }

            // -------------------------------
            // Ranged - keep distance + kite
            // Decisions based on SURFACE distance (better spacing)
            // -------------------------------
            float tooCloseDist = rangedStopDistance - kiteDeadZone;
            float safeDist = rangedStopDistance + kiteDeadZone;

            // Decide if we should start/stop kiting (throttled)
            if (Time.time >= nextKiteDecisiontime)
            {
                nextKiteDecisiontime = Time.time + kiteDecisionCooldown;

                if (!isKiting && surfaceDistance < tooCloseDist)
                {
                    isKiting = true;
                    kiteEndTime = Time.time + kiteMaxDuration;
                }

                if (isKiting && surfaceDistance > safeDist)
                {
                    isKiting = false;
                }
            }

            // Hard stop: don't kite forever
            if (isKiting && Time.time >= kiteEndTime)
            {
                isKiting = false;
            }

            if (isKiting)
            {
                // Move away to regain spacing
                Vector2 away = (-toTarget);
                Move(away, kiteSpeedMultiplier);
                return;
            }

            // Not kiting: hold position if within desired range, otherwise close distance
            if (surfaceDistance <= rangedStopDistance)
            {
                animDriver?.SetMovement(Vector2.zero);
                return;
            }

            Move(toTarget);
        }


        private void TryAttack()
        {
            if (primaryAbility == null || currentTarget == null) return;
            if (Time.time - lastAttackTime < primaryAbility.cooldown) return;

            Vector2 toTarget = (Vector2)currentTarget.position - rb2D.position;
            float distance = toTarget.magnitude;

            if(attackMode == AttackMode.Melee)
            {
                Collider2D targetCol = currentTarget.GetComponentInParent<Collider2D>();

                if (enemyCollider != null && targetCol != null)
                {
                    ColliderDistance2D d = enemyCollider.Distance(targetCol);
                    float surfDist = d.isOverlapped ? 0f : d.distance;

                    if (surfDist > attackRange) return;
                }
                else
                {
                    // fallback if colliders missing
                    if (distance > attackRange) return;
                }

                lastAttackTime = Time.time;
                PerformAbilityAttack(currentTarget, primaryAbility);
                return;
            }

            // ranged hitscan
            if (distance > rangedMaxRange) return;

            if (distance < rangedStopDistance - kiteDeadZone)
                return;

            // LOS check (raycast to target, stop if blocked
            Vector2 origin = rb2D.position;
            Vector2 dir = ((Vector2)currentTarget.position - origin).normalized;
            float rayDist = Vector2.Distance(origin, currentTarget.position);

            RaycastHit2D los = Physics2D.Raycast(origin, dir, rayDist, lineOfSightMask);
            if (los.collider != null) return; // blocked by wall.

            lastAttackTime = Time.time;
            PerformHitscanAttack(currentTarget, primaryAbility);
        }

        private void PerformHitscanAttack(Transform target, Ability ability)
        {
            Vector2 attackDir = ((Vector2)target.position - rb2D.position).normalized;

            animDriver?.PlayAttack(attackDir);

            // damage the player
            IDamageable dmg = target.GetComponentInParent<IDamageable>();
            if(dmg != null && ability.damage > 0f) dmg.TakeDamage(ability.damage, NetworkObjectId);

            if (ability.vfxPrefab != null)
            {
                GameObject vfx = Instantiate(ability.vfxPrefab, target.position, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        }

        private void PerformAbilityAttack(Transform target, Ability ability)
        {
            Vector2 attackDir = ((Vector2)target.position - rb2D.position).normalized;

            if (animDriver != null)
            {
                animDriver.PlayAttack(attackDir);
            }

            IDamageable dmg = target.GetComponentInParent<IDamageable>();
            if (dmg != null && ability.damage > 0f)
            {
                dmg.TakeDamage(ability.damage, NetworkObjectId);
            }

            if (ability.vfxPrefab != null)
            {
                GameObject vfx = GameObject.Instantiate(ability.vfxPrefab, target.position, Quaternion.identity);
                GameObject.Destroy(vfx, 2f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackMode == AttackMode.Melee ? attackRange : rangedMaxRange);

            if (attackMode == AttackMode.RangedHitscan)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, rangedStopDistance);
            }
        }
    }
}

