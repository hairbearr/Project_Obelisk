using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Enemy
{
    public class EnemySpawner : NetworkBehaviour
    {
        [Header("Spawning")]
        [SerializeField] private NetworkObject enemyPrefab;
        [SerializeField] private int initialSpawnCount = 3;
        [SerializeField] private float spawnRadius = 5f;

        [Header("Reset Behavior")]
        [Tooltip("If true, keeps track of enemies this spawner created so it can despawn them on reset.")]
        [SerializeField] private bool trackSpawnedEnemies = true;

        private readonly List<NetworkObject> spawned = new List<NetworkObject>(64);

        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            SpawnWave(initialSpawnCount);
        }

        [ContextMenu("Spawn Enemy (Editor)")]
        public void SpawnEnemy()
        {
            if (!IsServer) return;
            SpawnOne();
        }

        public void SpawnWave(int count)
        {
            if (!IsServer) return;
            for (int i = 0; i < count; i++)
                SpawnOne();
        }

        public void ServerResetSpawner()
        {
            if (!IsServer) return;

            // 1) Despawn everything we spawned (if we tracked it)
            if (trackSpawnedEnemies)
            {
                for (int i = spawned.Count - 1; i >= 0; i--)
                {
                    var no = spawned[i];
                    if (no == null) { spawned.RemoveAt(i); continue; }

                    if (no.IsSpawned)
                        no.Despawn(true);

                    spawned.RemoveAt(i);
                }
            }
            else
            {
                // Fallback: if not tracking, do nothing here.
                // Your debug script can still kill by tag/layer.
            }

            // 2) Spawn fresh
            SpawnWave(initialSpawnCount);
        }

        private void SpawnOne()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning("EnemySpawner has no enemyPrefab assigned.", this);
                return;
            }

            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, 0f);

            NetworkObject enemyInstance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemyInstance.Spawn(true);

            if (trackSpawnedEnemies)
                spawned.Add(enemyInstance);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.25f, 0.25f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            Handles.Label(
                transform.position + Vector3.up * (spawnRadius + 0.25f),
                $"Spawn Count: {initialSpawnCount}"
            );
        }
#endif

    }
}

