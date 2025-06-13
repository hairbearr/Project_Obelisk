using UnityEngine;
using Pathfinding;


public class EnemyRangeCheck : MonoBehaviour
{
    [SerializeField] EnemyController enemyController;

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
                print(collision + " is in Attack Range of " + enemyController.name);
                enemyController.IsInAttackRange = true;
                enemyController.GetComponent<AIPath>().canMove = false;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                print(collision + " is out of Attack Range of "+ enemyController.name);
                enemyController.IsInAttackRange = false;
                enemyController.GetComponent<AIPath>().canMove = true;
            }
        }
    }
}
