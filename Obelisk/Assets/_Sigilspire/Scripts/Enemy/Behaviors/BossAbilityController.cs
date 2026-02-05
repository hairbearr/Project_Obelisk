using Combat.AbilitySystem;
using Combat.DamageInterfaces;
using Combat.Projectiles;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static BossAbilitySet;
using static UnityEngine.GraphicsBuffer;

namespace Enemy
{
    public class BossAbilityController : NetworkBehaviour
    {
        [Header("Boss Configuration")]
        [SerializeField] private BossAbilitySet abilitySet;

        [Header("Settings")]
        [SerializeField] private LayerMask damageableLayers;

        [Header("Telegraph Prefabs")]
        [SerializeField] private GameObject circleTelegraphPrefab;
        [SerializeField] private GameObject coneTelegraphPrefab;
        [SerializeField] private GameObject lineTelegraphPrefab;

        public enum TelegraphType { Circle, Cone, Line }

        private int currentPhaseIndex = 0;
        private float lastPrimaryAbilityTime = -999f;
        private float lastSecondaryAbilityTime = -999f;

        private int primaryRotationIndex = 0; // For Alternate mode
        private bool useSecondaryNext = false; // For PriorityRotate mode

        private GameObject activeTelegraph; // Track current telegraph

        private bool isPerformingAbility = false;

        public bool IsPerformingAbility => isPerformingAbility;

        public void SetPhase(int phaseIndex)
        {
            currentPhaseIndex = phaseIndex;

            // Reset rotation state on phase change
            primaryRotationIndex = 0;
            useSecondaryNext = false;
            lastPrimaryAbilityTime = -999f;
            lastSecondaryAbilityTime = -999f;

        }

        public void TryUseAbility(Transform target)
        {
            if (!IsServer) return;
            if (isPerformingAbility) return;
            if (abilitySet == null) return;

            var phase = abilitySet.GetPhaseAbilities(currentPhaseIndex);
            if (phase == null) return;

            Ability chosenAbility = null;

            switch (phase.rotationMode)
            {
                case RotationMode.Alternate:
                    chosenAbility = HandleAlternateRotation(phase);
                    break;

                case RotationMode.PriorityRotate:
                    chosenAbility = HandlePriorityRotation(phase);
                    break;

                case RotationMode.Random:
                    chosenAbility = HandleRandomRotation(phase);
                    break;
            }

            if (chosenAbility != null)
            {
                StartCoroutine(UseAbilityCoroutine(chosenAbility, target));
            }
        }

        private Ability HandleAlternateRotation(BossAbilitySet.PhaseAbilities phase)
        {
            // A -> B -> A -> B pattern
            if (Time.time - lastPrimaryAbilityTime < phase.abilityCooldown)
                return null;

            if (phase.primaryAbilities.Count == 0)
                return null;

            // Cycle through abilities
            Ability ability = phase.primaryAbilities[primaryRotationIndex];
            primaryRotationIndex = (primaryRotationIndex + 1) % phase.primaryAbilities.Count;
            lastPrimaryAbilityTime = Time.time;

            return ability;
        }

        private Ability HandlePriorityRotation(BossAbilitySet.PhaseAbilities phase)
        {
            // (A or B) -> C -> (A or B) -> C pattern

            if (useSecondaryNext)
            {
                // Time for secondary (C)
                if (Time.time - lastSecondaryAbilityTime < phase.secondaryAbilityCooldown)
                    return null;

                if (phase.secondaryAbilities.Count == 0)
                {
                    useSecondaryNext = false; // Skip if no secondary
                    return null;
                }

                Ability ability = phase.secondaryAbilities[Random.Range(0, phase.secondaryAbilities.Count)];
                lastSecondaryAbilityTime = Time.time;
                useSecondaryNext = false;

                return ability;
            }
            else
            {
                // Time for primary (A or B)
                if (Time.time - lastPrimaryAbilityTime < phase.abilityCooldown)
                    return null;

                if (phase.primaryAbilities.Count == 0)
                    return null;

                Ability ability = phase.primaryAbilities[Random.Range(0, phase.primaryAbilities.Count)];
                lastPrimaryAbilityTime = Time.time;
                useSecondaryNext = true;

                return ability;
            }
        }

        private Ability HandleRandomRotation(BossAbilitySet.PhaseAbilities phase)
        {
            // Any ability, any time (chaos mode)
            if (Time.time - lastPrimaryAbilityTime < phase.abilityCooldown)
                return null;

            // Combine all abilities into one pool
            List<Ability> allAbilities = new List<Ability>();
            allAbilities.AddRange(phase.primaryAbilities);
            allAbilities.AddRange(phase.secondaryAbilities);

            if (allAbilities.Count == 0)
                return null;

            Ability ability = allAbilities[Random.Range(0, allAbilities.Count)];
            lastPrimaryAbilityTime = Time.time;

            return ability;
        }

        private IEnumerator UseAbilityCoroutine(Ability ability, Transform target)
        {
            isPerformingAbility = true;


            // Lock boss movement during ability
            var enemyAI = GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                enemyAI.NotifyKnockback(); // Reuses the movement lock mechanism
            }

            // Route to appropriate ability handler based on type
            if (ability.aoeRadius > 0f)
            {
                yield return StartCoroutine(GroundPoundSequence(ability));
            }
            else if (ability.damage > 0f && ability.knockbackForce > 0f)
            {
                yield return StartCoroutine(StoneFistSequence(ability, target));
            }
            else if (ability.projectilePrefab != null)
            {
                yield return StartCoroutine(RuneBarrageSequence(ability, target));
            }
            // TODO: Add other ability types (projectile, summon, etc.)
            else
            {
            }

            isPerformingAbility = false;
        }

        // Ability-specific implementations
        private IEnumerator GroundPoundSequence(Ability ability)
        {
            Vector2 bossPos = transform.position;

            // Show circle telegraph (direction doesn't matter)
            ShowTelegraphClientRpc(bossPos, Vector2.zero, TelegraphType.Circle, ability.aoeRadius, ability.windupDuration);

            yield return new WaitForSeconds(ability.windupDuration);

            // Deal circular AoE damage
            DealAoEDamage(bossPos, ability.aoeRadius, ability.damage);

            ScreenShakeClientRpc();
        }

        private IEnumerator StoneFistSequence(Ability ability, Transform target)
        {
            Vector2 bossPos = transform.position;
            Vector2 toTarget = ((Vector2)target.position - bossPos).normalized;

            // Show cone telegraph facing the target
            ShowTelegraphClientRpc(bossPos, toTarget, TelegraphType.Cone, ability.aoeRadius, ability.windupDuration);

            yield return new WaitForSeconds(ability.windupDuration);

            // Deal cone damage (90 degree arc)
            DealConeAoeDamage(bossPos, toTarget, ability.aoeRadius, 90f, ability.damage);

            ScreenShakeClientRpc();
        }

        private IEnumerator RuneBarrageSequence(Ability ability, Transform target)
        {
            Vector2 bossPos = transform.position;
            Vector2 toTarget = ((Vector2)target.position - bossPos).normalized;

            // Fire 3 projectiles in a spread pattern (-15, 0, +15 degrees)
            float[] spreadAngles = { -15f, 0f, 15f };
            float projectileRange = 10f; // Visual range for telegraph
            float lineWidth = 0.2f;

            // Show line telegraphs for each projectile path
            foreach (float angleOffset in spreadAngles)
            {
                Vector2 direction = RotateVector(toTarget, angleOffset);
                Vector2 endPoint = bossPos + direction * projectileRange;

                ShowLineTelegraphClientRpc(bossPos, endPoint, lineWidth, ability.windupDuration);
            }

            yield return new WaitForSeconds(ability.windupDuration);

            // Fire the actual projectiles
            foreach (float angleOffset in spreadAngles)
            {
                Vector2 direction = RotateVector(toTarget, angleOffset);
                SpawnProjectile(bossPos, direction, ability);
            }
        }

        // Helper methods
        private void DealAoEDamage(Vector2 center, float radius, float damage)
        {
            if (!IsServer) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, damageableLayers);

            var bossNetworkObject = GetComponentInParent<NetworkObject>();
            ulong bossId = bossNetworkObject != null ? bossNetworkObject.NetworkObjectId : 0;

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damage, bossId);

                var knockbackable = hit.GetComponentInParent<IKnockbackable>();
                if (knockbackable != null)
                {
                    Vector2 direction = ((Vector2)hit.transform.position - center).normalized;
                    knockbackable.ApplyKnockback(direction, 10f);
                }
            }
        }

        private void DealConeAoeDamage(Vector2 center, Vector2 direction, float radius, float arcDegrees, float damage)
        {
            if (!IsServer) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, damageableLayers);

            var bossNetworkObject = GetComponentInParent<NetworkObject>();
            ulong bossId = bossNetworkObject != null ? bossNetworkObject.NetworkObjectId : 0;

            float halfArc = arcDegrees * 0.5f;

            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                // Arc filter
                Vector2 toTarget = ((Vector2)hit.transform.position) - center;
                if (toTarget.sqrMagnitude < 0.0001f) continue;

                Vector2 toTargetNorm = toTarget.normalized;

                // Must be in front (within 180 degrees)
                if (Vector2.Dot(direction, toTargetNorm) <= 0f)
                    continue;

                // Must be within arc
                float angle = Vector2.Angle(direction, toTargetNorm);
                if (angle > halfArc)
                    continue;

                // Damage + knockback
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(damage, bossId);

                var knockbackable = hit.GetComponentInParent<IKnockbackable>();
                if (knockbackable != null)
                {
                    knockbackable.ApplyKnockback(toTargetNorm, 10f);
                }
            }
        }

        private void SpawnProjectile(Vector2 startPos, Vector2 direction, Ability ability)
        {
            if (!IsServer) return;
            if (ability.projectilePrefab == null)
            {
                Debug.LogWarning("[BossAbility] No projectilePrefab set on ability!");
                return;
            }

            // Instantiate projectile
            GameObject projObj = Instantiate(ability.projectilePrefab, startPos, Quaternion.identity);

            // Configure it before spawning
            var projectile = projObj.GetComponent<ProjectileBase>();
            if (projectile != null)
            {
                projectile.SetDirection(direction);
                // Override damage from ability (projectile prefab has default)
                projectile.damage = ability.damage;
            }

            // Network spawn
            var networkObject = projObj.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
            else
            {
                Debug.LogWarning("[BossAbility] Projectile prefab missing NetworkObject component!");
                Destroy(projObj);
            }
        }

        private Vector2 RotateVector(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                cos * vector.x - sin * vector.y,
                sin * vector.x + cos * vector.y
            );
        }


        [ClientRpc]
        private void ShowTelegraphClientRpc(Vector2 position, Vector2 direction, TelegraphType type, float radius, float duration)
        {
            GameObject prefab = type switch
            {
                TelegraphType.Circle => circleTelegraphPrefab,
                TelegraphType.Cone => coneTelegraphPrefab,
                TelegraphType.Line => lineTelegraphPrefab,
                _ => null
            };

            if (prefab == null)
            {
                Debug.LogWarning($"[BossAbility] No telegraph prefab for type {type}!");
                return;
            }

            GameObject instance = Instantiate(prefab, position, Quaternion.identity);

            // Rotate to face direction (matters for cones, doesn't hurt circles)
            if (direction.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                instance.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            }

            // Scale to match radius
            instance.transform.localScale = Vector3.one * radius;

            Destroy(instance, duration);
        }

        [ClientRpc]
        private void ShowLineTelegraphClientRpc(Vector2 startPoint, Vector2 endPoint, float width, float duration)
        {
            if (lineTelegraphPrefab == null) return;

            GameObject instance = Instantiate(lineTelegraphPrefab, startPoint, Quaternion.identity);

            // Get both line renderers
            LineRenderer[] lines = instance.GetComponents<LineRenderer>();

            if (lines.Length >= 2)
            {
                LineRenderer backgroundLine = lines[0];
                LineRenderer foregroundLine = lines[1];

                // Background: show full path instantly (semi-transparent)
                backgroundLine.positionCount = 2;
                backgroundLine.SetPosition(0, new Vector3(startPoint.x, startPoint.y, 0));
                backgroundLine.SetPosition(1, new Vector3(endPoint.x, endPoint.y, 0));
                backgroundLine.startWidth = width;
                backgroundLine.endWidth = width;

                // Foreground: animate fill (opaque)
                foregroundLine.startWidth = width;
                foregroundLine.endWidth = width;
                StartCoroutine(AnimateLineFill(foregroundLine, startPoint, endPoint, duration));
            }
            else
            {
                Debug.LogWarning("[BossAbility] Line telegraph prefab needs 2 LineRenderer components!");
            }

            Destroy(instance, duration);
        }

        private IEnumerator AnimateLineFill(LineRenderer line, Vector2 start, Vector2 end, float fillTime)
        {
            line.positionCount = 2;
            line.SetPosition(0, new Vector3(start.x, start.y, 0));
            line.SetPosition(1, new Vector3(start.x, start.y, 0)); // Start collapsed

            float elapsed = 0f;

            while (elapsed < fillTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fillTime;

                Vector2 currentEnd = Vector2.Lerp(start, end, t);
                line.SetPosition(1, new Vector3(currentEnd.x, currentEnd.y, 0));

                yield return null;
            }

            // Snap to final position
            line.SetPosition(1, new Vector3(end.x, end.y, 0));
        }

        [ClientRpc]
        private void ScreenShakeClientRpc()
        {
            var shake = FindFirstObjectByType<CameraShake>();
            if (shake != null)
                shake.Shake(0.3f, 0.3f);
        }

        private void OnDrawGizmosSelected()
        {
            if (abilitySet == null) return;

            Vector3 pos = transform.position;
            var phase = abilitySet.GetPhaseAbilities(currentPhaseIndex);
            if (phase == null) return;

            // Draw primary abilities (A/B pool)
            foreach (var ability in phase.primaryAbilities)
            {
                if (ability == null) continue;

                // AoE abilities (Ground Pound, Stone Fist)
                if (ability.aoeRadius > 0f)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // red transparent
                    Gizmos.DrawWireSphere(pos, ability.aoeRadius);

                    // Label
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(pos + Vector3.up * ability.aoeRadius, ability.abilityName + " AoE");
#endif
                }

                // Projectile abilities (Rune Barrage)
                if (ability.projectilePrefab != null)
                {
                    float range = 10f; // estimate based on projectileSpeed * lifetime
                    Gizmos.color = new Color(0f, 1f, 1f, 0.3f); // cyan transparent
                    Gizmos.DrawWireSphere(pos, range);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(pos + Vector3.up * range, ability.abilityName + " Range");
#endif
                }
            }

            // Draw secondary abilities (C pool) - different color
            foreach (var ability in phase.secondaryAbilities)
            {
                if (ability == null) continue;

                if (ability.aoeRadius > 0f)
                {
                    Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // magenta transparent
                    Gizmos.DrawWireSphere(pos, ability.aoeRadius);

#if UNITY_EDITOR
                    UnityEditor.Handles.Label(pos + Vector3.up * ability.aoeRadius, ability.abilityName + " (Secondary)");
#endif
                }
            }

            // Draw current phase info
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 3f, $"Phase {currentPhaseIndex + 1} - {phase.rotationMode}");
#endif
        }
    }
}
