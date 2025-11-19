using UnityEngine;
using Unity.Netcode;

namespace Enemy
{
    public class EnemySpawner : NetworkBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private NetworkObject enemyPrefab;
        [SerializeField] private int initialSpawnCount = 3;
        [SerializeField] private float spawnRadius = 5f;

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            for (int i = 0; i < initialSpawnCount; i++)
            {
                SpawnEnemy();
            }
        }

        [ContextMenu("Spawn Enemy (Editor)")]
        public void SpawnEnemy()
        {
            if (!IsServer) return;
            if (enemyPrefab == null)
            {
                Debug.LogWarning("EnemySpawner has no enemyPrefab assigned.", this);
                return;
            }

            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, 0f);

            NetworkObject enemyInstance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemyInstance.Spawn(true);
        }
    }
}
