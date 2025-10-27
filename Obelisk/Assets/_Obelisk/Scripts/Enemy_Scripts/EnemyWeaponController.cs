using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyWeaponController : MonoBehaviour
{
    private EnemyController enemy;
    private Animator animator;

    [SerializeField] private EnemyAttack[] attackPool;
    private EnemyAttack currentAttack;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponentInParent<EnemyController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (enemy != null)
        {
            if (enemy.IsInAttackRange)
            {
                Attack();
            }
        }    
    }

    // so you get into attack range.
    // you do NextAttack(), which calls the first attack
    // you do the first attack, which calls NextAttack(), which calls the next attack
    // keep going

    private void Attack()
    {
        // get all attacks that are off cooldown
        List<EnemyAttack> readyAttacks = attackPool.Where(a => a.IsReady()).ToList();
        if (readyAttacks.Count == 0) return; // No attacks are ready, do nothing

        // Sum weights of all ready attacks for weighted random selection
        float totalWeight = readyAttacks.Sum(a => a.Weight);

        // Roll a random value between 0 and totalWeight
        float roll = Random.Range(0f, totalWeight);

        float cumulative = 0f;
        foreach (var attack in readyAttacks)
        {
            cumulative += attack.Weight;
            if(roll <= cumulative)
            {
                currentAttack = attack;
                break; // select the attack and exit loop
            }
        }

        if (currentAttack == null) { return; }

        // Record current time as last used for cooldown tracking
        currentAttack.lastUsedTime = Time.time;

        // play the attack animation if assigned
        if(currentAttack.Animation != null)
        {
            animator.Play(currentAttack.Animation.name);
        }

        //if it's a ranged attack and has a projectile, fire it
        if (currentAttack.IsRanged && currentAttack.ProjectilePrefab != null)
        {
            FireProjectile(currentAttack);
        }
    }

    private void FireProjectile(EnemyAttack attack)
    {
        // instantiate the projectile at the enemy's position, no rotation
        GameObject projectile = Instantiate(attack.ProjectilePrefab, transform.position, Quaternion.identity);

        // initialize the projectile with damage and knockback values
        Projectile proj = projectile.GetComponent<Projectile>();
        if (proj != null) 
        {
            proj.Initialize(attack.DealDamage(), attack.KnockbackForce, enemy.Direction);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision == null || currentAttack == null) { return; }

        float damage = currentAttack.DealDamage();
        float knockback = currentAttack.KnockbackForce;

        if (collision.collider.gameObject.CompareTag("Shield"))
        {
            collision.collider.gameObject.GetComponent<ShieldController>()?.ShieldDamage(damage, knockback, transform.position);
            return;
        }
           if (collision.collider.gameObject.CompareTag("Player"))
        {
            collision.collider.gameObject.GetComponent<Health>()?.TakeDamage(damage, knockback, transform.position);
        }
    }


}
