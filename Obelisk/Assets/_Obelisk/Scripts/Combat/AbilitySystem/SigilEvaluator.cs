namespace Combat.AbilitySystem
{
    /// <summary>
    /// Static helper to compute effective ability stats
    /// from a base Ability + SigilDefinition + SigilProgressData.
    /// </summary>
    public static class SigilEvaluator
    {
        public static EffectiveAbilityStats GetEffectiveStats(
            Ability baseAbility,
            SigilDefinition sigil,
            SigilProgressData progress)
        {
            // Fallback: no sigil or no progress = base stats.
            if (baseAbility == null)
            {
                return default;
            }

            if (sigil == null || progress == null)
            {
                return new EffectiveAbilityStats
                {
                    damage = baseAbility.damage,
                    cooldown = baseAbility.cooldown,
                    knockbackForce = baseAbility.knockbackForce,
                    grappleForce = baseAbility.grappleForce
                };
            }

            int level = progress.level;
            if (level < 1) level = 1;
            if (level > sigil.maxLevel) level = sigil.maxLevel;

            float dmgMult = sigil.GetDamageMultiplier(level);
            float cdMult = sigil.GetCooldownMultiplier(level);
            float kbMult = sigil.GetKnockbackMultiplier(level);
            float gfMult = sigil.GetGrappleForceMultiplier(level);

            return new EffectiveAbilityStats
            {
                damage = baseAbility.damage * dmgMult,
                cooldown = baseAbility.cooldown * cdMult,
                knockbackForce = baseAbility.knockbackForce * kbMult,
                grappleForce = baseAbility.grappleForce * gfMult
            };
        }
    }
}

