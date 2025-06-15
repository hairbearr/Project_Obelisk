using Pathfinding;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Health : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float health, maxHealth, healthUpgrade, maxHealthUpgrade, knockBackDelayTime = .15f;
    // needs UI for health bar, health upgrades

    public float CurrentHealth
    {
        get { return health; }
        set { health = value; }
    }

    public float MaximumHealth
    {
        get { return maxHealth; }
        set { maxHealth = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // get the rb for the damage knockback
        rb = GetComponent<Rigidbody2D>();
    }

    void UpgradeHealth()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(float amount, float knockBackForce, Vector3 position)
    {
        print(this + " is being knocked backwards");
        if(this.GetComponent<EnemyController>() != null)
        {
            GetComponent<AIPath>().enabled = false;
        }
        rb.AddForce((transform.position - position) * knockBackForce, ForceMode2D.Force);
        print(this + " is being damaged");
        health -= amount;
        if(health <= 0)
        {
            // play death animation that calls a death function
            GetComponent<BoxCollider2D>().enabled = false;
        }

        StartCoroutine(KnockBackStop(knockBackDelayTime));
    }

    IEnumerator KnockBackStop(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        rb.linearVelocity = Vector3.zero;
        if (this.GetComponent<EnemyController>() != null)
        {
            GetComponent<AIPath>().enabled = true;
        }
    }

    void DisplayHealth(GameObject gameObject)
    {
        // if gameObject = player, display on HUD UI

        // if gameObject = enemy, display on healthbar above head, and only show bars if health < maxHealth
    }
}
