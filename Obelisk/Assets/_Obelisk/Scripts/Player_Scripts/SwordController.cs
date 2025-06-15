using System;
using UnityEngine;

public class SwordController : MonoBehaviour
{
    private PlayerController player;
    private Animator animator;
    [SerializeField] float baseDamage, damageModifier, knockbackForce, swordType; 
    // private somethingCollider2d collider;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        Animate();
        SwordTimer();
    }

    private void SwordTimer()
    {
        if (player.SwordAttackType > 1)
        {
            player.AttackComboTimer -= Time.deltaTime;
        }
        if (player.AttackComboTimer <= 0)
        {
            player.SwordAttackType = 1;
            player.AttackComboTimer = 10;
        }
    }

    public float DealDamage(float damage, float modifier)
    {
        return damage + modifier;
    }

    private void SwingSword()
    {
        player.IsDisabled = true;
        player.AttackComboTimer = 10f;
        if (player.SwordAttackType < 1)
        {
            player.SwordAttackType = 1;
        }
        player.AttackCooldown = true;
    }

    private void SheatheSword()
    {
        player.SwordAttackType++;
        if(player.SwordAttackType >3)
        {
            player.SwordAttackType = 1;
        }
        player.IsAttacking = 0;
        player.AttackCooldown = false;
        player.IsDisabled = false;
    }

    private void CastAbility(float abilityID)
    {
        print("Sword Ability");
    }

    private void Animate()
    {
        animator.SetFloat("Direction", player.Direction);
        animator.SetFloat("IsMoving", player.IsMoving);
        animator.SetFloat("IsAttacking", player.IsAttacking);
        animator.SetFloat("SwordAttackType", player.SwordAttackType);
        animator.SetFloat("IsBlocking", player.IsBlocking);
        animator.SetFloat("IsClimbing", player.IsClimbing);
        animator.SetFloat("IsDrinkingPotion", player.IsDrinkingPotion);
        animator.SetFloat("IsGettingHit", player.IsGettingHit);
        animator.SetFloat("IsInteracting", player.IsInteracting);
        animator.SetFloat("IsJumping", player.IsJumping);
        animator.SetFloat("IsGrappling", player.IsGrappling);
        animator.SetFloat("IsShooting", player.IsShooting);
        animator.SetFloat("IsUsingItem", player.IsUsingItem);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision != null)
        {
            if (collision.gameObject.CompareTag("Enemy"))
            {
                collision.gameObject.GetComponent<Health>().TakeDamage(DealDamage(baseDamage, damageModifier), knockbackForce, transform.position);
            }
        }
    }
}
