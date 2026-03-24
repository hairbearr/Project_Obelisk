using UnityEngine;
using UnityEngine.InputSystem;

public class TestBuildHotkeys : MonoBehaviour
{
    [Header("Debug Keys")]
    [SerializeField] private Key fullHealKey = Key.H;
    [SerializeField] private Key killAllKey = Key.K;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[fullHealKey].wasPressedThisFrame)
        {
            FullHealLocalPlayer();
        }

        if (kb[killAllKey].wasPressedThisFrame)
        {
            KillAllEnemies();
        }
    }

    private void FullHealLocalPlayer()
    {
        if (Unity.Netcode.NetworkManager.Singleton == null) return;
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer) return;

        var localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        var playerNO = Unity.Netcode.NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(localClientId);
        if (playerNO == null) return;

        var hb = playerNO.GetComponentInChildren<Combat.Health.HealthBase>();
        if (hb == null) return;

        hb.ServerSetFullHealth();
    }

    private void KillAllEnemies()
    {
        var enemies = FindObjectsByType<Enemy.EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.TakeDamage(9999f, 0);
        }
    }
}
