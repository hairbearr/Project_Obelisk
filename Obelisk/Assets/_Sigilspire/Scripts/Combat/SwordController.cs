using Combat.AbilitySystem;
using Combat.DamageInterfaces;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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

        [Header("Timings")]
        [SerializeField] private float hitWindupSeconds = 0.12f; // tune per animation
        [SerializeField] private float hitActiveSeconds = 0.05f; // how long the hitbox is active
        [SerializeField] private bool useWindupTiming = true;

        private readonly Dictionary<ulong, float> serverLastHitTimeByTarget = new Dictionary<ulong, float>(32);

        private bool pendingHitCheck;
        private Vector2 pendingHitDirection;
        private EffectiveAbilityStats pendingHitStats;

        [SerializeField] private float perTargetRehitLockSeconds = 0.25f;


        private Coroutine serverAttackRoutine;

        #endregion

        #region Inspector - Visual References

        [Header("Visual References")]
        [SerializeField] private Animator weaponAnimator;
        [SerializeField] private SpriteRenderer swordRenderer;
        [SerializeField] private Transform vfxSpawnPoint;

        #endregion

        #region Inspector - Hitbox Settings

        [Header("Hitbox Settings")]
        [SerializeField] private float hitRadius = 1.0f;
        [SerializeField] private LayerMask hitLayers;
        [SerializeField, Range(10f, 180f)] private float attackArcDegrees = 110f;
        [SerializeField] private float hitForwardOffset = 0.6f; // shifts hit center forward
        [SerializeField] private float northSouthMultiplier = 1.0f;
        [SerializeField] private float diagonalMultiplier = 1.0f;
        [SerializeField] private float eastWestMultiplier = 1.1f;


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

        private Vector2 lastDebugAttackDir = Vector2.up;

        private void OnDrawGizmosSelected()
        {
            // Choose a preview direction for editor gizmos
            // If you store last attack direction, you can use that instead
            Vector2 dir = lastDebugAttackDir;

            Vector2 origin = (Vector2)transform.position + dir * GetOffsetForDir(dir);

            // ---- Draw hit radius ----
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, hitRadius);

            // ---- Draw attack arc ----
            Gizmos.color = Color.yellow;

            float halfArc = attackArcDegrees * 0.5f;
            int steps = 24; // smoothness of the arc

            Vector3 prevPoint = origin + Rotate(dir, -halfArc) * hitRadius;

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float angle = Mathf.Lerp(-halfArc, halfArc, t);
                Vector3 nextPoint = origin + Rotate(dir, angle) * hitRadius;

                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // ---- Draw center direction line ----
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + dir * hitRadius);
        }

        private Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);

            return new Vector2(
                cos * v.x - sin * v.y,
                sin * v.x + cos * v.y
            );
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

        private float GetEffectiveWindup(EffectiveAbilityStats stats)
        {
            if (stats.windupSeconds > 0f)
                return stats.windupSeconds;

            if (baseAbility != null && baseAbility.windupSeconds > 0f)
                return baseAbility.windupSeconds;

            return 0f; // instant hit fallback
        }

        private float GetEffectiveActiveTime(EffectiveAbilityStats stats)
        {
            if (stats.activeSeconds > 0f)
                return stats.activeSeconds;

            if (baseAbility != null && baseAbility.activeSeconds > 0f)
                return baseAbility.activeSeconds;

            return 0.03f; // tiny default window
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

            double pressedServerTime = NetworkManager.Singleton.ServerTime.Time;
            UseAbilityServerRpc(dir);
            lastDebugAttackDir = dir;
        }

        float GetOffsetForDir(Vector2 dir)
        {
            dir = dir.normalized;

            bool isDiagonal = Mathf.Abs(dir.x) > 0.1f && Mathf.Abs(dir.y) > 0.1f;
            bool isNorthSouth = Mathf.Abs(dir.y) >= Mathf.Abs(dir.x);

            float mult = isDiagonal ? diagonalMultiplier : isNorthSouth ? northSouthMultiplier : eastWestMultiplier;

            return hitForwardOffset * mult;
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

            if (serverAttackRoutine != null)
                StopCoroutine(serverAttackRoutine);

            serverAttackRoutine = StartCoroutine(Server_DoSwordHit(direction, stats));
        }





        private System.Collections.IEnumerator Server_DoSwordHit(Vector2 direction, EffectiveAbilityStats stats)
        {
            // Store the hit data for when the animation event fires
            pendingHitCheck = true;
            pendingHitDirection = direction;
            pendingHitStats = stats;

            // Safety fallback: if animation event doesn't fire within 1 second, auto-execute
            float timeout = 1f;
            float elapsed = 0f;

            while (pendingHitCheck && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Timeout fallback: execute hit if animation event never fired
            if (pendingHitCheck)
            {
                Debug.LogWarning("[Sword] Animation event didn't fire within timeout, executing hit as fallback");
                ExecuteHitCheck();
            }
        }



        private void DoSwordOverlap(Vector2 direction, EffectiveAbilityStats stats)
        {
            float damage = stats.damage > 0f ? stats.damage : (baseAbility != null ? baseAbility.damage : 0f);
            float knockback = stats.knockbackForce > 0f ? stats.knockbackForce : (baseAbility != null ? baseAbility.knockbackForce : 0f);

            Vector2 dir = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.up;

            float offset = GetOffsetForDir(dir);
            Vector2 origin = (Vector2)transform.position + dir * offset;

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, hitRadius, hitLayers);
            float halfArc = attackArcDegrees * 0.5f;

            // Prefer "attacker root" exclusion
            Transform attackerRoot = GetComponentInParent<NetworkObject>()?.transform;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;

                // Don’t hit yourself / your own player root hierarchy
                if (attackerRoot != null)
                {
                    if (hit.transform == attackerRoot || hit.transform.IsChildOf(attackerRoot))
                        continue;
                }
                else
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        continue;
                }

                // --- Arc filter ---
                Vector2 closest = hit.bounds.ClosestPoint(origin);
                Vector2 to = closest - origin;
                if (to.sqrMagnitude < 0.0001f) continue;

                Vector2 toNorm = to.normalized;

                // must be in front
                if (Vector2.Dot(dir, toNorm) <= 0f)
                    continue;

                float angle = Vector2.Angle(dir, toNorm);
                if (angle > halfArc)
                    continue;

                // Resolve target NetworkObject (for the hard guarantee)
                var targetNO = hit.GetComponentInParent<NetworkObject>();
                ulong targetId = targetNO != null ? targetNO.NetworkObjectId : 0;

                // ---- HARD GUARANTEE: per-target re-hit lock (server side) ----
                if (targetId != 0)
                {
                    if (serverLastHitTimeByTarget.TryGetValue(targetId, out float lastHit) &&
                        Time.time - lastHit < perTargetRehitLockSeconds)
                    {
                        continue;
                    }
                    serverLastHitTimeByTarget[targetId] = Time.time;
                }

                // Attacker id should be the player/root NO, not the sword child
                ulong attackerId = NetworkObjectId;
                var attackerNO = GetComponentInParent<NetworkObject>();
                if (attackerNO != null) attackerId = attackerNO.NetworkObjectId;

                var dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null && damage > 0f)
                    dmg.TakeDamage(damage, attackerId);

                var kb = hit.GetComponentInParent<IKnockbackable>();
                if (kb != null && knockback > 0f)
                    kb.ApplyKnockback(toNorm, knockback);

                if (kb == null)
                {
                    Debug.Log($"[Sword] No IKnockbackable found for hit={hit.name} root={hit.transform.root.name}");
                }
                else
                {
                    Debug.Log($"[Sword] Knockbacking {hit.name} dir={toNorm} force={knockback}");
                }
            }

            PlayAttackVfxClientRpc(dir);
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

        /// <summary>
        /// Called by Animation Event on the sword attack animation.
        /// This should be placed on the frame where the sword makes contact.
        /// </summary>
        public void OnSwordHitFrame()
        {
            if (!IsServer) return;
            if (!pendingHitCheck) return;

            ExecuteHitCheck();
        }

        private void ExecuteHitCheck()
        {
            if (!pendingHitCheck) return;

            DoSwordOverlap(pendingHitDirection, pendingHitStats);
            pendingHitCheck = false;
        }
        #endregion
    }
}