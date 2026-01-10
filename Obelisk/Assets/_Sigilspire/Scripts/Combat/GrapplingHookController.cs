using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.DamageInterfaces;

namespace Combat
{
    public class GrapplingHookController : NetworkBehaviour, IWeaponController
    {
        // ===================================================================
        // Constants =========================================================
        // ===================================================================

        private const byte PhaseNone = 0;
        private const byte PhaseCasting = 1;
        private const byte PhaseAttached = 2;
        private const byte PhaseRetracting = 3;

        // ===================================================================
        // Inspector - Ability / Sigil =======================================
        // ===================================================================

        [Header("Ability / Sigil")]
        [SerializeField] private Ability baseAbility;
        [SerializeField] private SigilDefinition equippedSigil;
        [SerializeField] private string equippedSigilId;

        [Header("Progress / Inventory")]
        [SerializeField] private SigilInventory sigilInventory;

        // ===================================================================
        // Inspector - Grapple Settings ======================================
        // ===================================================================

        [Header("Grapple Settings")]
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask grappleLayers;
        [SerializeField] private float minDistanceToStop = 0.2f;

        [Header("Timing")]
        [SerializeField] private float castDuration = 0.35f;
        [SerializeField] private float retractDuration = 0.30f;

        // ===================================================================
        // Inspector - Visual References =====================================
        // ===================================================================

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer grappleRenderer;
        [SerializeField] private Transform vfxSpawnPoint; // muzzle
        [SerializeField] private LineRenderer lineRenderer;

        // ===================================================================
        // Inspector - Player References =====================================
        // ===================================================================

        [Header("Player References")]
        [SerializeField] private Rigidbody2D playerRb;
        [SerializeField] private Player.PlayerController playerController;

        // ===================================================================
        // Public / External State ===========================================
        // ===================================================================

        public NetworkVariable<bool> IsGrappling = new NetworkVariable<bool>(false);

        // ===================================================================
        // Runtime - Ability Visuals =========================================
        // ===================================================================

        private GameObject grappleVfxPrefab;
        private float lastGrappleTime;

        // ===================================================================
        // Runtime - Server Pull Data (Server Only) ==========================
        // ===================================================================

        private bool serverHasHit;
        private Vector2 serverTargetPoint;

        private NetworkObject serverPulledEnemy;
        private Rigidbody2D serverPulledEnemyRb;
        private bool serverPullEnemyToPlayer;

        // ===================================================================
        // Runtime - Networked Grapple State =================================
        // ===================================================================

        private NetworkVariable<byte> phase = new NetworkVariable<byte>(PhaseNone);
        private NetworkVariable<Vector2> netStartPoint = new NetworkVariable<Vector2>();
        private NetworkVariable<Vector2> netEndPoint = new NetworkVariable<Vector2>();
        private NetworkVariable<double> phaseStartServerTime = new NetworkVariable<double>(0.0);

        // If we attack to an enemy, clients can render to the enemy position
        private NetworkVariable<ulong> attachedEnemyId = new NetworkVariable<ulong>(0);

        // Local cache for phase change detection (for animation triggers).
        private byte lastPhaseLocal = PhaseNone;

        // ===================================================================
        // Unity Callbacks ===================================================
        // ===================================================================

        private void Awake()
        {
            if (playerRb == null) playerRb = GetComponentInParent<Rigidbody2D>();

            if (playerController == null) playerController = GetComponentInParent<Player.PlayerController>();

            if (sigilInventory == null) sigilInventory = GetComponentInParent<SigilInventory>();

            if (equippedSigil != null && string.IsNullOrEmpty(equippedSigilId)) equippedSigilId = equippedSigil.id;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

            if (lineRenderer != null) lineRenderer.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            lastPhaseLocal = phase.Value;

            // Owner movement unlock should be driven by phase returning to None
            phase.OnValueChanged += OnPhaseChanged;
        }

        private void Update()
        {
            // server runs the grapple simulation and phase transitions.

            if (!IsServer) return;

            ServerTick();
        }

        private void LateUpdate()
        {
            // Everyone draws the rope based on replicated state.
            DrawLineAndPlayLocalAnim();
        }

        // ===================================================================
        // Public API ========================================================
        // ===================================================================

        public void RequestUseAbility(Vector2 inputDirection)
        {
            RequestFireGrapple(inputDirection);
        }

        public void RequestFireGrapple(Vector2 inputDirection)
        {
            if (!IsOwner) return;
            if (baseAbility == null) return;

            // Prevent spaming while any phase is active.
            if (phase.Value != PhaseNone) return;

            // Local cooldown check (simple, responsive)
            var stats = GetCurrentStats();
            float cooldown = GetEffectiveCooldown(stats);
            if (Time.time - lastGrappleTime < cooldown) return;
            lastGrappleTime = Time.time;

            Vector2 dir = inputDirection.sqrMagnitude > 0.01f ? inputDirection : Vector2.up;

            // Lock movement immediately on the owner.
            if (playerController != null) playerController.SetMovementLocked(true);

            // Request server to begin a grapple attempt.
            FireGrappleServerRpc(dir);
        }

        // ===================================================================
        // Visual Set / Sigil ================================================
        // ===================================================================

        public void SetEquippedSigil(SigilDefinition sigil)
        {
            equippedSigil = sigil;
            equippedSigilId = sigil != null ? sigil.id : string.Empty;
        }

        public void ApplyVisualSet(WeaponVisualSet set)
        {
            if (set == null) return;

            if (weaponAnimator != null && set.overrideController != null) weaponAnimator.runtimeAnimatorController = set.overrideController;

            if (grappleRenderer != null && set.idleSprite != null) grappleRenderer.sprite = set.idleSprite;

            grappleVfxPrefab = set.attackVfx;
        }

        // ===================================================================
        // Ability Stat Helpers ==============================================
        // ===================================================================

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
            float baseForce = stats.grappleForce > 0f ? stats.grappleForce : (baseAbility != null ? baseAbility.grappleForce : 15f);

            return baseForce;
        }

        private float GetEffectiveDamage(EffectiveAbilityStats stats)
        {
            if (stats.damage > 0f) return stats.damage;

            return baseAbility != null ? baseAbility.damage : 0f;
        }

        // ===================================================================
        // Networking - Begin Grapple ========================================
        // ===================================================================

        [ServerRpc]
        private void FireGrappleServerRpc(Vector2 direction)
        {
            Vector2 start = vfxSpawnPoint != null ? (Vector2)vfxSpawnPoint.position : (Vector2)transform.position;

            RaycastHit2D hit = Physics2D.Raycast(start, direction, maxDistance, grappleLayers);

            serverHasHit = hit.collider != null;
            Vector2 end = serverHasHit ? hit.point : start + direction.normalized * maxDistance;

            // Reset server pull data.
            serverTargetPoint = end;
            serverPulledEnemy = null;
            serverPulledEnemyRb = null;
            serverPullEnemyToPlayer = false;
            attachedEnemyId.Value = 0;

            if (serverHasHit)
            {
                // Pull rules: enemy to player OR player to point
                var pullable = hit.collider.GetComponentInParent<Combat.DamageInterfaces.IGrapplePullable>();
                if (pullable != null && pullable.ShouldPullToPlayer())
                {
                    serverPullEnemyToPlayer = true;
                    serverPulledEnemyRb = hit.collider.GetComponentInParent<Rigidbody2D>();

                    var no = hit.collider.GetComponentInParent<NetworkObject>();
                    if (no != null) serverPulledEnemy = no;

                    if (serverPulledEnemy != null) attachedEnemyId.Value = serverPulledEnemy.NetworkObjectId;
                }

                // Apply a small damage on attach.
                var stats = GetCurrentStats();
                float damage = GetEffectiveDamage(stats);

                var dmg = hit.collider.GetComponent<IDamageable>();
                if (dmg != null && damage > 0f) dmg.TakeDamage(damage);
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

        // ===================================================================
        // Server Simulation =================================================
        // ===================================================================

        private void ServerTick()
        {
            byte p = phase.Value;
            if (p == PhaseNone) return;

            double now = NetworkManager.ServerTime.Time;
            float elapsed = (float)(now - phaseStartServerTime.Value);

            if (p == PhaseCasting)
            {
                // at the end of the cast, either attach (hit) or retract (miss).
                if (elapsed >= castDuration)
                {
                    if (serverHasHit)
                    {
                        phase.Value = PhaseAttached;
                        phaseStartServerTime.Value = now;

                        // spawnVFX on attach for all clients
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

                bool complete = ServerSimulatePull(pullSpeed, Time.deltaTime);

                if (complete)
                {
                    // When pull finishes, immediately retract.
                    phase.Value = PhaseRetracting;
                    phaseStartServerTime.Value = now;
                }
                return;
            }

            if (p == PhaseRetracting)
            {
                // When retract finishes, end grapple.
                if (elapsed >= retractDuration)
                {
                    phase.Value = PhaseNone;
                    phaseStartServerTime.Value = now;

                    IsGrappling.Value = false;
                    serverHasHit = false;
                    attachedEnemyId.Value = 0;
                }
                return;
            }
        }

        private bool ServerSimulatePull(float pullSpeed, float dt)
        {
            // If pulling enemy to player
            if (serverPullEnemyToPlayer && serverPulledEnemyRb != null && playerRb != null)
            {
                Vector2 enemyPos = serverPulledEnemyRb.position;
                Vector2 playerPos = playerRb.position;

                Vector2 toPlayer = playerPos - enemyPos;
                float dist = toPlayer.magnitude;
                float step = pullSpeed * dt;

                if (dist <= minDistanceToStop || dist <= step)
                {
                    serverPulledEnemyRb.MovePosition(playerPos);
                    return true;
                }

                Vector2 newPos = enemyPos + toPlayer.normalized * step;
                serverPulledEnemyRb.MovePosition(newPos);
                return false;
            }

            // Default: pull player to the target point
            if (playerRb == null) return true;

            Vector2 currentPos = playerRb.position;
            Vector2 toTarget = serverTargetPoint - currentPos;
            float dist2 = toTarget.magnitude;
            float step2 = pullSpeed * dt;

            if (dist2 <= minDistanceToStop || dist2 <= step2)
            {
                playerRb.MovePosition(serverTargetPoint);
                return true;
            }

            Vector2 newPos2 = currentPos + toTarget.normalized * step2;
            playerRb.MovePosition(newPos2);
            return false;
        }

        // ===================================================================
        // Client Visuals / Animations =======================================
        // ===================================================================

        private void OnPhaseChanged(byte oldPhase, byte newPhase)
        {
            // Owner unlocks when returning to None.
            if (IsOwner && newPhase == PhaseNone)
            {
                if (playerController != null) playerController.SetMovementLocked(false);
            }
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

            if (p == PhaseCasting) { currentEnd = Vector2.Lerp(start, end, t); }
            else if (p == PhaseAttached) { currentEnd = GetAttachedEnd(end); }
            else if (p == PhaseRetracting) { currentEnd = Vector2.Lerp(end, start, t); }

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

        // ===================================================================
        // Convenience =======================================================
        // ===================================================================

        public bool IsCurrentlyGrapplingLocal => phase.Value != PhaseNone;


        /*[Header("Ability / Sigil")]
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

            localTargetPoint = (Vector2)end;
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
            // if (lineRenderer  != null) { lineRenderer.enabled = false; }

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
            *//*if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }*//*

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

            //if (!IsGrappling.Value) return;

            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;

            Vector3 start = vfxSpawnPoint != null ? vfxSpawnPoint.position : transform.position;
            Vector3 end = new Vector3(localTargetPoint.x, localTargetPoint.y, 0f);

            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        public bool IsCurrentlyGrapplingLocal => IsGrappling.Value;*/
    }
}
