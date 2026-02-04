using Combat.AbilitySystem;
using Combat.DamageInterfaces;
using Combat.Projectiles;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static BossAbilitySet;

namespace Enemy
{
    public class BossAbilityController : NetworkBehaviour
    {
        [Header("Boss Configuration")]
        [SerializeField] private BossAbilitySet abilitySet;

        [Header("Settings")]
        [SerializeField] private LayerMask damageableLayers;

        private int currentPhaseIndex = 0;
        private float lastPrimaryAbilityTime = -999f;
        private float lastSecondaryAbilityTime = -999f;

        private int primaryRotationIndex = 0; // For Alternate mode
        private bool useSecondaryNext = false; // For PriorityRotate mode

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

            Debug.Log($"[BossAbility] Switched to phase {phaseIndex}");
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

            Debug.Log($"[BossAbility] Using: {ability.abilityName}");

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
                Debug.LogWarning($"[BossAbility] No handler for ability type: {ability.type}");
            }

            isPerformingAbility = false;
        }

        // Ability-specific implementations
        private IEnumerator GroundPoundSequence(Ability ability)
        {
            Vector2 bossPos = transform.position;
            float radius = ability.aoeRadius;
            float windup = ability.windupDuration;

            ShowTelegraphClientRpc(bossPos, radius, windup);

            yield return new WaitForSeconds(windup);

            DealAoEDamage(bossPos, radius, ability.damage);
            HideTelegraphClientRpc();
            ScreenShakeClientRpc();
        }

        private IEnumerator StoneFistSequence(Ability ability, Transform target)
        {
            if (target == null) yield break;

            Vector2 bossPos = transform.position;
            Vector2 targetPos = target.position;
            float windup = ability.windupDuration;

            // show telegraph at target position
            ShowTelegraphClientRpc(targetPos, ability.aoeRadius, windup);
            yield return new WaitForSeconds(windup);
            // deal damage + knockback in small AoE
            DealAoEDamage(targetPos, ability.aoeRadius, ability.damage);
            HideTelegraphClientRpc();
        }

        private IEnumerator RuneBarrageSequence(Ability ability, Transform target)
        {
            if (target == null) yield break;

            float windup = ability.windupDuration;
            Vector2 origin = transform.position;
            Vector2 toTarget = (Vector2)target.position - origin;
            Vector2 baseDir = toTarget.normalized;

            // Show telegraph lines for each projectile path
            ShowProjectileTelegraphClientRpc(origin, baseDir, ability.projectileCount, ability.spreadAngle, ability.windupDuration);

            yield return new WaitForSeconds(windup);

            HideProjectileTelegraphClientRpc();

            int count = Mathf.Max(1, ability.projectileCount);
            float spread = ability.spreadAngle;

            for (int i = 0; i < count; i++)
            {
                float angle = 0f;
                if (count > 1)
                {
                    float step = spread / (count - 1);
                    angle = -spread / 2f + (i * step);
                }

                Vector2 dir = RotateVector(baseDir, angle);
                SpawnProjectile(ability.projectilePrefab, origin, dir, ability.projectileSpeed);
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

        private void SpawnProjectile(GameObject prefab, Vector2 origin, Vector2 direction, float speed)
        {
            GameObject proj = Instantiate(prefab, origin, Quaternion.identity);

            var netObj = proj.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn(true);

            var projBase = proj.GetComponent<ProjectileBase>();
            if(projBase != null)
            {
                projBase.SetDirection(direction);
                // ProjectileBase uses its own speed field
            }
        }

        private Vector2 RotateVector(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * v.x - Mathf.Sin(rad) * v.y, Mathf.Sin(rad) * v.x + Mathf.Cos(rad) * v.y);
        }


        [ClientRpc]
        private void ShowTelegraphClientRpc(Vector2 position, float radius, float duration)
        {
            // For now, just log - we'll add telegraph prefab reference to BossAbilitySet later
            Debug.Log($"[BossAbility] Showing telegraph at {position}, radius {radius}");

            // TODO: Instantiate telegraph visual
        }

        [ClientRpc]
        private void HideTelegraphClientRpc()
        {
            // Telegraph auto-destroys after duration
        }

        [ClientRpc]
        private void ShowProjectileTelegraphClientRpc(Vector2 origin, Vector2 baseDirection, int count, float spread, float duration)
        {
            // For now, just log - you can add actual LineRenderer visualization later
            Debug.Log($"[BossAbility] Showing {count} projectile telegraphs from {origin}");

            // TODO: Instantiate LineRenderers showing projectile paths
            // Similar to ranged enemy telegraph but multiple lines
        }

        [ClientRpc]
        private void HideProjectileTelegraphClientRpc()
        {
            // TODO: Hide/destroy projectile telegraph lines
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
