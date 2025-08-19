using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all sword-related actions for the player using Attack ScriptableObjects.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerSwordController : MonoBehaviour
{
    [Header("Sword Attacks")]
    [SerializeField] private Attack[] attackPool; // Assign sword attacks in inspector
    private Attack currentAttack;

    private Animator animator;
    private PlayerController playerController;

    private bool isComboActive = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponentInParent<PlayerController>();

        if (attackPool == null || attackPool.Length == 0)
            Debug.LogWarning("No sword attacks assigned to PlayerSwordController!");
    }

    /// <summary>
    /// Perform an attack in the specified direction.
    /// </summary>
    public void PerformAttack(Direction direction)
    {
        if (playerController.IsDead) return;

        // Select next available attack
        currentAttack = GetNextAttack();

        if (currentAttack == null) return;

        // Set player state
        playerController.IsAttacking = true;

        // Play directional animation
        AnimationClip clip = currentAttack.GetAnimation(direction);
        if (clip != null)
        {
            animator.Play(clip.name);
        }

        // Apply attack effects after animation delay (optional)
        StartCoroutine(HandleAttackCooldown(currentAttack.Cooldown));
    }

    /// <summary>
    /// Chooses the next attack based on cooldown and weighted selection.
    /// </summary>
    private Attack GetNextAttack()
    {
        List<Attack> readyAttacks = new List<Attack>();
        foreach (var atk in attackPool)
        {
            if (atk.IsReady())
                readyAttacks.Add(atk);
        }

        if (readyAttacks.Count == 0) return null;

        // Weighted selection
        float totalWeight = 0f;
        foreach (var atk in readyAttacks) totalWeight += atk.Weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var atk in readyAttacks)
        {
            cumulative += atk.Weight;
            if (roll <= cumulative) return atk;
        }

        return readyAttacks[0]; // fallback
    }

    /// <summary>
    /// Handles cooldown timing and combo chaining.
    /// </summary>
    private IEnumerator HandleAttackCooldown(float cooldown)
    {
        currentAttack.lastUsedTime = Time.time;

        // Wait for the cooldown duration before allowing next attack
        yield return new WaitForSeconds(cooldown);

        if (currentAttack.CanChainCombo)
        {
            isComboActive = true;
        }
        else
        {
            isComboActive = false;
            playerController.IsAttacking = false;
        }
    }

    /// <summary>
    /// Called by animation event or external script to apply attack effects.
    /// </summary>
    public void ApplyAttackEffects(GameObject target)
    {
        if (currentAttack != null)
        {
            currentAttack.ApplyEffect(target);
        }
    }
}
