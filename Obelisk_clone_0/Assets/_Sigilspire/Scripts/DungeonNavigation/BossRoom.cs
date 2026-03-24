using Unity.Netcode;
using UnityEngine;

public class BossRoom : NetworkBehaviour
{
    [Header("Boss Room Config")]
    [SerializeField] private RoomDoor exitDoor; // Door that leads OUT of boss room
    [SerializeField] private BoxCollider2D encounterTrigger; // Trigger zone that starts encounter

    private NetworkVariable<bool> encounterActive = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bossDefeated = new NetworkVariable<bool>(false);
    private bool hasTriggeredEncounter = false;

    private void Start()
    {
        // Make sure trigger is set to trigger mode
        if (encounterTrigger != null)
        {
            encounterTrigger.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (hasTriggeredEncounter) return; // Only trigger once

        // Check if player entered
        if (collision.CompareTag("Player"))
        {
            hasTriggeredEncounter = true;
            StartEncounter();
        }
    }

    private void StartEncounter()
    {
        if (!IsServer) return;

        encounterActive.Value = true;

        // Lock the exit door
        if (exitDoor != null)
        {
            exitDoor.ServerLock();
        }
    }

    /// <summary>
    /// Called by boss's EnemyHealth.OnDeath()
    /// </summary>
    public void ServerNotifyBossDefeated()
    {
        if (!IsServer) return;

        bossDefeated.Value = true;
        encounterActive.Value = false;

        // Unlock the exit door
        if (exitDoor != null)
        {
            exitDoor.ServerUnlock();
        }
    }

    /// <summary>
    /// Called when player dies and run resets
    /// </summary>
    public void ServerResetEncounter()
    {
        if (!IsServer) return;

        encounterActive.Value = false;
        bossDefeated.Value = false;
        hasTriggeredEncounter = false;

        // Unlock door for fresh attempt
        if (exitDoor != null)
        {
            exitDoor.ServerUnlock();
        }
    }
}
