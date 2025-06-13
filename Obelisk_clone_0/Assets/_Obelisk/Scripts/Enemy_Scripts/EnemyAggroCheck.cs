using Pathfinding;
using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{

    [SerializeField] private EnemyController enemyController;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemyController = GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {



        if (collision != null)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                print(collision + " is in Aggro Range of " + enemyController.name);
                enemyController.IsInAggroRange = true;
                enemyController.GetComponent<AIDestinationSetter>().target = collision.transform;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                print(collision + " is out of Aggro Range of " + enemyController.name);
                enemyController.IsInAggroRange = false;

                enemyController.GetComponent<AIDestinationSetter>().target = null;
            }
        }
    }
}
