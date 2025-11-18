using UnityEngine;
using Unity.Netcode;

namespace Enemy
{
    /// <summary>
    /// Simple networked enemy spawner skeleton.
    /// Spawns enemies on the server at start.
    /// </summary>
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

            Vector3 offset = Random.insideUnitSphere;
            offset.y = 0f;
            offset = offset.normalized * Random.Range(0f, spawnRadius);

            Vector3 spawnPos = transform.position + offset;
            Quaternion spawnRot = Quaternion.identity;

            NetworkObject enemyInstance = Instantiate(enemyPrefab, spawnPos, spawnRot);
            enemyInstance.Spawn(true);
        }
    }
}

