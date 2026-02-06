using UnityEngine;

namespace Combat.AbilitySystem
{
    public enum AbilityType
    {
        Damage,
        Knockback,
        ShieldBlock,
        Grapple,
        Utility
    }

    public enum AbilityDirection
    {
        None,
        Cardinal,
        EightWay,
        AllAround
    }

    public enum AbilityShape
    {
        Circle,
        Cone,
        Projectile
    }

    [CreateAssetMenu(fileName = "NewAbility", menuName = "Sigilspire/Ability")]
    public class Ability : ScriptableObject
    {
        [Header("Basic Info")]
        public string abilityName;
        public AbilityType type;

        [Header("Usage")]
        public float cooldown = 0.25f;
        public float resourceCost = 0f;

        [Header("Direction")]
        public AbilityDirection direction = AbilityDirection.EightWay;

        [Header("Effects")]
        public float damage = 0f;
        public float knockbackForce = 0f;        // Sword
        public float shieldEnergyModifier = 0f;  // Shield
        public float grappleForce = 0f;          // Grapple

        [Header("Visuals")]
        public GameObject vfxPrefab;

        [Header("Attack Timing")]
        public float windupSeconds = 0.12f;
        public float activeSeconds = 0.05f;

        [Header("Boss/Advanced")]
        public float windupDuration = 1.0f;
        public float aoeRadius = 3f;
        public GameObject telegraphPrefab;

        [Header("Multi-Projectile")]
        public int projectileCount = 3;
        public float spreadAngle = 30f;
        public GameObject projectilePrefab;
        public float projectileSpeed = 10f;

        [Header("Summon")]
        public GameObject summonPrefab;
        public int summonCount = 1;
        public float summonHPMultiplier = 0.5f;

        [Header("Charge")]
        public float chargeSpeed = 15f;
        public bool isOneShot = false;

        [Header("Channel")]
        public float channelDuration = 10f;
        public bool killAllOnComplete = false;

        [Header("Shape")]
        public AbilityShape shape = AbilityShape.Circle;
    }
}