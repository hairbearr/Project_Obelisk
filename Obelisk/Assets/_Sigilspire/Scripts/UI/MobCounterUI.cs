using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MobCounterUI : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI counterText;

    private NetworkVariable<int> enemiesKilled = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<int> totalEnemies = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Delay counting so all enemies have time to spawn
            StartCoroutine(CountAfterDelay());
        }

        enemiesKilled.OnValueChanged += OnCountChanged;
        totalEnemies.OnValueChanged += OnCountChanged;

        UpdateDisplay();
    }

    public override void OnNetworkDespawn()
    {
        enemiesKilled.OnValueChanged -= OnCountChanged;
        totalEnemies.OnValueChanged -= OnCountChanged;
    }

    private System.Collections.IEnumerator CountAfterDelay()
    {
        // Wait for end of frame so all spawners finish
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // Extra safety

        CountTotalEnemies();
    }

    private void CountTotalEnemies()
    {
        if (!IsServer) return;

        var enemies = FindObjectsByType<Enemy.EnemyHealth>(FindObjectsSortMode.None);
        totalEnemies.Value = enemies.Length;

        Debug.Log($"[MobCounter] Counted {totalEnemies.Value} total enemies.");
    }

    public void ServerIncrementKills()
    {
        if (!IsServer) return;
        enemiesKilled.Value++;

        CheckCompletion();
    }

    public void CheckCompletion()
    {
        if (!IsServer) return;

        float completion = totalEnemies.Value > 0 ? (float)enemiesKilled.Value / totalEnemies.Value : 0f;

        if(completion >= 1f)
        {
            Debug.Log("[MobCounter] All enemies defeated!");
            // TODO: Trigger Victory or Unlock Boss? Probably not.
        }
    }

    private void OnCountChanged(int oldVal, int newVal)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (counterText == null) return;

        counterText.text = $"Enemies: {enemiesKilled.Value}/{totalEnemies.Value}";
    }

    [ContextMenu("Recount Enemies")]
    public void RecountEnemies()
    {
        if (!IsServer) return;
        CountTotalEnemies();
    }
}
