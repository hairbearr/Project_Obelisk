using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ShieldController : NetworkBehaviour
{
    // Reference to the parent PlayerController
    private PlayerController player;

    // Animator for shield animations (blocking, attacking, sigil effects)
    private Animator animator;

    // Collider for melee shield attacks
    private BoxCollider2D boxCollider;

    // Currently equipped shield ability (sigil)
    [SerializeField] private Attack currentAttack;

    // Shield-specific stats
    [SerializeField] private float maxShieldEnergy = 100f;
    [SerializeField] private float shieldRegenRate = 5f;
    [SerializeField] private float knockBackModifier = 2f;
    [SerializeField] private float disableTime = 2f;
    [SerializeField] private float shieldCooldownTime = 5f;
    public float ShieldCooldownTime => shieldCooldownTime;


    // Server-synced shield variables
    private NetworkVariable<float> shieldEnergy = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> shieldCooldown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;

        // Initialize shield on server
        if (IsServer)
        {
            shieldEnergy.Value = maxShieldEnergy;
            shieldCooldown.Value = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // Handle blocking
        if (player.GetParameterFloat("IsBlocking") > 0 && shieldEnergy.Value > 0 && !shieldCooldown.Value)
        {
            EnableShield();
        }
        else
        {
            DisableShield();
        }

        // Handle cooldown or regeneration
        if (shieldCooldown.Value)
        {
            StartCoroutine(ShieldDisabled());
        }
        else if (player.GetParameterFloat("IsBlocking") <= 0)
        {
            StartCoroutine(RegenerateShieldEnergy());
        }

        // Sync shield animator with player parameters
        AnimateShield();
    }

    // Enables the shield collider and optionally triggers defensive abilities
    private void EnableShield()
    {
        boxCollider.enabled = true;
        // Optional: activate defensive effects, visual FX, etc.
    }

    private void DisableShield()
    {
        boxCollider.enabled = false;
    }

    // Called when shield takes damage
    public void ShieldDamage(float amount, float knockBackForce, Vector3 position)
    {
        if (!IsServer) return;

        knockBackForce /= knockBackModifier;

        // Reduce damage using shield energy
        float absorbed = Mathf.Min(amount, shieldEnergy.Value);
        shieldEnergy.Value -= absorbed;
        amount -= absorbed;

        if (shieldEnergy.Value <= 0)
        {
            shieldCooldown.Value = true;
            RequestSetIsBlockingServerRpc(false);
        }

        if (amount > 0)
        {
            var health = player.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(amount, knockBackForce, position);
            }
        }
    }

    // Equip a new shield sigil (Attack SO)
    public void EquipAttack(Attack newAttack)
    {
        currentAttack = newAttack;
    }

    // Called by PlayerController input or animation events
    public void CastAbility()
    {
        if (!IsOwner || currentAttack == null) return;

        if (!currentAttack.IsReady()) return;

        currentAttack.lastUsedTime = Time.time;

        if (currentAttack.IsRanged && currentAttack.ProjectilePrefab != null)
        {
            // Spawn projectile in world (networked)
            GameObject proj = Instantiate(currentAttack.ProjectilePrefab, transform.position, Quaternion.identity);
            // Optional: setup projectile direction & owner
            NetworkObject netObj = proj.GetComponent<NetworkObject>();
            netObj?.Spawn();
        }
        else
        {
            // Trigger shield melee attack animation or effects
            PlayShieldAbilityClientRpc();
        }
    }

    private IEnumerator RegenerateShieldEnergy()
    {
        while (shieldEnergy.Value < maxShieldEnergy && !shieldCooldown.Value && player.GetParameterFloat("IsBlocking") <= 0)
        {
            shieldEnergy.Value += shieldRegenRate * Time.deltaTime;
            if (shieldEnergy.Value > maxShieldEnergy)
                shieldEnergy.Value = maxShieldEnergy;
            yield return null;
        }
    }

    private IEnumerator ShieldDisabled()
    {
        player.SetParameterFloat("IsBlocking", 0f);
        shieldCooldown.Value = true;

        StartCoroutine(player.DelayedDisable(disableTime));
        yield return new WaitForSeconds(shieldCooldownTime);

        shieldCooldown.Value = false;
        player.SetParameterFloat("IsBlocking", 0f);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetIsBlockingServerRpc(bool isBlocking)
    {
        player.SetParameterFloat("IsBlocking", isBlocking ? 1f : 0f);
        SetIsBlockingClientRpc(player.GetParameterFloat("IsBlocking"));
    }

    [ClientRpc]
    private void SetIsBlockingClientRpc(float isBlockingValue)
    {
        if (!IsOwner)
        {
            player.SetParameterFloat("IsBlocking", isBlockingValue);
        }
    }

    [ClientRpc]
    private void PlayShieldAbilityClientRpc()
    {
        animator.SetTrigger("CastAbility");
    }

    private void AnimateShield()
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
}
