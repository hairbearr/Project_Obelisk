using System;
using UnityEngine;

public class EnemyWeaponController : MonoBehaviour
{
    private EnemyController enemy;
    private Animator animator;
<<<<<<< HEAD

    [SerializeField] private Attack[] attackPool;
    private Attack currentAttack;
=======
    [SerializeField] float baseDamage, damageModifier, knockbackForce;
>>>>>>> parent of 5b3f190 (Enemy Attacks)

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponentInParent<EnemyController>();
        animator = GetComponent<Animator>();
    }

    private void NextAttack()
    {
        // roll a die, pick either basic attack or whichever one of it's special attacks
        // cast that attack/play that animation
    }

    void BasicMeleeAttack()
    {
<<<<<<< HEAD
        // get all attacks that are off cooldown
        List<Attack> readyAttacks = attackPool.Where(a => a.IsReady()).ToList();
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

        // Play animation for the correct direction
        AnimationClip clip = currentAttack.GetAnimation(enemy.Direction);
        if (clip != null)
        {
            animator.Play(clip.name);
        }

        //if it's a ranged attack and has a projectile, fire it
        if (currentAttack.IsRanged && currentAttack.ProjectilePrefab != null)
        {
            FireProjectile(currentAttack);
        }
    }

    private void FireProjectile(Attack attack)
=======
        //pick the right animation
        //set baseDamage, damageModifier, and knockbackForce correctly
    }

    void BasicRangedAttack()
>>>>>>> parent of 5b3f190 (Enemy Attacks)
    {
        //pick the right animation
        //set the baseDamage, damageModifier, and KnockbackForce correctly
        // pick the right projectile, and set it's baseDamage, damageModifier, and KnockbackForce correctly
    }

    private float DealDamage(float damage, float modifier)
    {
        return damage + modifier;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision != null)
        {
            if (collision.collider.gameObject.CompareTag("Shield"))
            {
                collision.collider.gameObject.GetComponent<ShieldController>().ShieldDamage(DealDamage(baseDamage, damageModifier), knockbackForce, transform.position);
                return;
            }
            if (collision.collider.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<Health>().TakeDamage(DealDamage(baseDamage, damageModifier), knockbackForce, transform.position);
            }
        }
    }


}
