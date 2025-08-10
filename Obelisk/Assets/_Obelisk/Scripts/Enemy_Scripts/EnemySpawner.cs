using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private Transform enemyPrefab, enemy;
    [SerializeField] private float spawnCooldown = 60f , spawnCooldownOG =60f;

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
            yield return new WaitForSeconds(spawnCooldown);

            if (enemy == null)
            {
                Transform enemyTransform = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
                enemyTransform.GetComponent<EnemyController>().enemySpawner = this;
                enemy = enemyTransform;
                enemy.GetComponent<NetworkObject>().Spawn();
                spawnCooldown = spawnCooldownOG;
            }
        }
    }
}
