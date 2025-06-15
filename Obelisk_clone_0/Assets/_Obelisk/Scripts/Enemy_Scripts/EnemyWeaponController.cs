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

    // Update is called once per frame
    void Update()
    {
        Animate();
    }
    private float DealDamage(float damage, float modifier)
    {
        return damage + modifier;
    }

    private void Animate()
    {
        animator.SetBool("IsAttacking", enemy.IsAttacking);
        animator.SetBool("IsSpecialAttacking", enemy.SpecialAttack);
        animator.SetBool("IsGettingHit", enemy.IsGettingHit);
        animator.SetBool("IsRunning", enemy.IsRunning);
        animator.SetBool("IsWalking", enemy.IsWalking);
        animator.SetFloat("Direction", enemy.Direction);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision != null)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<Health>().TakeDamage(DealDamage(baseDamage, damageModifier), knockbackForce, transform.position);
            }
        }
    }


}
