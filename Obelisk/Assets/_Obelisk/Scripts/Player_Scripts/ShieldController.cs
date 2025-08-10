using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ShieldController : NetworkBehaviour
{
    private PlayerController player;
    private Animator animator;
    private BoxCollider2D boxCollider;

    [SerializeField] private int abilityID;
    [SerializeField] private float maxShieldEnergy = 100f;
    [SerializeField] private float shieldRegenRate = 5f;
    [SerializeField] private float knockBackModifier = 2f;
    [SerializeField] private float disableTime = 2f;
    [SerializeField] private float shieldCooldownTime = 5f;

    private NetworkVariable<float> shieldEnergy = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> shieldCooldown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float ShieldCooldownTime
    {
        get => shieldCooldownTime;
        set => shieldCooldownTime = value;
    }

    void Start()
    {
        player = GetComponentInParent<PlayerController>();
        animator = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.enabled = false;

        // Initialize shield energy and cooldown on server
        if (IsServer)
        {
            shieldEnergy.Value = maxShieldEnergy;
            shieldCooldown.Value = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return; // Only local player controls shield logic

        if (shieldEnergy.Value > 0 && !shieldCooldown.Value)
        {
            if (player.IsBlocking > 0)
            {
                ShieldBlock();
                RequestSetIsBlockingServerRpc(true);
            }
            else
            {
                LowerShield();
                RequestSetIsBlockingServerRpc(false);
            }
        }
        else
        {
            LowerShield();
            RequestSetIsBlockingServerRpc(false);
        }

        if (shieldCooldown.Value)
        {
            StartCoroutine(ShieldDisabled());
        }
        else if (player.IsBlocking <= 0 && !shieldCooldown.Value)
        {
            StartCoroutine(RegenerateShieldEnergy());
        }

        Animate();
    }

    private void ShieldBlock()
    {
        boxCollider.enabled = true;
        // Blocking active
    }

    private void LowerShield()
    {
        boxCollider.enabled = false;
        // Blocking inactive
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestSetIsBlockingServerRpc(bool isBlocking)
    {
        // Sync player blocking state on the server
        player.IsBlocking = isBlocking ? 1f : 0f;

        // Notify clients if needed (player.IsBlocking can be a NetworkVariable too for full sync)
        SetIsBlockingClientRpc(player.IsBlocking);
    }

    [ClientRpc]
    private void SetIsBlockingClientRpc(float isBlockingValue)
    {
        if (!IsOwner)
        {
            player.IsBlocking = isBlockingValue;
        }
    }

    public void ShieldDamage(float amount, float knockBackForce, Vector3 position)
    {
        if (!IsServer) return; // Server authority

        knockBackForce /= knockBackModifier;

        if (shieldEnergy.Value > 0)
        {
            float originalAmount = amount;
            amount -= shieldEnergy.Value;
            shieldEnergy.Value -= originalAmount;

            if (shieldEnergy.Value < 0)
                shieldEnergy.Value = 0;
        }

        if (shieldEnergy.Value <= 0)
        {
            shieldCooldown.Value = true;
            RequestSetIsBlockingServerRpc(false);
        }

        if (amount < 0)
            amount = 0;

        var health = player.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(amount, knockBackForce, position);
        }
    }

    private IEnumerator RegenerateShieldEnergy()
    {
        while (shieldEnergy.Value < maxShieldEnergy && !shieldCooldown.Value && player.IsBlocking <= 0)
        {
            shieldEnergy.Value += shieldRegenRate * Time.deltaTime;
            if (shieldEnergy.Value > maxShieldEnergy)
                shieldEnergy.Value = maxShieldEnergy;
            yield return null;
        }
    }

    private IEnumerator ShieldDisabled()
    {
        player.IsBlocking = 0f;
        shieldCooldown.Value = true;

        StartCoroutine(player.GetComponent<PlayerController>().DelayedDisable(disableTime));
        yield return new WaitForSeconds(shieldCooldownTime);

        shieldCooldown.Value = false;
        player.IsBlocking = 0f;
    }

    private void Animate()
    {
        animator.SetFloat("Direction", (float)player.Direction);
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
}




// old code
//using System.Collections;
//using UnityEngine;

//public class ShieldController : MonoBehaviour
//{
//    private PlayerController player;
//    private Animator animator;
//    [SerializeField] int abilityID;
//    [SerializeField] float shieldEnergy, maxShieldEnergy, shieldRegenRate, knockBackModifier, disableTime, shieldCooldownTime;
//    [SerializeField] bool isBlocking, hasShield, shieldCooldown, regenerateShieldEnergy;

//    public float ShieldCooldownTime
//    {
//        get { return shieldCooldownTime; }
//        set { shieldCooldownTime = value; }
//    }


//    // Start is called once before the first execution of Update after the MonoBehaviour is created

//    void Start()
//    {
//        player = GetComponentInParent<PlayerController>();
//        animator = GetComponent<Animator>();
//    }

//    // Update is called once per frame
//    void Update()
//    {

//        if (hasShield)
//        {
//            Animate();
//            if (player.IsBlocking > 0 && !shieldCooldown)
//            {
//                ShieldBlock();
//            }

//            if (player.IsBlocking <= 0)
//            {
//                LowerShield();
//            }

//            if (shieldCooldown)
//            {
//                StartCoroutine(ShieldDisabled());
//            }

//            if (!shieldCooldown && player.IsBlocking <= 0)
//            {
//                StartCoroutine(RegenerateShieldEnergy());
//            }
//        }
//    }

//    private void ShieldBlock()
//    {
//        GetComponent<BoxCollider2D>().enabled = true;
//        // turn on the Box Collider
//        // block the incoming damage
//        // while shield blocking decrease shieldEnergy per damage done.
//        // have it be a re-useable health bar, basically. 
//    }

//    private void LowerShield()
//    {
//        GetComponent<BoxCollider2D>().enabled = false;
//    }

//    private void ShieldBreak()
//    {
//        // disable the player
//        // regen shield energy
//        // re-enable the player
//    }

//    private void CastAbility()
//    {
//        if (abilityID != 0)
//        {
//            print("Cast Shield Ability");
//            // use a switch to decide which abilities to cast, have functions to call what ability it is
//        }
//        else
//        {
//            print("No Shield Ability");
//        }
//    }

//    public void ShieldDamage(float amount, float knockBackForce, Vector3 position)
//    {
//        knockBackForce /= knockBackModifier;
//        if(shieldEnergy > 0)
//        {
//            float ogAmount = amount;
//            amount -= shieldEnergy;
//            shieldEnergy -= ogAmount;
//        }
//        else if (shieldEnergy <= 0)
//        {
//            shieldCooldown = true;
//            amount -= shieldEnergy;
//            player.IsBlocking = 0;
//        }
//        if( amount < 0)
//        {
//            amount = 0;
//        }

//        print(this.gameObject + "Being Damaged By " + amount + " amount, with " + knockBackForce + " force.");
//        player.GetComponent<Health>().TakeDamage(amount, knockBackForce, position);
//    }

//    IEnumerator Delay(float delayTime)
//    {
//        yield return new WaitForSeconds(delayTime);
//    }

//    private IEnumerator RegenerateShieldEnergy()
//    {
//        if(shieldEnergy < maxShieldEnergy)
//        {
//            shieldEnergy += shieldRegenRate;
//            if(shieldEnergy >= maxShieldEnergy)
//            {
//                shieldEnergy = maxShieldEnergy;
//                yield break;
//            }
//        }
//        yield return new WaitForSeconds(.1f);
//    }

//    private IEnumerator ShieldDisabled()
//    {
//        player.IsBlocking = 0;
//        shieldCooldown = true;
//        StartCoroutine(player.GetComponent<PlayerController>().DelayedDisable(disableTime));
//        yield return new WaitForSeconds(shieldCooldownTime);
//        shieldCooldown = false;
//        player.IsBlocking = 0;
//        shieldCooldown = false;
//    }


//    private void Animate()
//    {
//        animator.SetFloat("Direction", player.Direction);
//        animator.SetFloat("IsMoving", player.IsMoving);
//        animator.SetFloat("IsAttacking", player.IsAttacking);
//        animator.SetFloat("SwordAttackType", player.SwordAttackType);
//        animator.SetFloat("IsBlocking", player.IsBlocking);
//        animator.SetFloat("IsClimbing", player.IsClimbing);
//        animator.SetFloat("IsDrinkingPotion", player.IsDrinkingPotion);
//        animator.SetFloat("IsInteracting", player.IsInteracting);
//        animator.SetFloat("IsJumping", player.IsJumping);
//        animator.SetFloat("IsGrappling", player.IsGrappling);
//        animator.SetFloat("IsShooting", player.IsShooting);
//        animator.SetFloat("IsUsingItem", player.IsUsingItem);
//        animator.SetFloat("IsDead", player.IsDead);
//    }
//}
