using Pathfinding;
using System.Collections;
using UnityEngine;

public class EnemyAggroCheck : MonoBehaviour
{
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private float waitTime = 10f;

    private Coroutine returnRoutine;

    void Start()
    {
        enemyController = GetComponentInParent<EnemyController>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            print(collision + " is in Aggro Range of " + enemyController.name);
            enemyController.IsInAggroRange = true;
            enemyController.GetComponent<AIDestinationSetter>().target = collision.transform;
            enemyController.IsReturningToStartPoint = false;

            // Cancel return coroutine if running
            if (returnRoutine != null)
            {
                enemyController.StopCoroutine(returnRoutine);
                returnRoutine = null;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision != null && collision.gameObject.CompareTag("Player"))
        {
            print(collision + " is out of Aggro Range of " + enemyController.name);
            enemyController.IsInAggroRange = false;

            // Start delayed return coroutine
            if (returnRoutine == null)
            {
                returnRoutine = enemyController.StartCoroutine(DelayedReturn(waitTime));
            }
        }
    }

    private IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!enemyController.IsInAggroRange && !enemyController.IsDead)
        {
            enemyController.IsReturningToStartPoint = true;
            enemyController.GetComponent<AIDestinationSetter>().target = enemyController.StartPosition;
        }

        returnRoutine = null;
    }
}
