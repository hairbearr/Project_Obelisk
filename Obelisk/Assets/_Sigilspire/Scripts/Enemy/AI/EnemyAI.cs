using Combat.AbilitySystem;
using Combat.DamageInterfaces;
using Combat.Health;
using Combat.Projectiles;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Enemy
{
    public class EnemyAI : NetworkBehaviour
    {
        [Header("Targeting")]
        [SerializeField] protected float detectionRadius = 8f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Leashing")]
        [SerializeField] private float leashDistance = 5f; // Distance from spawn before resetting
        [SerializeField] private float outOfCombatDelay = 5f; // Seconds out of combat before resetting
        [SerializeField] private bool canLeash = true; // Can this enemy reset?
        [SerializeField] protected ResetBehavior resetBehavior = ResetBehavior.RunBack;

        private Vector2 spawnPos;
        private float lastCombatTime;
        private bool isReturningToSpawn = false;
        private bool hasEnteredCombat = false;

        [Header("Movement")]
        [SerializeField] public float moveSpeed = 2.5f;
      //[SerializeField] private float stoppingDistance = 1.2f;
        [SerializeField] private Collider2D enemyCollider;
        [SerializeField] private float knockbackMoveLockTime = 0.12f;
        private float moveLockedUntil;

        [Header("Attack")]
        [SerializeField] public Ability primaryAbility;
        [SerializeField] protected float attackRange = 1.6f;
        [SerializeField] private float attackLockTime = 0.2f;
        private float attackLockUntil;
        [SerializeField] private float facingEpsilon = 0.001f;
        protected Vector2 lastFacingDir = Vector2.down;
        [SerializeField, Range(0f, 360f)] private float attackArcDegrees = 360f; // 360 = full circle, 120 = cone
        private Vector2 lockedRangedAimDirection;

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
        [SerializeField] private ulong currentTargetId = 0;

        private Rigidbody2D rb2D;
        protected EnemyAnimationDriver animDriver;
        private HealthBase health;

        [Header("Pathfinding")]
        private Seeker seeker;
        private Path currentPath;
        private int currentWaypoint = 0;
        private float pathUpdateInterval = 0.5f; // update path every 0.5s
        private float nextPathUpdateTime = 0f;
        private const float waypointReachedDistance = 0.3f; // how close to waypoint before moving to next

        private float lastAttackTime;

        private enum AttackMode { Melee, RangedHitscan }
        [Header("Attack Mode")]
        [SerializeField] private AttackMode attackMode = AttackMode.Melee;

        public enum ResetBehavior
        {
            RunBack,
            Teleport
        }

        [Header("Attack Telegraph")]
        [SerializeField] private SpriteRenderer meleeTelegraphOuter; // static danger zone
        [SerializeField] private SpriteRenderer meleeTelegraphInner; // growing fill
        [SerializeField] private LineRenderer rangedTelegraphOuter; // static aim line
        [SerializeField] private LineRenderer rangedTelegraphInner; // growing aim line
        [SerializeField] private Color telegraphOuterColor = new Color(1f, 0f, 0f, 0.2f); // faint
        [SerializeField] private Color telegraphInnerColor = new Color(1f, 0f, 0f, 0.6f); // bright

        public bool inTransition = false;

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
            
            spawnPos = rb2D != null ? rb2D.position : (Vector2)transform.position;
            lastCombatTime = -99999f;

            seeker = GetComponent<Seeker>();

            if (enemyCollider == null) { enemyCollider = GetComponent<Collider2D>(); }

            if(animDriver != null)
            {
                animDriver.onAttackHitFrame += OnEnemyAttackHitFrame;
            }
        }

        private void Update()
        {


            if (!IsServer) return;
            if (inTransition) return;

            // if boss, check if it's stunned
            var bossAbility = GetComponentInChildren<BossAbilityController>();
            if (bossAbility != null && bossAbility.isStunned.Value) return;


            if (health != null && health.Initialized && health.CurrentHealth.Value <= 0f)
            {
                if (animDriver != null) animDriver.SetMovement(Vector2.zero);
                return;
            }

            if (canLeash)
            {
                CheckLeash();
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
                    // Check if boss is performing ability (immobilize during special attacks)
                    if (bossAbility != null && bossAbility.IsPerformingAbility)
                    {
                        // Boss frozen during abilities
                        animDriver?.SetFacing(lastFacingDir);
                        animDriver?.SetMovement(Vector2.zero);
                    }
                    else
                    {
                        // Normal movement and attacks
                        HandleMovement();
                        if (primaryAbility != null)
                        {
                            TryAttack();
                        }
                    }
                }
                else
                {
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero);
                }


            }
            else
            {
                if (isReturningToSpawn) HandleMovement();
                else
                {
                    if (animDriver != null) animDriver.SetMovement(Vector2.zero);
                }
                
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

            // Update combat time if we have a target
            if(currentTarget != null)
            {
                lastCombatTime = Time.time;
                hasEnteredCombat = true;
            }
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

        protected virtual void HandleMovement()
        {
            if (isReturningToSpawn)
            {
                RunBackToSpawn();
                return;
            }

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

            // Request path to target (throttled by pathUpdateInterval)
            RequestPath(targetPos);

            // Get pathfinding direction (used for both melee and ranged)
            Vector2 pathDir = GetPathDirection();

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
                Vector2 newPos = selfPos + dir.normalized * (spd * Time.deltaTime);

                rb2D.MovePosition(newPos);
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
                    // Move away from target (direct, not pathfinding)
                    Vector2 awayFromTarget = -toTarget.normalized;
                    Move(awayFromTarget, .5f);
                    return;
                }

                // At ideal distance? Stop and attack
                if (surfaceDistance <= meleeIdealDistance)
                {
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero);
                    return;
                }

                // Too far? Chase using pathfinding
                if (pathDir.sqrMagnitude > 0.01f)
                {
                    Move(pathDir); // Follow the path
                }
                else
                {
                    Move(toTarget); // Fallback to direct if no path
                }
                return;
            }

            // ----- Ranged -----
            if (Time.frameCount % 60 == 0) // Log every second
            {
                Debug.Log($"[Ranged] {name} - HasPath: {currentPath != null}, Waypoint: {currentWaypoint}, PathDir: {pathDir}");
            }
            float tooCloseDist = rangedStopDistance - kiteDeadZone;
            float safeDist = rangedStopDistance + kiteDeadZone;

            // Check line of sight BEFORE kiting decision
            Vector2 origin = selfPos;
            Vector2 dir = toTarget.normalized;
            float rayDist = surfaceDistance;
            RaycastHit2D los = Physics2D.Raycast(origin, dir, rayDist, lineOfSightMask);
            bool hasLOS = (los.collider == null);

            // Only evaluate kiting if we have LOS
            if (hasLOS && Time.time >= nextKiteDecisiontime)
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

            // Stop kiting if we lose LOS (obstacle in the way)
            if (!hasLOS && isKiting)
            {
                isKiting = false;
            }

            if (isKiting)
            {
                // Kite away (direct movement, not pathfinding)
                Move(-toTarget, kiteSpeedMultiplier);
                return;
            }

            // If we have LOS and are in range - stop and shoot
            if (hasLOS && surfaceDistance <= rangedStopDistance)
            {
                animDriver?.SetFacing(lastFacingDir);
                animDriver?.SetMovement(Vector2.zero);
                return;
            }

            // No LOS or out of range - move using pathfinding to get better position
            if (pathDir.sqrMagnitude > 0.01f)
            {
                Move(pathDir); // Follow the path around obstacles
            }
            else
            {
                // No valid path - try direct movement as fallback
                if (surfaceDistance > rangedStopDistance)
                {
                    Move(toTarget);
                }
                else
                {
                    // In range but stuck - stop moving
                    animDriver?.SetFacing(lastFacingDir);
                    animDriver?.SetMovement(Vector2.zero);
                }
            }
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

                ShowMeleeTelegraph();

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

            lockedRangedAimDirection = dir;

            ShowRangedTelegraph();

            // Just play the animation - damage happens on animation event
            animDriver?.PlayAttack(lastFacingDir);
        }

        private void OnEnemyAttackHitFrame()
        {
            if (!IsServer) return;
            if (currentTarget == null) return;
            if (primaryAbility == null) return;

            lastCombatTime = Time.time; // combat activity

            HideMeleeTelegraph();
            HideRangedTelegraph();

            if (attackMode == AttackMode.Melee)
            {
                // NEW: Range check - player must STILL be in range when damage occurs
                if (currentTarget == null) return;

                Vector2 toTarget = (Vector2)currentTarget.position - rb2D.position;
                float distance = toTarget.magnitude;

                // Check collider distance if available
                Collider2D targetCol = currentTarget.GetComponentInParent<Collider2D>();
                bool inRange = false;

                if (enemyCollider != null && targetCol != null)
                {
                    ColliderDistance2D d = enemyCollider.Distance(targetCol);
                    float surfDist = d.isOverlapped ? 0f : d.distance;
                    inRange = surfDist <= attackRange;
                }
                else
                {
                    // Fallback to simple distance check
                    inRange = distance <= attackRange;
                }

                if (!inRange)
                {
                    return; // Player dodged!
                }

                // NEW: Arc check (if not 360 degree attack)
                if (attackArcDegrees < 360f)
                {
                    Vector2 attackDir = lastFacingDir.sqrMagnitude > 0.01f ? lastFacingDir : Vector2.down;
                    Vector2 toTargetNorm = toTarget.normalized;

                    float angle = Vector2.Angle(attackDir, toTargetNorm);
                    float halfArc = attackArcDegrees * 0.5f;

                    if (angle > halfArc)
                    {
                        return;
                    }
                }

                // Player is still in range & in arc - deal damage
                IDamageable dmg = currentTarget.GetComponentInParent<IDamageable>();
                if (dmg != null && primaryAbility.damage > 0f)
                {
                    float damageMultiplier = 1f;
                    var bossAbility = GetComponentInChildren<BossAbilityController>();
                    if(bossAbility != null)
                    {
                        damageMultiplier = bossAbility.damageDealingMultiplier.Value;
                    }

                    float finalDamage = primaryAbility.damage * damageMultiplier;


                    dmg.TakeDamage(finalDamage, NetworkObjectId);
                }

                if (primaryAbility.vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(primaryAbility.vfxPrefab, currentTarget.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }
            }
            else if (attackMode == AttackMode.RangedHitscan)
            {
                // Check if this ability uses projectiles instead of hitscan
                if (primaryAbility.projectilePrefab != null)
                {
                    FireProjectiles();
                    return;
                }

                // Original hitscan logic
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
                if (dmg != null && primaryAbility.damage > 0f)
                {
                    float damageMultiplier = 1f;
                    var bossAbility = GetComponentInChildren<BossAbilityController>();
                    if (bossAbility != null)
                    {
                        damageMultiplier = bossAbility.damageDealingMultiplier.Value;
                    }

                    float finalDamage = primaryAbility.damage * damageMultiplier;


                    dmg.TakeDamage(finalDamage, NetworkObjectId);
                }

                if (primaryAbility.vfxPrefab != null)
                {
                    GameObject vfx = Instantiate(primaryAbility.vfxPrefab, currentTarget.position, Quaternion.identity);
                    Destroy(vfx, 2f);
                }
            }
        }

        private void FireProjectiles()
        {
            if (currentTarget == null) return;
            if (primaryAbility.projectilePrefab == null) return;

            Vector2 origin = rb2D.position;
            // Use locked direction instead of current target position
            Vector2 baseDirection = lockedRangedAimDirection;

            int count = Mathf.Max(1, primaryAbility.projectileCount);
            float spread = primaryAbility.spreadAngle;

            // Fire projectiles in spread pattern
            for (int i = 0; i < count; i++)
            {
                float angle = 0f;

                if (count > 1)
                {
                    // Distribute evenly across spread angle
                    float step = spread / (count - 1);
                    angle = -spread / 2f + (i * step);
                }

                Vector2 direction = RotateVector(baseDirection, angle);

                // Spawn projectile
                GameObject proj = Instantiate(primaryAbility.projectilePrefab, origin, Quaternion.identity);
                var netObj = proj.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn(true);
                }

                // Set direction and speed
                var projBase = proj.GetComponent<ProjectileBase>();
                if (projBase != null)
                {
                    projBase.SetDirection(direction);
                    // Optionally override speed from ability if ProjectileBase exposes it
                }
            }
        }

        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);

            return new Vector2(
                cos * v.x - sin * v.y,
                sin * v.x + cos * v.y
            );
        }

        private Coroutine meleeTelegraphRoutine;

        private void ShowMeleeTelegraph()
        {
            if (meleeTelegraphOuter == null || meleeTelegraphInner == null) return;

            // Stop previous animation
            if (meleeTelegraphRoutine != null)
                StopCoroutine(meleeTelegraphRoutine);

            meleeTelegraphRoutine = StartCoroutine(AnimateMeleeTelegraph());
        }

        private IEnumerator AnimateMeleeTelegraph()
        {
            float duration = primaryAbility != null ? primaryAbility.windupSeconds : 0.5f;
            float baseScale = attackRange * 2f;

            // Setup outer ring (static)
            meleeTelegraphOuter.enabled = true;
            meleeTelegraphOuter.color = telegraphOuterColor;
            meleeTelegraphOuter.transform.localScale = new Vector3(baseScale, baseScale, 1f);

            // Setup inner ring (will grow)
            meleeTelegraphInner.enabled = true;
            meleeTelegraphInner.color = telegraphInnerColor;
            meleeTelegraphInner.transform.localScale = Vector3.zero;

            // Rotate both if it's an arc attack
            if (attackArcDegrees < 360f)
            {
                Vector2 attackDir = lastFacingDir.sqrMagnitude > 0.01f ? lastFacingDir : Vector2.down;
                float angle = Mathf.Atan2(attackDir.y, attackDir.x) * Mathf.Rad2Deg;
                Quaternion rot = Quaternion.Euler(0f, 0f, angle - 90f);

                meleeTelegraphOuter.transform.rotation = rot;
                meleeTelegraphInner.transform.rotation = rot;
            }

            // Grow inner ring over duration
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float scale = baseScale * t;
                meleeTelegraphInner.transform.localScale = new Vector3(scale, scale, 1f);

                yield return null;
            }

            // Ensure it reaches full size
            meleeTelegraphInner.transform.localScale = new Vector3(baseScale, baseScale, 1f);
        }

        private void HideMeleeTelegraph()
        {
            if (meleeTelegraphRoutine != null)
            {
                StopCoroutine(meleeTelegraphRoutine);
                meleeTelegraphRoutine = null;
            }

            if (meleeTelegraphOuter != null) meleeTelegraphOuter.enabled = false;
            if (meleeTelegraphInner != null) meleeTelegraphInner.enabled = false;
        }

        private Coroutine rangedTelegraphRoutine;

        private void ShowRangedTelegraph()
        {
            if (rangedTelegraphOuter == null || rangedTelegraphInner == null || currentTarget == null) return;

            if (rangedTelegraphRoutine != null)
                StopCoroutine(rangedTelegraphRoutine);

            rangedTelegraphRoutine = StartCoroutine(AnimateRangedTelegraph());
        }

        private IEnumerator AnimateRangedTelegraph()
        {
            float duration = primaryAbility != null ? primaryAbility.windupSeconds : 0.5f;

            Vector2 start = rb2D.position;
            // Use locked direction instead of tracking target
            float lineLength = rangedMaxRange;
            Vector2 end = start + lockedRangedAimDirection * lineLength;

            // Setup outer line (static, full length)
            rangedTelegraphOuter.enabled = true;
            rangedTelegraphOuter.startColor = telegraphOuterColor;
            rangedTelegraphOuter.endColor = telegraphOuterColor;
            rangedTelegraphOuter.positionCount = 2;
            rangedTelegraphOuter.SetPosition(0, start);
            rangedTelegraphOuter.SetPosition(1, end);

            // Setup inner line (will grow)
            rangedTelegraphInner.enabled = true;
            rangedTelegraphInner.startColor = telegraphInnerColor;
            rangedTelegraphInner.endColor = telegraphInnerColor;
            rangedTelegraphInner.positionCount = 2;
            rangedTelegraphInner.SetPosition(0, start);
            rangedTelegraphInner.SetPosition(1, start); // starts at 0 length

            // Grow inner line over duration
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Update enemy position (in case enemy moves), but NOT direction
                start = rb2D.position;
                end = start + lockedRangedAimDirection * lineLength;

                Vector2 currentEnd = Vector2.Lerp(start, end, t);

                rangedTelegraphOuter.SetPosition(0, start);
                rangedTelegraphOuter.SetPosition(1, end);

                rangedTelegraphInner.SetPosition(0, start);
                rangedTelegraphInner.SetPosition(1, currentEnd);

                yield return null;
            }

            // Ensure it reaches full length
            rangedTelegraphInner.SetPosition(1, end);
        }

        private void HideRangedTelegraph()
        {
            if (rangedTelegraphRoutine != null)
            {
                StopCoroutine(rangedTelegraphRoutine);
                rangedTelegraphRoutine = null;
            }

            if (rangedTelegraphOuter != null) rangedTelegraphOuter.enabled = false;
            if (rangedTelegraphInner != null) rangedTelegraphInner.enabled = false;
        }

        private void CheckLeash()
        {
            if (!IsServer) return;

            // Check if we're too far from spawn
            Vector2 currentPos = rb2D != null ? rb2D.position : (Vector2)transform.position;

            // If already returning, check if we reached spawn
            if (isReturningToSpawn)
            {
                float distToSpawn = Vector2.Distance(currentPos, spawnPos);
                Debug.Log($"[Leash] {name} returning to spawn, distance: {distToSpawn:F2}");

                if (distToSpawn < 0.5f) // close enough to spawn
                {
                    CompleteReset();
                }
                return;
            }

            // Debug: Check all conditions
            bool hasTarget = currentTarget != null;
            float timeSinceLastCombat = Time.time - lastCombatTime;
            bool outOfCombatLongEnough = timeSinceLastCombat >= outOfCombatDelay;
            float distanceFromSpawn = Vector2.Distance(currentPos, spawnPos);
            bool tooFarFromSpawn = distanceFromSpawn > leashDistance;

            // Log every few seconds to avoid spam
            if (Time.frameCount % 120 == 0) // Every ~2 seconds at 60fps
            {
                Debug.Log($"[Leash] {name} - Target: {hasTarget}, OutOfCombat: {outOfCombatLongEnough} ({timeSinceLastCombat:F1}s/{outOfCombatDelay}s), Distance: {distanceFromSpawn:F1}/{leashDistance}");
            }

            // TRIGGER 1: Boss pulled too far from spawn
            if (tooFarFromSpawn)
            {
                Debug.Log($"[Leash] {name} TRIGGERING RESET! Pulled too far: {distanceFromSpawn:F1} > {leashDistance}");
                StartReset();
                return;
            }

            // TRIGGER 2: Players disengaged (no target for X seconds)
            if (!hasTarget && outOfCombatLongEnough && hasEnteredCombat)
            {
                Debug.Log($"[Leash] {name} TRIGGERING RESET! Players disengaged for {timeSinceLastCombat:F1}s");
                StartReset();
            }
        }


        private void StartReset()
        {
            if (!IsServer) return;

            Debug.Log($"[Enemy] {name} starting reset (behavior: {resetBehavior})");

            if(resetBehavior == ResetBehavior.Teleport)
            {
                TeleportReset();
            }
            else
            {
                isReturningToSpawn = true;

                // clear threat and target so we don't attack while running
                if(threatTracker != null)
                {
                    threatTracker.ClearAllThreat();
                }
                currentTarget = null;
                currentTargetId = 0;

                LeashEffectClientRpc(false); // Visual: enemy is returning
            }
        }

        private void TeleportReset()
        {
            if (!IsServer) return;

            Debug.Log($"[Enemy] {name} teleporting to spawn!");

            // clear threat
            if (threatTracker != null) threatTracker.ClearAllThreat();

            // clear target
            currentTarget = null;
            currentTargetId = 0;

            // Teleport to Spawn
            if (rb2D != null)
            {
                rb2D.position = spawnPos;
            }
            else
            {
                transform.position = spawnPos;
            }
            CompleteReset();
        }

        private void CompleteReset()
        {
            if (!IsServer) return;

            Debug.Log($"[Enemy] {name} reset complete!");

            // reset health
            if (health != null) health.ServerSetFullHealth();

            // stop movement
            if (animDriver != null) animDriver.SetMovement(Vector2.zero);

            // reset flags
            isReturningToSpawn = false;
            lastCombatTime = -9999f;
            hasEnteredCombat = false;

            // boss specific reset hook
            var bossAI = this as BossAI;
            if(bossAI != null)
            {
                bossAI.OnBossReset();
            }

            // visual feedback
            LeashEffectClientRpc(true); // completed reset
        }

        private void RunBackToSpawn()
        {
            if (!IsServer) return;
            if (rb2D == null) return;

            Vector2 currentPos = rb2D.position;
            Vector2 toSpawn = spawnPos - currentPos;

            if(toSpawn.sqrMagnitude < 0.01f)
            {
                // arrived at spawn
                CompleteReset();
                return;
            }

            // run towards spawn at full speed
            Vector2 direction = toSpawn.normalized;
            SetFacing(direction);

            rb2D.MovePosition(currentPos + direction * (moveSpeed * Time.deltaTime));

            if(animDriver != null)
            {
                animDriver.SetMovement(direction);
            }
        }

        private void RequestPath(Vector2 targetPos)
        {
            if(seeker == null || !seeker.enabled) return;
            if (Time.time < nextPathUpdateTime) return; // Don't spam path requests

            nextPathUpdateTime = Time.time + pathUpdateInterval;

            Vector2 startPos = rb2D != null ? rb2D.position : (Vector2)transform.position;

            seeker.StartPath(startPos, targetPos, OnPathComplete);
        }

        private void OnPathComplete(Path p)
        {
            if (!p.error)
            {
                currentPath = p;
                currentWaypoint = 0;
            }
            else
            {
                Debug.LogWarning($"[Pathfinding] Path error for {name}: {p.errorLog}");
            }
        }

        private Vector2 GetPathDirection()
        {
            if (currentPath == null || currentWaypoint >= currentPath.vectorPath.Count) return Vector2.zero;

            Vector2 currentPos = rb2D != null ? rb2D.position : (Vector2)transform.position;
            Vector2 waypointPos = currentPath.vectorPath[currentWaypoint];

            float distanceToWaypoint = Vector2.Distance(currentPos, waypointPos);

            // reached this waypoint move to next
            if(distanceToWaypoint < waypointReachedDistance)
            {
                currentWaypoint++;

                // reached end of path
                if(currentWaypoint >= currentPath.vectorPath.Count)
                {
                    return Vector2.zero;
                }

                waypointPos = currentPath.vectorPath[currentWaypoint];
            }

            // Direction to current waypoint
            return (waypointPos - currentPos).normalized;
        }

        [ClientRpc]
        private void LeashEffectClientRpc(bool completed)
        {
            if (completed)
            {
                Debug.Log($"[Enemy] {name} reset complete (visual feedback)");
                // TODO: Flash effect, particle burst
            }
            else
            {
                Debug.Log($"[Enemy] {name} returning to spawn (visual feedback)");
                // TODO: Trail effect, speed lines
            }
        }


        private void ServerResetEnemy()
        {
            if (!IsServer) return;

            Debug.Log($"[Enemy] {name} leashing back to spawn!");

            // Clear threat
            if(threatTracker != null)
            {
                threatTracker.ClearAllThreat();
            }

            // clear target
            currentTarget = null;
            currentTargetId = 0;

            // Reset health
            if(health != null)
            {
                health.ServerSetFullHealth();
            }

            // Teleport to Spawn
            if(rb2D != null)
            {
                rb2D.position = spawnPos;
            }
            else
            {
                transform.position = spawnPos;
            }

            // stop Movement
            if (animDriver != null)
            {
                animDriver.SetMovement(Vector2.zero);
            }

            // reset combat timer
            lastCombatTime = Time.time;
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

