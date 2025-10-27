using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float damage;
    private float knockback;
    private Vector2 direction;
    private Rigidbody2D rb;

    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(float dmg, float knock, float directionIndex)
    {
        damage = dmg;
        knockback = knock;
        direction = DirectionFromIndex((int)directionIndex);

        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    private Vector2 DirectionFromIndex(int index)
    {
        switch (index)
        {
            case 0: return Vector2.right;                               // E
            case 1: return Vector2.up;                                  // N
            case 2: return (Vector2.up + Vector2.right).normalized;     // NE
            case 3: return (Vector2.up + Vector2.left).normalized;      // NW
            case 4: return Vector2.down;                                // S
            case 5: return (Vector2.down + Vector2.right).normalized;   // SE
            case 6: return (Vector2.down + Vector2.left).normalized;    // SW
            case 7: return Vector2.left;                                // W
            default: return Vector2.right;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.gameObject.CompareTag("Shield"))
        {
            collision.collider.gameObject.GetComponent<ShieldController>()?.ShieldDamage(damage, knockback, transform.position);
        }
        else if (collision.collider.gameObject.CompareTag("Player"))
        {
            collision.collider.gameObject.GetComponent<Health>()?.TakeDamage(damage, knockback, transform.position);
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
