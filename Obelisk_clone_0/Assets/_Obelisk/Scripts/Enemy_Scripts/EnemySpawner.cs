using UnityEngine;
using Unity.Netcode;
using System.Collections;
using NUnit.Framework;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private Transform enemyPrefab, enemy;
    [SerializeField] private float spawnCooldown;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
            return;
        }

        StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        while (true)
        {
            if (enemy == null)
            {
                Transform enemyTransform = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
                spawnCooldown = 60;
                enemyTransform.GetComponent<EnemyController>().enemySpawner = this;
                enemy = enemyTransform;
                yield return new WaitForSeconds(spawnCooldown);
            }
            yield return null;
        }
    }

}
