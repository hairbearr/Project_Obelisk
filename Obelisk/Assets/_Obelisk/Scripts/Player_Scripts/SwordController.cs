using System;
using Unity.Netcode;
using UnityEngine;

public class SwordController : NetworkBehaviour
{
    private PlayerController player;
    private BoxCollider2D boxCollider;
    private Animator animator;

    [SerializeField] private float baseDamage, damageModifier, knockbackForce;
    [SerializeField] private int abilityID;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        if (!IsOwner) return;

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
            player.AttackComboTimer = 10f;
        }
    }

    public float DealDamage(float damage, float modifier)
    {
        return damage + modifier;
    }

    private void SwingSword()
    {
        if (!IsOwner) return;

        player.IsDisabled = true;
        player.AttackComboTimer = 10f;

        if (player.SwordAttackType < 1)
            player.SwordAttackType = 1;

        player.AttackCooldown = true;

        // Trigger animation for all clients
        PlaySwingAnimationClientRpc(player.SwordAttackType);
    }

    private void SheatheSword()
    {
        if (!IsOwner) return;

        player.SwordAttackType++;
        if (player.SwordAttackType > 3)
            player.SwordAttackType = 1;

        player.IsAttacking = 0;
        player.AttackCooldown = false;
        player.IsDisabled = false;
    }

    private void CastAbility()
    {
        if (!IsOwner) return;

        if (abilityID != 0)
        {
            Debug.Log("Cast Sword Ability");
            // Call ability logic here
        }
        else
        {
            Debug.Log("No Sword Ability");
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
        animator.SetFloat("IsInteracting", player.IsInteracting);
        animator.SetFloat("IsJumping", player.IsJumping);
        animator.SetFloat("IsGrappling", player.IsGrappling);
        animator.SetFloat("IsShooting", player.IsShooting);
        animator.SetFloat("IsUsingItem", player.IsUsingItem);
        animator.SetFloat("IsDead", player.IsDead);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner || !IsServer) return;

        if (collision != null && collision.CompareTag("Enemy"))
        {
            NetworkObject enemyNetObj = collision.GetComponent<NetworkObject>();
            if (enemyNetObj != null)
            {
                ApplyDamageServerRpc(enemyNetObj);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ApplyDamageServerRpc(NetworkObjectReference enemyRef)
    {
        if (enemyRef.TryGet(out NetworkObject enemyObj))
        {
            Health health = enemyObj.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(DealDamage(baseDamage, damageModifier), knockbackForce, transform.position);
            }
        }
    }

    [ClientRpc]
    private void PlaySwingAnimationClientRpc(float attackType)
    {
        animator.SetFloat("SwordAttackType", attackType);
        animator.SetTrigger("Swing"); // Assumes you use a "Swing" trigger in your Animator
    }
}

// old code
//using System;
//using UnityEngine;

//public class SwordController : MonoBehaviour
//{
//    private PlayerController player;
//    private BoxCollider2D boxCollider;
//    private Animator animator;
//    [SerializeField] float baseDamage, damageModifier, knockbackForce;
//    [SerializeField] int abilityID;
//    // private somethingCollider2d collider;
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        player = GetComponentInParent<PlayerController>();
//        animator = GetComponent<Animator>();
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        Animate();
//        SwordTimer();
//    }

//    private void SwordTimer()
//    {
//        if (player.SwordAttackType > 1)
//        {
//            player.AttackComboTimer -= Time.deltaTime;
//        }
//        if (player.AttackComboTimer <= 0)
//        {
//            player.SwordAttackType = 1;
//            player.AttackComboTimer = 10;
//        }
//    }

//    public float DealDamage(float damage, float modifier)
//    {
//        return damage + modifier;
//    }

//    private void SwingSword()
//    {
//        player.IsDisabled = true;
//        player.AttackComboTimer = 10f;
//        if (player.SwordAttackType < 1)
//        {
//            player.SwordAttackType = 1;
//        }
//        player.AttackCooldown = true;
//    }

//    private void SheatheSword()
//    {
//        player.SwordAttackType++;
//        if(player.SwordAttackType >3)
//        {
//            player.SwordAttackType = 1;
//        }
//        player.IsAttacking = 0;
//        player.AttackCooldown = false;
//        player.IsDisabled = false;
//    }

//    private void CastAbility()
//    {
//        if(abilityID != 0)
//        {
//            print("Cast Sword Ability");
//            // use a switch to decide which abilities to cast, have functions to call what ability it is
//        }
//        else
//        {
//            print ("No Sword Ability");
//        }
//    }

//    //private void Animate()
//    //{
//    //    animator.SetFloat("Direction", player.Direction);
//    //    animator.SetFloat("IsMoving", player.IsMoving);
//    //    animator.SetFloat("IsAttacking", player.IsAttacking);
//    //    animator.SetFloat("SwordAttackType", player.SwordAttackType);
//    //    animator.SetFloat("IsBlocking", player.IsBlocking);
//    //    animator.SetFloat("IsClimbing", player.IsClimbing);
//    //    animator.SetFloat("IsDrinkingPotion", player.IsDrinkingPotion);
//    //    animator.SetFloat("IsInteracting", player.IsInteracting);
//    //    animator.SetFloat("IsJumping", player.IsJumping);
//    //    animator.SetFloat("IsGrappling", player.IsGrappling);
//    //    animator.SetFloat("IsShooting", player.IsShooting);
//    //    animator.SetFloat("IsUsingItem", player.IsUsingItem);
//    //    animator.SetFloat("IsDead", player.IsDead);
//    //}

//    private void OnTriggerEnter2D(Collider2D collision)
//    {
//        if (collision != null)
//        {
//            if (collision.gameObject.CompareTag("Enemy"))
//            {
//                print(collision.gameObject.name);
//                collision.gameObject.GetComponent<Health>().TakeDamage(DealDamage(baseDamage, damageModifier), knockbackForce, transform.position);
//            }
//        }
//    }
//}
