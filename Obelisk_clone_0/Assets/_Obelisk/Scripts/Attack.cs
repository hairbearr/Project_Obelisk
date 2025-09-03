using UnityEngine;

namespace Sigilspire.Combat
{
    /// <summary>
    /// Central ScriptableObject for attacks and abilities.
    /// Stores animation references, stats, cooldowns, and upgrade paths.
    /// Used by both Player and Enemy controllers with Netcode for GameObjects.
    /// </summary>
    [CreateAssetMenu(menuName = "Sigilspire/Combat/Attack")]
    public class Attack : ScriptableObject
    {
        [Header("Attack Info")]
        [Tooltip("Name of this attack, used for debugging/UI.")]
        public string attackName;

        [Tooltip("Damage dealt by this attack.")]
        public float damage;

        [Tooltip("Cooldown in seconds before this attack can be used again.")]
        public float cooldown;

        [Tooltip("Knockback force applied to the target.")]
        public float knockback;

        [Tooltip("Weight of the attack (used for stagger, poise, or weighted random enemy selection).")]
        public float weight;

        [Tooltip("If true, this attack is a special ability.")]
        public bool isSpecial;

        [Tooltip("If true, this attack can chain into another attack.")]
        public bool canChainCombo;

        [Tooltip("Prefab for any projectile this attack spawns (optional).")]
        public GameObject projectilePrefab;

        [Tooltip("Radius of this attack. If >0, attack hits all players within this radius.")]
        public float attackRadius = 0f;

        [Header("Shield Stats")]
        [Tooltip("Maximum energy the shield can hold.")]
        public float shieldMaxEnergy = 50f;

        [Tooltip("Energy regenerated per second when not blocking or stunned.")]
        public float shieldRegenRate = 5f;

        [Tooltip("Time the player is stunned when the shield breaks.")]
        public float shieldBreakStunDuration = 1.5f;

        [Tooltip("Percentage of knockback reduced while blocking.")]
        [Range(0f, 1f)]
        public float shieldKnockBackReduction = 0.5f;

        [Header("Grappling Hook Stats")]
        [Tooltip("Speed at which the grappling hook retracts.")]
        public float grapplingHookPullSpeed = 10f;

        [Tooltip("Amount of damage dealt to enemies when hit.")]
        public float grapplingHookDamage = 10f;

        [Tooltip("Maximum distance grappling hook flies.")]
        public float grapplingHookMaxDistance = 5f;

        [Tooltip("LayerMask used to grapple.")]
        public LayerMask grapplingHookLayerMask;

        [Header("Animation Clips")]
        [Tooltip("Animations for directional actions (attack, block, grapple, etc.).")]
        public DirectionalAnimations animations;

        [Header("Upgrade System")]
        [Tooltip("Reference to the upgraded version of this attack (if any).")]
        public Attack upgradedAttack;

        // Tracks the last time this attack was used
        [HideInInspector] public float lastUsedTime;

        // -------------------------------
        // Cooldown / Combo / Weight Logic
        // -------------------------------

        /// <summary>
        /// Returns true if this attack is ready to be used (cooldown elapsed).
        /// </summary>
        public bool IsReady()
        {
            return Time.time >= lastUsedTime + cooldown;
        }

        /// <summary>
        /// Gets the attack name.
        /// </summary>
        public string AttackName => attackName;

        /// <summary>
        /// Gets the attack cooldown.
        /// </summary>
        public float Cooldown => cooldown;

        /// <summary>
        /// Gets the attack weight (useful for enemies).
        /// </summary>
        public float Weight => weight;

        /// <summary>
        /// Gets whether this attack can chain into another.
        /// </summary>
        public bool CanChainCombo => canChainCombo;

        // -------------------------------
        // Animation Accessors
        // -------------------------------

        public AnimationClip GetAttackAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Attack, dir);

        public AnimationClip GetBlockAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Block, dir);

        public AnimationClip GetGrappleAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Grapple, dir);

        public AnimationClip GetRunAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Run, dir);

        public AnimationClip GetClimbAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Climb, dir);

        public AnimationClip GetPotionAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Potion, dir);

        public AnimationClip GetInteractAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Interact, dir);

        public AnimationClip GetJumpAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Jump, dir);

        public AnimationClip GetShootAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Shoot, dir);

        public AnimationClip GetUseItemAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.UseItem, dir);

        public AnimationClip GetDeathAnimation(Direction dir) =>
            animations.GetAnimation(ActionType.Death, dir);

        // -------------------------------
        // Upgrade Handling
        // -------------------------------

        /// <summary>
        /// Returns true if this attack has an upgrade path.
        /// </summary>
        public bool HasUpgrade() => upgradedAttack != null;

        /// <summary>
        /// Gets the upgraded version of this attack.
        /// </summary>
        public Attack GetUpgradedAttack() => upgradedAttack;
    }

    /// <summary>
    /// Defines the type of action (attack, block, run, etc.).
    /// Used to organize animations.
    /// </summary>
    public enum ActionType
    {
        Attack,
        Block,
        Grapple,
        Run,
        Climb,
        Potion,
        Interact,
        Jump,
        Shoot,
        UseItem,
        Death
    }

    /// <summary>
    /// Holds AnimationClips for each ActionType & Direction.
    /// </summary>
    [System.Serializable]
    public class DirectionalAnimations
    {
        [Tooltip("Animations grouped by action type.")]
        public ActionAnimations[] actionAnimations;

        /// <summary>
        /// Retrieve animation for a given action type and direction.
        /// </summary>
        public AnimationClip GetAnimation(ActionType type, Direction dir)
        {
            foreach (var action in actionAnimations)
            {
                if (action.actionType == type)
                {
                    return action.GetDirectionalClip(dir);
                }
            }

            Debug.LogWarning($"No animation found for {type} in direction {dir}");
            return null;
        }
    }

    /// <summary>
    /// Holds directional animations for a specific action type.
    /// </summary>
    [System.Serializable]
    public class ActionAnimations
    {
        [Tooltip("The type of action these animations represent.")]
        public ActionType actionType;

        [Tooltip("Animations for each of the 8 directions.")]
        public AnimationClip north;
        public AnimationClip northEast;
        public AnimationClip east;
        public AnimationClip southEast;
        public AnimationClip south;
        public AnimationClip southWest;
        public AnimationClip west;
        public AnimationClip northWest;

        /// <summary>
        /// Returns the animation for the given direction.
        /// </summary>
        public AnimationClip GetDirectionalClip(Direction dir)
        {
            return dir switch
            {
                Direction.North => north,
                Direction.NorthEast => northEast,
                Direction.East => east,
                Direction.SouthEast => southEast,
                Direction.South => south,
                Direction.SouthWest => southWest,
                Direction.West => west,
                Direction.NorthWest => northWest,
                _ => null
            };
        }
    }
}

