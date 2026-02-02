using Combat.AbilitySystem;
using Combat.DamageInterfaces;
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

            // Route to appropriate ability handler based on type
            if (ability.aoeRadius > 0f)
            {
                yield return StartCoroutine(GroundPoundSequence(ability));
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
        private void ScreenShakeClientRpc()
        {
            var shake = FindFirstObjectByType<CameraShake>();
            if (shake != null)
                shake.Shake(0.3f, 0.3f);
        }
    }
}
