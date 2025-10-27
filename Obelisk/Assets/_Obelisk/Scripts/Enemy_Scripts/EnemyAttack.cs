using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttack", menuName = "Enemy/Attack")]
public class EnemyAttack : ScriptableObject
{
    [SerializeField] private string attackName;
    [SerializeField] private bool isRanged;
    [SerializeField] private AnimationClip animation;
    [SerializeField] private float baseDamage, damageModifier, knockbackForce, cooldown = 1f, weight = 1f;
    [SerializeField] private GameObject projectilePrefab;

    [HideInInspector] public float lastUsedTime = -Mathf.Infinity;

    public string AttackName => attackName;
    public bool IsRanged => isRanged;
    public AnimationClip Animation => animation;
    public GameObject ProjectilePrefab => projectilePrefab;
    public float BaseDamage => baseDamage;
    public float DamageModifier => damageModifier;
    public float KnockbackForce => knockbackForce;
    public float Cooldown => cooldown;
    public float Weight => weight;

    // calculate total damage from base + modifier
    public float DealDamage() => baseDamage + damageModifier;

    // check if cooldown time has passed to allow attack again
    public bool IsReady() => Time.time >= lastUsedTime + cooldown;
}
