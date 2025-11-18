using UnityEngine;

namespace Combat.AbilitySystem
{
    /// <summary>
    /// Design-time definition of a sigil.
    /// Shared across all players and runs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSigil", menuName = "Sigilspire/Sigil Definition")]
    public class SigilDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;              // e.g. "sword_fire_slash"
        public string displayName;
        [TextArea]
        public string description;

        [Header("Usage")]
        public WeaponSlot slot;        // Sword / Shield / Grapple
        public Ability baseAbility;    // Ability this sigil is built around

        [Header("Progression")]
        public int maxLevel = 10;

        // Very simple XP model for now; you can replace with curves later.
        public AnimationCurve xpRequiredByLevel;
        // x = level (1..maxLevel), y = XP required for that level

        [Header("Scaling")]
        // Multipliers applied to baseAbility stats per level.
        // x = level, y = multiplier.
        public AnimationCurve damageMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 2f);
        public AnimationCurve cooldownMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 0.5f);
        public AnimationCurve knockbackMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 1.5f);
        public AnimationCurve grappleForceMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 1.5f);

        public WeaponVisualSet visualSet;

        public float GetXpRequiredForLevel(int level)
        {
            if (xpRequiredByLevel == null) return 100f * level; // TODO: better default
            return xpRequiredByLevel.Evaluate(level);
        }

        public float GetDamageMultiplier(int level) => damageMultiplierByLevel.Evaluate(level);
        public float GetCooldownMultiplier(int level) => cooldownMultiplierByLevel.Evaluate(level);
        public float GetKnockbackMultiplier(int level) => knockbackMultiplierByLevel.Evaluate(level);
        public float GetGrappleForceMultiplier(int level) => grappleForceMultiplierByLevel.Evaluate(level);
    }
}

