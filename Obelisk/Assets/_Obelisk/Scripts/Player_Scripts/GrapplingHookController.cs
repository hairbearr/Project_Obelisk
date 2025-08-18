using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GrapplingHookController : NetworkBehaviour
{
    // Reference to the PlayerController
    private PlayerController player;

    // Animator for grappling hook-specific animations
    private Animator animator;

    // Grappling hook prefab (visuals/projectile)
    [SerializeField] private GameObject hookPrefab;

    // Active hook instance
    private GameObject activeHook;

    // Currently targeted object
    private Rigidbody2D targetRigidbody;
    private Vector3 hookTargetPosition;
    private bool isPulling;

    // Cooldown
    private bool isOnCooldown;

    // Currently equipped attack/sigil
    [SerializeField] private Attack currentAttack;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (!IsOwner) return;

        AnimateHook();

        if (isPulling && targetRigidbody != null)
        {
            Vector3 direction = player.transform.position - (Vector3)targetRigidbody.position;
            float distance = direction.magnitude;
            direction.Normalize();

            targetRigidbody.linearVelocity = direction * currentAttack.PullSpeed;

            if (distance < 1f)
            {
                StopPulling();
            }
        }
    }

    /// <summary>
    /// Fire grappling hook toward target point
    /// </summary>
    public void FireGrapple(Vector3 targetPoint)
    {
        if (isOnCooldown || isPulling || currentAttack == null) return;

        float distance = Vector3.Distance(player.transform.position, targetPoint);
        if (distance > currentAttack.MaxRange) return;

        hookTargetPosition = targetPoint;

        // Spawn hook visual
        if (hookPrefab != null)
        {
            activeHook = Instantiate(hookPrefab, player.transform.position, Quaternion.identity);
            var spriteRenderer = activeHook.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && currentAttack.HookSprite != null)
            {
                spriteRenderer.sprite = currentAttack.HookSprite; // Apply sprite from SO
            }
        }

        // Detect rigidbody at target
        Collider2D hit = Physics2D.OverlapCircle(targetPoint, 0.5f);
        if (hit != null && hit.attachedRigidbody != null)
        {
            targetRigidbody = hit.attachedRigidbody;

            if (hit.CompareTag("Enemy"))
            {
                EnemyController enemy = hit.GetComponent<EnemyController>();
                if (enemy != null && !enemy.IsBoss)
                    StartPulling();
            }
            else
            {
                StartPulling();
            }
        }

        StartCoroutine(GrappleCooldown());
    }

    private void StartPulling()
    {
        isPulling = true;
        player.IsMovementLocked = true;
        player.SetParameterFloat("IsGrappling", 1f);

        // Apply custom effect from Attack SO
        currentAttack.ApplyEffect(targetRigidbody?.gameObject);
    }

    private void StopPulling()
    {
        isPulling = false;
        player.IsMovementLocked = false;
        player.SetParameterFloat("IsGrappling", 0f);

        if (targetRigidbody != null)
            targetRigidbody.linearVelocity = Vector2.zero;

        targetRigidbody = null;

        if (activeHook != null)
            Destroy(activeHook);
    }

    private IEnumerator GrappleCooldown()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(currentAttack.Cooldown);
        isOnCooldown = false;
    }

    /// <summary>
    /// Sync grappling hook animator with player parameters
    /// </summary>
    private void AnimateHook()
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

    /// <summary>
    /// Equip a new grappling hook sigil/attack
    /// </summary>
    public void EquipGrapple(Attack newAttack)
    {
        currentAttack = newAttack;
        if (currentAttack != null)
        {
            // Override grappling hook properties with values from Attack SO
            // PullSpeed, MaxRange, Cooldown, Sprite, etc.
        }
    }
}
