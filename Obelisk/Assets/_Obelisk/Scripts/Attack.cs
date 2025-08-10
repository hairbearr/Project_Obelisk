using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "Combat/Attack")]
public class Attack : ScriptableObject
{
    [SerializeField] private string attackName;
    [SerializeField] private bool isRanged;
    [SerializeField] private AnimationClip[] directionalAnimations = new AnimationClip[8];
    [SerializeField] private float baseDamage;
    [SerializeField] private float damageModifier;
    [SerializeField] private float knockbackForce;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private float weight = 1f;
    [SerializeField] private GameObject projectilePrefab;

    [HideInInspector] public float lastUsedTime = -Mathf.Infinity;

    public string AttackName => attackName;
    public bool IsRanged => isRanged;
    public AnimationClip[] DirectionalAnimations => directionalAnimations;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float BaseDamage => baseDamage;
    public float DamageModifier => damageModifier;
    public float KnockbackForce => knockbackForce;
    public float Cooldown => cooldown;
    public float Weight => weight;

    // Get animation for a specific direction
    public AnimationClip GetAnimation(Direction dir) => directionalAnimations[(int)dir];

    // Calculate total damage
    public float DealDamage() => baseDamage + damageModifier;

    // Check if cooldown is ready
    public bool IsReady() => Time.time >= lastUsedTime + cooldown;
}
