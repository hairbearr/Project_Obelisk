using UnityEngine;
using Pathfinding;
using Sigilspire.Enemy;

public class EnemyRangeCheck : MonoBehaviour
{
    [SerializeField] private EnemyController enemyController;

    void Start()
    {
        enemyController = GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            enemyController.IsInAttackRange.Value = true;
            enemyController.GetComponent<AIPath>().canMove = false;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Player"))
        {
            enemyController.IsInAttackRange.Value = false;
            enemyController.GetComponent<AIPath>().canMove = true;
        }
    }
}
