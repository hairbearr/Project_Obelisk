using UnityEngine;

public class SwordController : MonoBehaviour
{
    private PlayerController player;
    private Animator animator;
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
    }

    private void DealDamage()
    {

    }

    private void CastSwordAbility()
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
}
