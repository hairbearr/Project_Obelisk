using System.Collections;
using UnityEngine;
using Pathfinding;

namespace Sigilspire.Enemy
{
    public class EnemyAggroRangeCheck : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyController;
        [SerializeField] private float returnDelay = 10f;

        private Coroutine returnRoutine;

        private void Start()
        {
            enemyController = GetComponentInParent<EnemyController>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                enemyController.SetAggroTarget(collision.transform);

                if (returnRoutine != null)
                {
                    enemyController.StopCoroutine(returnRoutine);
                    returnRoutine = null;
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (returnRoutine == null)
                {
                    returnRoutine = enemyController.StartCoroutine(DelayedReturn(returnDelay, collision.transform));
                }
            }
        }

        private IEnumerator DelayedReturn(float delay, Transform target)
        {
            yield return new WaitForSeconds(delay);

            if (!enemyController.IsInAggroRange.Value && !enemyController.IsDead)
            {
                enemyController.ClearAggroTarget(target);
            }

            returnRoutine = null;
        }
    }
}


