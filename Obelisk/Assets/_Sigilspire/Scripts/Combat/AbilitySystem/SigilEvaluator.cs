using System.Collections.Generic;
using UnityEngine;

namespace Combat.AbilitySystem
{
    public static class SigilEvaluator
    {
        public static EffectiveAbilityStats GetEffectiveStats(
            Ability baseAbility,
            SigilDefinition major,
            SigilProgressData majorProgress,
            List<SigilDefinition> minors,
            SigilInventory inventory
            )
        {
            // Fallback: no sigil or no progress = base stats.
            if (baseAbility == null)
            {
                return default;
            }

            // base ability stats
            float damage = baseAbility.damage;
            float cooldown = baseAbility.cooldown;
            float knockback = baseAbility.knockbackForce;
            float grappleForce = baseAbility.grappleForce;
            float windupSeconds = baseAbility.windupSeconds;
            float activeSeconds = baseAbility.activeSeconds;

            #region Majors
            if (major != null && major.sigilType == SigilType.Major && majorProgress != null)
            {
                int level = Mathf.Clamp(majorProgress.level, 1, major.maxLevel);

                damage *= major.GetDamageMultiplier(level);
                cooldown *= major.GetCooldownMultiplier(level);
                knockback *= major.GetKnockbackMultiplier(level);
                grappleForce *= major.GetGrappleForceMultiplier(level);
            }
            #endregion

            #region Minors
            if (minors != null && minors.Count > 0 && inventory != null)
            {
                float damageBonus = 0f;          // % bonus
                float cooldownBonus = 0f;        // % bonus (can be negative for reduction)
                float rangeBonus = 0f;           // % bonus (not used yet)
                float knockbackBonus = 0f;       // % bonus
                float lifestealBonus = 0f;       // % bonus (not implemented yet)
                float maxHealthBonus = 0f;       // flat bonus (not implemented yet)
                float maxShieldBonus = 0f;       // flat bonus (not implemented yet)

                foreach (var minor in minors)
                {
                    if (minor == null || minor.sigilType != SigilType.Minor) continue;

                    var minorProgress = inventory.GetOrCreateProgress(minor.id);
                    int minorLevel = Mathf.Clamp(minorProgress.level, 1, minor.minorMaxLevel);

                    damageBonus += minor.bonusDamagePercent * minorLevel;
                    cooldownBonus += minor.bonusCooldownPercent * minorLevel;
                    rangeBonus += minor.bonusRangePercent * minorLevel;
                    knockbackBonus += minor.bonusKnockbackPercent * minorLevel;
                    lifestealBonus += minor.bonusLifestealPercent * minorLevel;
                    maxHealthBonus += minor.bonusMaxHealthFlat * minorLevel;
                    maxShieldBonus += minor.bonusMaxShieldFlat * minorLevel;
                }

                damage *= (1f + damageBonus / 100f);
                cooldown *= (1f + cooldownBonus / 100f);
                knockback *= (1f + knockbackBonus / 100f);

                // Range, lifesteal, health, shield bonuses need to be exposed in EffectiveAbilityStats for when they go in
            }
            #endregion

            return new EffectiveAbilityStats
            {
                damage = damage,
                cooldown = cooldown,
                knockbackForce = knockback,
                grappleForce = grappleForce,
                windupSeconds = windupSeconds,
                activeSeconds = activeSeconds
            };
        }
    }
}

