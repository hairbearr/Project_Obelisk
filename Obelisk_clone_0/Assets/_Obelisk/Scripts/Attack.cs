using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Combat/Attack")]
public class Attack : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string attackName;
    [SerializeField] private bool isRanged;
    [SerializeField] private bool isSpecialAttack;
    [SerializeField] private bool canChainCombo;

    [Header("Animations (8 Directions)")]
    [SerializeField] private AnimationClip[] attackAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] blockAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] grappleAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] runAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] climbAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] potionAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] interactAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] jumpAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] shootAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] useItemAnimations = new AnimationClip[8];
    [SerializeField] private AnimationClip[] deathAnimations = new AnimationClip[8];

    [Header("Combat Stats")]
    [SerializeField] private float baseDamage;
    [SerializeField] private float damageModifier;
    [SerializeField] private float knockbackForce;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float weight = 1f;

    [Header("Projectile / Grapple")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float pullSpeed = 5f;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private Sprite hookSprite;

    [HideInInspector] public float lastUsedTime = -Mathf.Infinity;

    // --- Accessors ---
    public string AttackName => attackName;
    public bool IsRanged => isRanged;
    public bool IsSpecialAttack => isSpecialAttack;
    public bool CanChainCombo => canChainCombo;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float BaseDamage => baseDamage;
    public float DamageModifier => damageModifier;
    public float KnockbackForce => knockbackForce;
    public float Cooldown => cooldown;
    public float Weight => weight;
    public float PullSpeed => pullSpeed;
    public float MaxRange => maxRange;
    public Sprite HookSprite => hookSprite;

    // --- Animation Accessors ---
    public AnimationClip GetAttackAnimation(Direction dir) => attackAnimations[(int)dir];
    public AnimationClip GetBlockAnimation(Direction dir) => blockAnimations[(int)dir];
    public AnimationClip GetGrappleAnimation(Direction dir) => grappleAnimations[(int)dir];
    public AnimationClip GetRunAnimation(Direction dir) => runAnimations[(int)dir];
    public AnimationClip GetClimbAnimation(Direction dir) => climbAnimations[(int)dir];
    public AnimationClip GetPotionAnimation(Direction dir) => potionAnimations[(int)dir];
    public AnimationClip GetInteractAnimation(Direction dir) => interactAnimations[(int)dir];
    public AnimationClip GetJumpAnimation(Direction dir) => jumpAnimations[(int)dir];
    public AnimationClip GetShootAnimation(Direction dir) => shootAnimations[(int)dir];
    public AnimationClip GetUseItemAnimation(Direction dir) => useItemAnimations[(int)dir];
    public AnimationClip GetDeathAnimation(Direction dir) => deathAnimations[(int)dir];

    // --- Combat Helpers ---
    public float DealDamage() => baseDamage + damageModifier;
    public bool IsReady() => Time.time >= lastUsedTime + cooldown;

    public virtual void ApplyEffect(GameObject target)
    {
        if (target == null) return;
        Debug.Log($"{attackName} effect applied to {target.name}");
        // Effects like burn, stun, slow can be implemented here
    }
}
