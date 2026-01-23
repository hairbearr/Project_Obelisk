namespace Combat.AbilitySystem
{
    /// <summary>
    /// Runtime effective stats for an ability, after applying sigil scaling.
    /// </summary>
    public struct EffectiveAbilityStats
    {
        public float damage;
        public float cooldown;
        public float knockbackForce;
        public float grappleForce;
        public float windupSeconds;
        public float activeSeconds;
        // You can add more as you expand Ability later.
    }
}
