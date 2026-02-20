using System.Security.Cryptography.X509Certificates;
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
        Projectile,
        Channel
    }

    public enum AttackAnimationType
    {
        Slash = 1,
        Slam = 2,
        Spin = 3,
        Cast = 4,
        Shoot = 5,
        Special = 6
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

        [Header("Animation")]
        public AttackAnimationType animationType = AttackAnimationType.Slash;

        [Header("Attack Timing")]
        public float windupSeconds = 0.12f;
        public float activeSeconds = 0.05f;

        [Header("Boss/Advanced")]
        public float windupDuration = 1.0f;
        public float aoeRadius = 3f;
        public GameObject telegraphPrefab;

        [Header("Projectile Settings")]
        public int projectileCount = 3;         // How many lines/directions
        public float spreadAngle = 30f;         // total arc in degrees
        public GameObject projectilePrefab;     // projectile's prefab
        public float projectileSpeed = 10f;     // rate at which the projectile travels
        public int shotsPerProjectile = 1;      // Shots per line (barrage density)
        public float shotInterval = 0.1f;       // Delay between shots
        public int volleyCount = 1;             // How many times to retarget
        public float volleyInterval = 1.2f;     // Time between volleys
        public float projectileRange = 10f;     // How far projectiles travel        

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
        public float minActivationRange = 0f; // 0  no restriction, >0 = must be this close

        [Header("Shape")]
        public AbilityShape shape = AbilityShape.Circle;
    }
}