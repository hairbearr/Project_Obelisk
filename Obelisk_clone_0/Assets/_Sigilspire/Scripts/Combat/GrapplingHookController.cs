using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Combat
{
    public class GrapplingHookController : NetworkBehaviour, IWeaponController
    {
        #region Constants
        private const byte PhaseNone = 0;
        private const byte PhaseCasting = 1;
        private const byte PhaseAttached = 2;
        private const byte PhaseRetracting = 3;
        #endregion

        #region Inspector - Ability / Sigil
        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;
        #endregion

        #region Inspector - Grapple Settings
        [Header("Grapple Settings")]
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask grappleLayers;
        [SerializeField] private float minDistanceToStop = 0.2f;
        [SerializeField] private GrappleTarget serverGrappleTarget;

        [Header("Timing")]
        [SerializeField] private float castDuration = 0.35f;
        [SerializeField] private float retractDuration = 0.25f;
        #endregion

        #region Inspector - Visual References
        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer grappleRenderer;
        [SerializeField] private Transform vfxSpawnPoint; // muzzle
        [SerializeField] private LineRenderer lineRenderer;
        #endregion

        #region Inspector - Player References
        [Header("Player References")]
        [SerializeField] private Rigidbody2D playerRb;
        [SerializeField] private Player.PlayerController playerController;
        [SerializeField] private Collider2D playerCollider;
        #endregion

        #region Public / External State
        public NetworkVariable<bool> IsGrappling = new NetworkVariable<bool>(false);
        #endregion

        #region Runtime - Ability Visuals
        private GameObject grappleVfxPrefab;
        private float lastGrappleTime;

        [Header("Input")]
        [SerializeField] private float localPressDebounce = 0.10f;

        private float nextAllowedLocalPressTime;
        private Vector2 lastRequestedDir;
        #endregion

        #region Runtime - Server Pull Data (Server Only)
        [SerializeField] private float stopDistanceFromSurface = 0.25f;
        private Collider2D serverHitCollider;

        private bool serverHasHit;
        private Vector2 serverTargetPoint;

        private NetworkObject serverPulledEnemy;
        private Rigidbody2D serverPulledEnemyRb;
        private bool serverPullEnemyToPlayer;
        private Collider2D serverPulledEnemyCol;
        private bool ignoringPullCollision;


        private IGrapplePullable serverPullable;
        #endregion

        #region Runtime - Networked Grapple State
        private NetworkVariable<byte> phase = new NetworkVariable<byte>(PhaseNone);
        private NetworkVariable<Vector2> netStartPoint = new NetworkVariable<Vector2>();
        private NetworkVariable<Vector2> netEndPoint = new NetworkVariable<Vector2>();
        private NetworkVariable<double> phaseStartServerTime = new NetworkVariable<double>(0.0);

        // If we attach to an enemy, clients can render to the enemy position
        private NetworkVariable<ulong> attachedEnemyId = new NetworkVariable<ulong>(0);

        // Local cache for phase change detection (for animation triggers).
        private byte lastPhaseLocal = PhaseNone;

        // Input buffering so presses during retract/cooldown are not lost
        private bool queuedFire;
        private Vector2 queuedDir;
        #endregion

        #region Unity Callbacks
        private void Awake()
        {
            if (playerRb == null) playerRb = GetComponentInParent<Rigidbody2D>();
            if (playerController == null) playerController = GetComponentInParent<Player.PlayerController>();
            if (sigilInventory == null) sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId))
                equippedSigilId = equippedSigil.id;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null) lineRenderer.enabled = false;

            if (playerCollider == null) playerCollider = GetComponentInParent<Collider2D>();
        }

        public override void OnNetworkSpawn()
        {
            lastPhaseLocal = phase.Value;
            // Owner movement unlock should be driven by phase returning to None
            phase.OnValueChanged += OnPhaseChanged;
        }

        private void FixedUpdate()
        {
            // server runs the grapple simulation and phase transitions.
            if (!IsServer) return;
            ServerTick(Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            // Everyone draws the rope based on replicated state.
            DrawLineAndPlayLocalAnim();
        }

        public override void OnNetworkDespawn()
        {
            phase.OnValueChanged -= OnPhaseChanged;

            if (IsServer)
            {
                ClearPullCollisionIgnore();

                if (serverGrappleTarget != null)
                {
                    serverGrappleTarget.ServerEndGrapple();
                    serverGrappleTarget = null;
                }
            }
        }
        #endregion

        #region Public API
        public void RequestUseAbility(Vector2 inputDirection)
        {
            RequestFireGrapple(inputDirection);
        }

        public void RequestFireGrapple(Vector2 inputDirection)
        {
            if (!IsOwner) return;
            if (baseAbility == null) return;

            // Debounce: ignore presses too close together
            if (Time.time < nextAllowedLocalPressTime) return;
            nextAllowedLocalPressTime = Time.time + localPressDebounce;

            Vector2 dir = inputDirection.sqrMagnitude > 0.01f ? inputDirection : Vector2.up;
            lastRequestedDir = dir;

            // If we are in any phase, queue and fire when done
            if (phase.Value != PhaseNone)
            {
                queuedFire = true;
                queuedDir = dir;
                return;
            }

            // local cooldown check
            var stats = GetCurrentStats();
            float cooldown = GetEffectiveCooldown(stats);

            // If still on cooldown, queue and fire when cooldown is over
            if (Time.time - lastGrappleTime < cooldown)
            {
                queuedFire = true;
                queuedDir = dir;
                return;
            }

            lastGrappleTime = Time.time;

            // Request server to begin a grapple attempt
            FireGrappleServerRpc(dir);
        }

        public bool CanUseAbility()
        {
            if (!IsOwner) return false;
            if (baseAbility == null) return false;
            if (phase.Value != PhaseNone) return false;

            var stats = GetCurrentStats();
            float cooldown = GetEffectiveCooldown(stats);

            return (Time.time - lastGrappleTime) >= cooldown;
        }

        public float GetCooldownRemaining()
        {
            if (baseAbility == null) return 0f;

            var stats = GetCurrentStats();
            float cooldown = GetEffectiveCooldown(stats);

            float remaining = cooldown - (Time.time - lastGrappleTime);
            return Mathf.Max(0f, remaining);
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
            if (set == null) return;

            if (weaponAnimator != null && set.overrideController != null)
                weaponAnimator.runtimeAnimatorController = set.overrideController;

            if (grappleRenderer != null && set.idleSprite != null)
                grappleRenderer.sprite = set.idleSprite;

            grappleVfxPrefab = set.attackVfx;
        }
        #endregion

        #region Ability Stat Helpers
        private EffectiveAbilityStats GetCurrentStats()
        {
            if (baseAbility == null) return default;

            SigilProgressData progress = null;

            if (equippedSigil != null && sigilInventory != null && !string.IsNullOrEmpty(equippedSigilId))
            {
                progress = sigilInventory.GetOrCreateProgress(equippedSigilId);
            }

            return SigilEvaluator.GetEffectiveStats(baseAbility, equippedSigil, progress);
        }

        private float GetEffectiveCooldown(EffectiveAbilityStats stats)
        {
            if (stats.cooldown <= 0f && baseAbility != null) return baseAbility.cooldown;
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
            if (stats.damage > 0f) return stats.damage;
            return baseAbility != null ? baseAbility.damage : 0f;
        }
        #endregion

        #region Networking - Begin Grapple
        [ServerRpc]
        private void FireGrappleServerRpc(Vector2 direction)
        {

            // If we were somehow still holding an old grapple target, release it.
            if(serverGrappleTarget != null)
            {
                serverGrappleTarget.ServerEndGrapple();
                serverGrappleTarget = null;
            }
            ClearPullCollisionIgnore();

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.up;

            Vector2 start = vfxSpawnPoint != null ? (Vector2)vfxSpawnPoint.position : (Vector2)transform.position;

            RaycastHit2D hit = Physics2D.Raycast(start, direction, maxDistance, grappleLayers);

            serverHasHit = hit.collider != null;
            Vector2 end = serverHasHit ? hit.point : start + direction.normalized * maxDistance;

            serverHitCollider = hit.collider;
            if (!serverHasHit) serverHitCollider = null;

            // Reset server pull data.
            serverTargetPoint = end;
            serverPulledEnemy = null;
            serverPulledEnemyRb = null;
            serverPullEnemyToPlayer = false;
            serverPullable = null; // IMPORTANT: clear stale reference on miss/new cast
            attachedEnemyId.Value = 0;

            if (serverHasHit)
            {
                serverGrappleTarget = null;

                var pullable = hit.collider.GetComponentInParent<IGrapplePullable>();
                serverPullable = pullable;

                if (pullable != null && pullable.ShouldPullToPlayer())
                {
                    serverPullEnemyToPlayer = true;

                    var no = hit.collider.GetComponentInParent<NetworkObject>();
                    if (no != null)
                    {
                        serverPulledEnemy = no;
                        serverPulledEnemyRb = no.GetComponentInParent<Rigidbody2D>();
                        attachedEnemyId.Value = no.NetworkObjectId;

                        serverGrappleTarget = no.GetComponentInParent<GrappleTarget>();
                        if (serverGrappleTarget != null)
                            serverGrappleTarget.ServerBeginGrapple();
                    }
                }

                // Apply a small damage on attach.
                var stats = GetCurrentStats();
                float damage = GetEffectiveDamage(stats);

                var dmg = hit.collider.GetComponent<IDamageable>();

                var attackerNO = GetComponentInParent<NetworkObject>();
                ulong attackerId = attackerNO != null ? attackerNO.NetworkObjectId : NetworkObjectId;

                if (dmg != null && damage > 0f)
                    dmg.TakeDamage(damage, attackerId);
            }

            // Replicate cast start/end for everyone to render.
            netStartPoint.Value = start;
            netEndPoint.Value = end;

            // Enter casting phase.
            phase.Value = PhaseCasting;
            phaseStartServerTime.Value = NetworkManager.ServerTime.Time;

            // Compatibility bool: treat "grappling active" as any non-none phase.
            IsGrappling.Value = true;
        }
        #endregion

        #region Server Simulation
        private void ServerTick(float dt)
        {
            byte p = phase.Value;
            if (p == PhaseNone) return;

            double now = NetworkManager.ServerTime.Time;

            // Abort cleanly if the attached target despawned mid-grapple
            ServerAbortGrappleIfTargetGone(now);

            // Re-read phase in case abort changed it
            p = phase.Value;
            if (p == PhaseNone) return;

            float elapsed = (float)(now - phaseStartServerTime.Value);

            if (p == PhaseCasting)
            {
                if (elapsed >= castDuration)
                {
                    if (serverHasHit)
                    {
                        phase.Value = PhaseAttached;
                        phaseStartServerTime.Value = now;
                        SpawnAttachVfxClientRpc(netStartPoint.Value);
                    }
                    else
                    {
                        phase.Value = PhaseRetracting;
                        phaseStartServerTime.Value = now;
                    }
                }
                return;
            }

            if (p == PhaseAttached)
            {
                var stats = GetCurrentStats();
                float pullSpeed = GetEffectivePullSpeed(stats);

                bool complete = ServerSimulatePull(pullSpeed, dt);

                if (complete)
                {
                    phase.Value = PhaseRetracting;
                    phaseStartServerTime.Value = now;
                }
                return;
            }

            if (p == PhaseRetracting)
            {
                if (elapsed >= retractDuration)
                {
                    ClearPullCollisionIgnore();

                    phase.Value = PhaseNone;
                    phaseStartServerTime.Value = now;

                    IsGrappling.Value = false;
                    serverHasHit = false;
                    attachedEnemyId.Value = 0;

                    if (serverGrappleTarget != null)
                    {
                        serverGrappleTarget.ServerEndGrapple();
                        serverGrappleTarget = null;
                    }

                    serverPullable = null;
                    serverPulledEnemy = null;
                    serverPulledEnemyRb = null;
                    serverPullEnemyToPlayer = false;
                    serverHitCollider = null;
                }
                return;
            }
        }

        private void ClearPullCollisionIgnore()
        {
            if (!IsServer) return;
            if (!ignoringPullCollision) return;

            if (playerCollider != null && serverPulledEnemyCol != null) { Physics2D.IgnoreCollision(playerCollider, serverPulledEnemyCol, false); }

            ignoringPullCollision = false;
            serverPulledEnemyCol = null;
        }

        private bool ServerSimulatePull(float pullSpeed, float dt)
        {
            float step = pullSpeed * dt;

            // Pull enemy to player
            if (serverPullEnemyToPlayer)
            {
                if (serverPullable == null) return true;
                if (playerRb == null) return true;

                Vector2 enemyPosForClosest =
                    serverPulledEnemyRb != null
                        ? serverPulledEnemyRb.position
                        : (Vector2)((Component)serverPullable).transform.position;

                Vector2 pullPoint =
                    playerCollider != null
                        ? playerCollider.ClosestPoint(enemyPosForClosest)
                        : (Vector2)playerRb.position;

                // Ignore collision once at pull start
                if (!ignoringPullCollision && playerCollider != null)
                {
                    Collider2D enemyCol =
                        serverPulledEnemyRb != null ? serverPulledEnemyRb.GetComponent<Collider2D>() :
                        serverGrappleTarget != null ? serverGrappleTarget.GetComponent<Collider2D>() :
                        null;

                    if (enemyCol != null)
                    {
                        serverPulledEnemyCol = enemyCol;
                        Physics2D.IgnoreCollision(playerCollider, serverPulledEnemyCol, true);
                        ignoringPullCollision = true;
                    }
                }

                if (serverPullable == null) return true;

                serverPullable.PullTowards(pullPoint, pullSpeed);

                if (playerCollider != null && serverPulledEnemyRb != null)
                {
                    var enemyCol = serverPulledEnemyRb.GetComponent<Collider2D>();
                    if (enemyCol != null)
                    {
                        ColliderDistance2D d = enemyCol.Distance(playerCollider);
                        if (d.isOverlapped || d.distance <= stopDistanceFromSurface)
                            return true;
                    }
                }

                float dist = Vector2.Distance(enemyPosForClosest, pullPoint);
                if (dist <= stopDistanceFromSurface + minDistanceToStop || dist <= step)
                    return true;

                return false;
            }

            // Pull player to target
            if (playerRb == null) return true;

            Vector2 playerPos = playerRb.position;

            if (serverHasHit && serverHitCollider == null)
                return true;

            if (serverHitCollider != null && playerCollider != null)
            {
                ColliderDistance2D d = playerCollider.Distance(serverHitCollider);
                if (d.isOverlapped || d.distance <= stopDistanceFromSurface)
                    return true;
            }

            Vector2 surfacePoint = serverHitCollider != null
                ? serverHitCollider.ClosestPoint(playerPos)
                : serverTargetPoint;

            Vector2 toSurface = surfacePoint - playerPos;
            float distToSurface = toSurface.magnitude;

            Vector2 desiredTarget = surfacePoint;

            if (serverHitCollider != null && distToSurface > 0.0001f)
            {
                desiredTarget = surfacePoint - toSurface.normalized * stopDistanceFromSurface;
            }

            Vector2 toTarget = desiredTarget - playerPos;
            float distToTarget = toTarget.magnitude;

            if (distToTarget <= minDistanceToStop || distToTarget <= step)
            {
                playerRb.MovePosition(desiredTarget);
                return true;
            }

            playerRb.MovePosition(playerPos + toTarget.normalized * step);
            return false;
        }

        #endregion

        #region Client Visuals / Animations
        private void OnPhaseChanged(byte oldPhase, byte newPhase)
        {
            if (!IsOwner) return;

            // Lock when the server begins casting
            if (newPhase == PhaseCasting)
            {
                if (playerController != null) playerController.SetMovementLocked(true);
            }

            // Unlock when the server finishes retracting.
            if (newPhase == PhaseNone)
            {
                if (playerController != null) playerController.SetMovementLocked(false);

                // if the player pressed grapple while we were busy, fire now
                if (queuedFire)
                {
                    queuedFire = false;
                    RequestFireGrapple(queuedDir);
                }
            }
        }

        private void ServerAbortGrappleIfTargetGone(double now)
        {
            if (!IsServer) return;

            // If we believe we're attached to an enemy, verify it's still spawned.
            if (attachedEnemyId.Value != 0)
            {
                var sm = NetworkManager != null ? NetworkManager.SpawnManager : null;
                bool stillSpawned = (sm != null && sm.SpawnedObjects.ContainsKey(attachedEnemyId.Value));

                if (!stillSpawned)
                {
                    // Target despawned mid-grapple. Cleanly retract/end.
                    ClearPullCollisionIgnore();

                    // Clear cached refs that could be "missing"
                    serverHasHit = false;
                    serverHitCollider = null;
                    serverPullable = null;
                    serverPulledEnemy = null;
                    serverPulledEnemyRb = null;
                    serverPullEnemyToPlayer = false;

                    if (serverGrappleTarget != null)
                    {
                        // If this is already destroyed, Unity null-check will evaluate true anyway
                        serverGrappleTarget.ServerEndGrapple();
                        serverGrappleTarget = null;
                    }

                    attachedEnemyId.Value = 0;

                    // Either retract quickly or just end immediately.
                    phase.Value = PhaseRetracting;
                    phaseStartServerTime.Value = now;
                }
            }

            // Also guard against Unity "fake null" components
            if (serverHitCollider == null) serverHitCollider = null;
            if (serverGrappleTarget == null) serverGrappleTarget = null;
            if (serverPullable == null) serverPullable = null;
        }

        private void DrawLineAndPlayLocalAnim()
        {
            // Trigger local animations on phase changes
            byte p = phase.Value;
            if (p != lastPhaseLocal)
            {
                HandleLocalPhaseAnim(lastPhaseLocal, p);
                lastPhaseLocal = p;
            }

            if (lineRenderer == null) return;

            if (p == PhaseNone)
            {
                lineRenderer.enabled = false;
                return;
            }

            // Always draw line when not None.
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;

            Vector2 start = netStartPoint.Value;
            Vector2 end = netEndPoint.Value;

            // Track muzzle for the start point so it matches animation
            if (vfxSpawnPoint != null) start = vfxSpawnPoint.position;

            float t = GetPhaseT();

            Vector2 currentEnd = end;

            if (p == PhaseCasting)
            {
                t = EastOutCubic(t); // fast snap outward
                currentEnd = Vector2.Lerp(start, end, t);
            }
            else if (p == PhaseAttached)
            {
                currentEnd = GetAttachedEnd(end);
            }
            else if (p == PhaseRetracting)
            {
                t = EaseInCubic(t); // quick snap back
                currentEnd = Vector2.Lerp(end, start, t);
            }

            lineRenderer.SetPosition(0, new Vector3(start.x, start.y, 0f));
            lineRenderer.SetPosition(1, new Vector3(currentEnd.x, currentEnd.y, 0f));
        }

        private float GetPhaseT()
        {
            // Use Server time on all clients so it stays in sync.
            double now = NetworkManager != null ? NetworkManager.ServerTime.Time : Time.time;
            float elapsed = (float)(now - phaseStartServerTime.Value);

            if (phase.Value == PhaseCasting) return Mathf.Clamp01(elapsed / Mathf.Max(0.001f, castDuration));
            if (phase.Value == PhaseRetracting) return Mathf.Clamp01(elapsed / Mathf.Max(0.001f, retractDuration));
            return 1f;
        }

        private float EastOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private float EaseInCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        private Vector2 GetAttachedEnd(Vector2 fallbackEnd)
        {
            if (attachedEnemyId.Value == 0) return fallbackEnd;

            if (NetworkManager == null) return fallbackEnd;
            var sm = NetworkManager.SpawnManager;
            if (sm == null) return fallbackEnd;

            if (!sm.SpawnedObjects.TryGetValue(attachedEnemyId.Value, out NetworkObject obj)) return fallbackEnd;

            return obj.transform.position;
        }

        private void HandleLocalPhaseAnim(byte oldP, byte newP)
        {
            if (weaponAnimator == null) return;

            if (newP == PhaseCasting)
            {
                weaponAnimator.ResetTrigger("GrappleRetract");
                weaponAnimator.SetTrigger("GrappleCast");
            }
            else if (newP == PhaseRetracting)
            {
                weaponAnimator.ResetTrigger("GrappleCast");
                weaponAnimator.SetTrigger("GrappleRetract");
            }
        }

        [ClientRpc]
        private void SpawnAttachVfxClientRpc(Vector2 startPoint)
        {
            if (grappleVfxPrefab == null) return;

            Vector3 pos = vfxSpawnPoint != null ? vfxSpawnPoint.position : new Vector3(startPoint.x, startPoint.y, 0f);
            var vfx = Object.Instantiate(grappleVfxPrefab, pos, Quaternion.identity);
            Object.Destroy(vfx, 2f);
        }
        #endregion

        #region Convenience
        public bool IsCurrentlyGrapplingLocal => phase.Value != PhaseNone;
        #endregion
    }
}
