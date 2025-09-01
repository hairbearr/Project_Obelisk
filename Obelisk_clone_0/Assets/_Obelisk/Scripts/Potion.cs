using UnityEngine;
using Sigilspire.Combat;

public class Potion : MonoBehaviour
{
    private Health health;
    [SerializeField] private float potionCount, maxPotions, potionPotency, potionUpgrade, maxPotionUpgrades;
    private bool usePotion, upgradePotion, pickingUpPotion;

    public bool UsePotion
    {
        get { return usePotion;  }
        set { usePotion = value; }
    }

    public bool UpgradePotion
    {
        get { return upgradePotion;  }
        set { upgradePotion = value; }
    }

    public bool IsPickingUpPotion
    {
        get { return pickingUpPotion; }
        set { pickingUpPotion = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        health = GetComponent<Health>();
    }

    // Update is called once per frame
    void Update()
    {
        if (usePotion)
        {
            DrinkPotion();
        }

        if (upgradePotion)
        {
            PotionUpgrade();
        }

        if (pickingUpPotion)
        {
            PickUpPotion();
        }
    }

    private void DrinkPotion()
    {
        //Need to make this health.CurrentHealth readonly, and then add an "add to health" function in Health, then have this Call it

        if (potionCount > 0)
        {
            if (potionUpgrade == 00)
            {
                potionPotency = 2f;
            }
            else
            {
                potionPotency = potionUpgrade * 4f;
            }

            if (health.CurrentHealth.Value >= health.MaxHealth)
            {
                health.CurrentHealth.Value = health.MaxHealth;
            }
            else
            {
                health.CurrentHealth.Value += potionPotency;
                potionCount--;
                if (health.CurrentHealth.Value >= health.MaxHealth)
                {
                    health.CurrentHealth.Value = health.MaxHealth;
                }
            }
            usePotion = false;
        }        
    }

    private void PotionUpgrade()
    {
        potionUpgrade++;
        if(potionCount < maxPotions)
        {
            potionCount++;
        }
        upgradePotion = false;
    }

    private void PickUpPotion()
    {
        if(potionCount < maxPotions)
        {
            potionCount++;
        }
    }

}
