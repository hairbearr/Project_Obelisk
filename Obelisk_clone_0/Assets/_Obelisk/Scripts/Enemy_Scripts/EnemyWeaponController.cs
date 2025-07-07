using System;
using UnityEngine;

public class EnemyWeaponController : MonoBehaviour
{
    private EnemyController enemy;
    private Animator animator;
    [SerializeField] float baseDamage, damageModifier, knockbackForce;

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
        //pick the right animation
        //set baseDamage, damageModifier, and knockbackForce correctly
    }

    void BasicRangedAttack()
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
