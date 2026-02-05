using Unity.Netcode;
using UnityEngine;

public class CombatRoom : NetworkBehaviour
{
    [Header("Room Config")]
    [SerializeField] private RoomDoor[] doorsToUnlock;

    private bool hasCheckedCompletion = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) return;
        if (hasCheckedCompletion) return;

        // Check if all enemies in this room are dead
        if (AreAllEnemiesDead())
        {
            hasCheckedCompletion = true;
            UnlockDoors();
        }
    }

    private bool AreAllEnemiesDead()
    {
        // Find all enemy spawners that are children of this room
        var spawners = GetComponentsInChildren<Enemy.EnemySpawner>();


        foreach (var spawner in spawners)
        {
            // Check if any enemies from this spawner are still alive
            var enemies = FindObjectsByType<Enemy.EnemyHealth>(FindObjectsSortMode.None);
            if (enemies.Length <= 0) return true;

            foreach (var enemy in enemies) 
            {
                // if enemy is within this room's bounds (rouch check)
                if(Vector3.Distance(enemy.transform.position, transform.position) < 50f)
                {
                    if(enemy.CurrentHealth.Value > 0)
                    {
                        return false; // still enemies alive
                    }
                }
            }
        }
        return true;
    }

    private void UnlockDoors()
    {

        foreach (var door in doorsToUnlock)
        {
            if(door != null)
            {
                door.ServerUnlock();
            }
        }
    }
}
