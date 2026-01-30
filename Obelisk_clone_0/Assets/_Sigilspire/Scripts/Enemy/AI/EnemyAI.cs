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
        [SerializeField] private float knockbackMoveLockTime = 0.12f;
        private float moveLockedUntil;

        [Header("Attack")]
        [SerializeField] private Ability primaryAbility;
        [SerializeField] private float attackRange = 1.6f;
        [SerializeField] private float attackLockTime = 0.2f;
        private float attackLockUntil;
        [SerializeField] private float facingEpsilon = 0.001f;
        private Vector2 lastFacingDir = Vector2.down;

        [Header("Melee Spacing")]
        [SerializeField] private float meleeMinDistance = 0.8f; // Don't get closer than this
        [SerializeField] private float meleeIdealDistance = 1.2f; // Try to stay at this range

        [Tooltip("When during the attack animation should damage occur (0 = start, 1 = end)")]
        [SerializeField, Range(0f, 1f)] private float attackHitTiming = 0.5f;

        public float AttackHitTiming => attackHitTiming;

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

            if(animDriver != null)
            {
                animDriver.onAttackHitFrame += OnEnemyAttackHitFrame;
            }
        }

        private void Update()
        {



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

            bool attackLocked = Time.time < attackLockUntil;

            if (currentTarget != null)
            {
                if (!attackLocked)
                {
                    HandleMovement();
                    TryAttack();
                }
                else
                {
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero); 
                }

                    
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


            // if nobody has threat yet, aggro the closest target in range
            if (bestId == 0 || threatTracker.GetThreat(bestId) <= 0f)
            {
                bestId = PickClosestCandidateId(candidates);
            }
            

            currentTargetId = bestId;
            threatTracker.SetCurrentTargetId(bestId);

            currentTarget = ResolveTargetTransform(bestId);

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

            if (Time.time < moveLockedUntil)
            {
                animDriver?.SetFacing(lastFacingDir);
                animDriver?.SetMovement(Vector2.zero);
                return;
            }

            Vector2 selfPos = rb2D.position;
            Vector2 targetPos = currentTarget.position;
            Vector2 toTarget = targetPos - selfPos;

            Collider2D targetCol = currentTarget.GetComponentInParent<Collider2D>();

            bool hasColliderDistance = (enemyCollider != null && targetCol != null);
            ColliderDistance2D cd = default;
            float surfaceDistance = toTarget.magnitude;

            if (hasColliderDistance)
            {
                cd = enemyCollider.Distance(targetCol);
                surfaceDistance = cd.isOverlapped ? 0f : cd.distance;
            }

            void Move(Vector2 dir, float speedMult = 1f)
            {
                if (dir.sqrMagnitude < 0.0001f)
                {
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero);
                    return;
                }

                SetFacing(dir);

                float spd = moveSpeed * speedMult;
                rb2D.MovePosition(selfPos + dir.normalized * (spd * Time.deltaTime));
                animDriver?.SetMovement(dir.normalized);
            }

            // --- Overlap handling (ONLY ONCE) ---
            if (hasColliderDistance && cd.isOverlapped)
            {
                if (attackMode == AttackMode.Melee)
                {
                    // Melee: don't bounce away, just stop and let TryAttack handle it.
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero);
                    return;
                }
                else
                {
                    // Ranged: escape overlap so they don't stand inside you.
                    Vector2 away =
                        cd.normal.sqrMagnitude > 0.0001f
                            ? cd.normal
                            : (-toTarget).sqrMagnitude > 0.0001f ? (-toTarget).normalized : Vector2.up;

                    Move(away, 1.0f);
                    return;
                }
            }

            // Melee - Chase until "stop distance" from collider surface
            if (attackMode == AttackMode.Melee)
            {
                // Too close? Back away
                if (surfaceDistance < meleeMinDistance)
                {
                    // Move away from target
                    Vector2 awayFromTarget = -toTarget.normalized;
                    Move(awayFromTarget, .5f); // Move Slower when backing up
                    return;
                }

                // At ideal distance? Stop and attack
                if(surfaceDistance <= meleeIdealDistance)
                {
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero);
                    return;
                }

                // Too far? Chase
                Move(toTarget);

            }

            // ----- Ranged -----
            float tooCloseDist = rangedStopDistance - kiteDeadZone;
            float safeDist = rangedStopDistance + kiteDeadZone;

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

            if (isKiting && Time.time >= kiteEndTime)
                isKiting = false;

            if (isKiting)
            {
                Move(-toTarget, kiteSpeedMultiplier);
                return;
            }

            if (surfaceDistance <= rangedStopDistance)
            {
                animDriver?.SetFacing(lastFacingDir);
                animDriver?.SetMovement(Vector2.zero);
                return;
            }

            Move(toTarget);
        }

        public void NotifyKnockback()
        {
            moveLockedUntil = Time.time + knockbackMoveLockTime;
        }

        private void SetFacing(Vector2 dir)
        {
            if (dir.sqrMagnitude <= facingEpsilon) return;
            lastFacingDir = dir.normalized;
        }


        private void TryAttack()
        {
            if (Time.time < attackLockUntil) return;

            if (primaryAbility == null || currentTarget == null) return;

            float cd = Mathf.Max(0.15f, primaryAbility.cooldown);
            if (Time.time - lastAttackTime < cd) return;

            
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
                attackLockUntil = Time.time + attackLockTime;

                Vector2 attackDir = ((Vector2)currentTarget.position - rb2D.position);
                if (attackDir.sqrMagnitude < 0.0001f) attackDir = lastFacingDir;
                SetFacing(attackDir);
                animDriver?.SetFacing(lastFacingDir);

                // Just play the animation - damage happens on animation event
                animDriver?.PlayAttack(lastFacingDir);
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
            SetFacing(dir);
            animDriver?.SetFacing(lastFacingDir);

            // Just play the animation - damage happens on animation event
            animDriver?.PlayAttack(lastFacingDir);
        }

        private void OnEnemyAttackHitFrame()
        {
            if (!IsServer) return;
            if (currentTarget == null) return;
            if (primaryAbility == null) return;

            Debug.Log($"[Enemy] {name} OnEnemyAttackHitFrame called! damage={primaryAbility.damage}");

            // Execute attack immediately when animation hits the "release" frame
            if (attackMode == AttackMode.Melee)
            {
                // Melee Attack
                IDamageable dmg = currentTarget.GetComponentInParent<IDamageable>();
                if (dmg != null && primaryAbility.damage > 0f)
                {
                    dmg.TakeDamage(primaryAbility.damage, NetworkObjectId);
                }

                if (primaryAbility.vfxPrefab != null) 
                {
                    GameObject vfx = Instantiate(primaryAbility.vfxPrefab, currentTarget.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }
            }
            else if(attackMode == AttackMode.RangedHitscan)
            {
                // Ranged hitscan attack
                Vector2 toTarget = (Vector2)currentTarget.position - rb2D.position;
                float distance = toTarget.magnitude;

                if (distance > rangedMaxRange) return;

                // LOS check
                Vector2 origin = rb2D.position;
                Vector2 dir = toTarget.normalized;
                float rayDist = distance;

                RaycastHit2D los = Physics2D.Raycast(origin, dir, rayDist, lineOfSightMask);
                if (los.collider != null) return; // Blocked by wall

                // Deal Damage
                IDamageable dmg = currentTarget.GetComponentInParent<IDamageable>();
                if(dmg != null && primaryAbility.damage > 0f)
                {
                    Debug.Log($"[Enemy] About to deal {primaryAbility.damage} damage");
                    dmg.TakeDamage(primaryAbility.damage, NetworkObjectId);
                    Debug.Log($"[Enemy] Damage dealt!");
                }

                if (primaryAbility.vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(primaryAbility.vfxPrefab, currentTarget.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }
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

