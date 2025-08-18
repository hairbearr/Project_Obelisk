using System;
using Unity.Netcode;
using UnityEngine;

public class SwordController : NetworkBehaviour
{
    // Reference to the PlayerController (parent object)
    private PlayerController player;

    // Collider for detecting hits
    private BoxCollider2D boxCollider;

    // Animator for the sword itself (for unique swing/ability animations)
    private Animator animator;

    // Currently equipped attack (sigil/ability)
    [SerializeField] private Attack currentAttack;

    // Unique ID for this ability (optional)
    [SerializeField] private int abilityID;

    void Start()
    {
        // Grab references to PlayerController, Animator, and Collider
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

        if (currentAttack != null)
        {
            // Set initial stats from Attack ScriptableObject
            player.SetParameterFloat("SwordAttackType", 1f);
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Sync sword animation with player animator parameters
        AnimateSword();

        // Handle combo timer
        if (player.GetParameterFloat("SwordAttackType") > 1)
        {
            player.AttackComboTimer -= Time.deltaTime;
        }

        if (player.AttackComboTimer <= 0)
        {
            player.SetParameterFloat("SwordAttackType", 1);
            player.AttackComboTimer = 10f;
        }
    }

    /// <summary>
    /// Called by animation events when the sword swing starts
    /// </summary>
    public void SwingSword()
    {
        if (!IsOwner || currentAttack == null) return;

        // Disable player input/actions during swing
        player.SetParameterBool("IsDisabled", true);

        // Reset combo timer
        player.AttackComboTimer = 10f;

        // Ensure SwordAttackType starts at 1
        if (player.GetParameterFloat("SwordAttackType") < 1)
            player.SetParameterFloat("SwordAttackType", 1);

        player.SetParameterBool("AttackCooldown", true);

        // Trigger swing animation across clients
        PlaySwingAnimationClientRpc(player.GetParameterFloat("SwordAttackType"));
    }

    /// <summary>
    /// Called by animation events when the sword swing ends
    /// </summary>
    public void SheatheSword()
    {
        if (!IsOwner || currentAttack == null) return;

        // Increment combo attack type
        float attackType = player.GetParameterFloat("SwordAttackType") + 1;
        if (attackType > 3) attackType = 1;
        player.SetParameterFloat("SwordAttackType", attackType);

        // Reset attacking state
        player.SetParameterFloat("IsAttacking", 0);
        player.SetParameterBool("AttackCooldown", false);
        player.SetParameterBool("IsDisabled", false);
    }

    /// <summary>
    /// Deal damage to a target using the Attack ScriptableObject
    /// </summary>
    public float DealDamage()
    {
        return currentAttack != null ? currentAttack.DealDamage() : 0f;
    }

    /// <summary>
    /// Update sword animator with all relevant player parameters
    /// </summary>
    private void AnimateSword()
    {
        foreach (var param in player.GetAnimatorParameters())
        {
            switch (param.Value)
            {
                case float f:
                    animator.SetFloat(param.Key, f);
                    break;
                case bool b:
                    animator.SetBool(param.Key, b);
                    break;
            }
        }
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
                health.TakeDamage(DealDamage(), currentAttack != null ? currentAttack.KnockbackForce : 0f, transform.position);
            }
        }
    }

    [ClientRpc]
    private void PlaySwingAnimationClientRpc(float attackType)
    {
        animator.SetFloat("SwordAttackType", attackType);
    }

    /// <summary>
    /// Equip a new Attack ScriptableObject (e.g., swapping sigils)
    /// </summary>
    public void EquipAttack(Attack newAttack)
    {
        currentAttack = newAttack;
        if (currentAttack != null)
        {
            player.SetParameterFloat("SwordAttackType", 1f);
        }
    }
}

