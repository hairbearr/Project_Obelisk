using UnityEngine;

public class ShieldController : MonoBehaviour
{
    private PlayerController player;
    private Animator animator;
    [SerializeField] int abilityID;
    [SerializeField] float shieldEnergy;
    [SerializeField] bool isBlocking;
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

        if (player.IsBlocking > 0)
        {
            if (shieldEnergy > 0) { ShieldBlock(); }
            if (shieldEnergy <= 0) { ShieldBreak(); }
        }
        if(player.IsBlocking <= 0)
        {
            LowerShield();
        }

        // if shieldEnergy > 0
        // Shield Block
        // else if shieldEnergy <= 0
        // Shield Break
    }

    private void ShieldBlock()
    {
        // turn on the Box Collider
        // block the incoming damage
        // while shield blocking decrease shieldEnergy per damage done.
        // have it be a re-useable health bar, basically. 
    }

    private void LowerShield()
    {
        // lower the shield
    }

    private void ShieldBreak()
    {
        // disable the player
        // regen shield energy
        // re-enable the player
    }

    private void CastAbility()
    {
        if (abilityID != 0)
        {
            print("Cast Shield Ability");
            // use a switch to decide which abilities to cast, have functions to call what ability it is
        }
        else
        {
            print("No Shield Ability");
        }
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
