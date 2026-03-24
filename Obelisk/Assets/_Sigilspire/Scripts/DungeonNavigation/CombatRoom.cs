using Unity.Netcode;
using UnityEngine;

public class CombatRoom : MonoBehaviour
{
    [Header("Room Config")]
    [SerializeField] private RoomDoor[] doorsToUnlock;

    private bool hasCheckedCompletion = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void Update()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null ||
        !Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            return;
        }

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

        // If no spawners, room is clear (no enemies to kill)
        if (spawners.Length == 0) return true;

        // Check each spawner for living enemies
        foreach (var spawner in spawners)
        {
            int livingCount = spawner.GetLivingEnemyCount();

            if (livingCount > 0)
            {
                return false; // Found living enemies, room not clear
            }
        }

        return true;
    }

    private void UnlockDoors()
    {

        foreach (var door in doorsToUnlock)
        {
            if (door != null)
            {
                door.ServerUnlock();
            }
        }
    }
}
