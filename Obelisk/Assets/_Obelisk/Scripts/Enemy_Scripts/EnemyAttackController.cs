using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyWeaponController : MonoBehaviour
{
    private EnemyController enemy;
    private Animator animator;

    [SerializeField] private Attack[] attackPool;
    private Attack currentAttack;
    private Queue<Attack> comboQueue = new Queue<Attack>(); // Queue for combo chaining

    void Start()
    {
        enemy = GetComponentInParent<EnemyController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (enemy == null || enemy.IsDead) return;

        if (enemy.IsInAttackRange)
        {
            Attack();
        }
    }

    private void Attack()
    {
        // If combo queue has attacks, execute next in queue
        if (comboQueue.Count > 0)
        {
            currentAttack = comboQueue.Dequeue();
        }
        else
        {
            // Otherwise, pick a weighted random ready attack
            List<Attack> readyAttacks = attackPool.Where(a => a.IsReady()).ToList();
            if (readyAttacks.Count == 0) return;

            float totalWeight = readyAttacks.Sum(a => a.Weight);
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var attack in readyAttacks)
            {
                cumulative += attack.Weight;
                if (roll <= cumulative)
                {
                    currentAttack = attack;
                    break;
                }
            }
        }

        if (currentAttack == null) return;

        // Record last used time
        currentAttack.lastUsedTime = Time.time;

        // Play animation for correct direction
        AnimationClip clip = currentAttack.GetAnimation(AnimationType.Attack, enemy.Direction);
        if (clip != null) animator.Play(clip.name);

        // Fire projectile if ranged
        if (currentAttack.IsRanged && currentAttack.ProjectilePrefab != null)
        {
            FireProjectile(currentAttack);
        }

        // Queue next attack if comboable
        if (currentAttack.CanChainCombo)
        {
            List<Attack> comboAttacks = attackPool.Where(a => a.IsReady() && a.IsSpecialAttack).ToList();
            foreach (var a in comboAttacks)
            {
                comboQueue.Enqueue(a);
            }
        }
    }

    private void FireProjectile(Attack attack)
    {
        GameObject projectile = Instantiate(attack.ProjectilePrefab, transform.position, Quaternion.identity);
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.Initialize(attack.DealDamage(), attack.KnockbackForce, enemy.Direction);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || currentAttack == null) return;

        float damage = currentAttack.DealDamage();
        float knockback = currentAttack.KnockbackForce;

        if (collision.collider.CompareTag("Player"))
        {
            collision.collider.GetComponent<Health>()?.TakeDamage(damage, knockback, transform.position);
        }
    }
}
