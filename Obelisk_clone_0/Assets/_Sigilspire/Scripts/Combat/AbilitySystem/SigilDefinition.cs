using UnityEngine;

namespace Combat.AbilitySystem
{
    public enum SigilType { Major, Minor }
    public enum SigilCompatibility { Universal, SwordOnly, ShieldOnly, GrappleOnly }

    [CreateAssetMenu(fileName = "NewSigil", menuName = "Sigilspire/Sigil Definition")]
    public class SigilDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("Type")]
        public SigilType sigilType = SigilType.Major;

        public WeaponSlot slot;        // Majors
        public SigilCompatibility compatibility = SigilCompatibility.Universal; // Minors
        
        #region Major Sigils
        [Header("Major Sigil - Core")]
        public Ability baseAbility;
        public WeaponVisualSet visualSet;

        [Header("Major Sigil - Progression")]
        public int maxLevel = 10;
        [Tooltip("x = level (1..maxLevel), y = XP required for that level")]
        public AnimationCurve xpRequiredByLevel;

        [Header("Major Sigil - Scaling")]
        [Tooltip("Multipliers applied to baseAbility stats per level. x = level, y = multiplier")]
        public AnimationCurve damageMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 2f);
        [Tooltip("Multipliers applied to baseAbility stats per level. x = level, y = multiplier")]
        public AnimationCurve cooldownMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 0.5f);
        [Tooltip("Multipliers applied to baseAbility stats per level. x = level, y = multiplier")]
        public AnimationCurve knockbackMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 1.5f);
        [Tooltip("Multipliers applied to baseAbility stats per level. x = level, y = multiplier")]
        public AnimationCurve grappleForceMultiplierByLevel = AnimationCurve.Linear(1, 1f, 10, 1.5f);
        #endregion
        
        #region Minor Sigils
        [Header("Minor Sigil - Progression")]
        public int minorMaxLevel = 3;

        [Header("Minor Sigil - Bonuses (Per Level)")]
        [Tooltip("Damage bonus % per level (e.g., 10 = +10% per level)")]
        public float bonusDamagePercent = 0f;
        [Tooltip("Cooldown reduction % per level (e.g., -10 = -10% per level)")]
        public float bonusCooldownPercent = 0f;
        [Tooltip("Range/Radius bonus % per level")]
        public float bonusRangePercent = 0f;
        [Tooltip("Knockback bonus % per level")]
        public float bonusKnockbackPercent = 0f;
        [Tooltip("Lifesteal % per level (e.g., 5 = 5% lifesteal per level)")]
        public float bonusLifestealPercent = 0f;
        [Tooltip("Max Health flat bonus per level")]
        public float bonusMaxHealthFlat = 0f;
        [Tooltip("Max Shield flat bonus per level")]
        public float bonusMaxShieldFlat = 0f;
        #endregion

        #region Methods
        public float GetXpRequiredForLevel(int level)
        {
            if (sigilType == SigilType.Minor) return 0f;

            if (xpRequiredByLevel == null) return 100f * level; 
            return xpRequiredByLevel.Evaluate(level);
        }

        public int GetMaxLevel()
        {
            return sigilType == SigilType.Minor ? minorMaxLevel : maxLevel;
        }

        public float GetDamageMultiplier(int level) => damageMultiplierByLevel.Evaluate(level);
        public float GetCooldownMultiplier(int level) => cooldownMultiplierByLevel.Evaluate(level);
        public float GetKnockbackMultiplier(int level) => knockbackMultiplierByLevel.Evaluate(level);
        public float GetGrappleForceMultiplier(int level) => grappleForceMultiplierByLevel.Evaluate(level);
        #endregion
    }
}

