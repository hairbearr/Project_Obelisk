using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;

public class RunManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private MobCounterUI mobCounter;
    [SerializeField] private RunTimerUI timer;

    [Header("Victory Conditions")]
    [SerializeField, Range(0f, 1f)] private float requiredMobPercentage = 1.0f; // 100% = kill everything

    [Header("UI")]
    [SerializeField] private VictoryScreen victoryScreen;

    private NetworkVariable<bool> bossDefeated = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> runComplete = new NetworkVariable<bool>(false);

    public float GetRequiredMobPercentage() => requiredMobPercentage;

    void Update()
    {
        if (!IsServer) return;
        if (runComplete.Value) return;

        // Victory = boss dead + mob count met
        if(bossDefeated.Value && IsMobCountMet())
        {
            CompleteRun(true);
        }
    }

    public void ServerNotifyBossDeath()
    {
        if (!IsServer) return;

        bossDefeated.Value = true;

        if (IsMobCountMet())
        {
            CompleteRun(true);
        }
        else
        {
            WarnInsufficientMobsClientRpc();
        }
    }

    private bool IsMobCountMet()
    {
        if(mobCounter == null) return false;

        int total = mobCounter.TotalEnemies;
        int killed = mobCounter.EnemiesKilled;

        if(total == 0) return false;

        float percentage = (float)killed / total;
        return percentage >= requiredMobPercentage;
    }

    private void CompleteRun(bool victory)
    {
        if(runComplete.Value) return;

        runComplete.Value = true;

        if (timer != null) timer.ServerStopTimer();

        // Freeze all players
        FreezePlayersClientRpc();

        //Get stats
        float timeRemaining = timer != null ? timer.GetTimeRemaining() : 0f;
        int killed = mobCounter != null ? mobCounter.EnemiesKilled : 0;
        int total = mobCounter != null ? mobCounter.TotalEnemies : 0;
        int required = Mathf.CeilToInt(total * requiredMobPercentage);

        if (victory)
        {
            Debug.Log("[RunManager] Victory! Boss defeated and mob count met!");
            ShowVictoryClientRpc(timeRemaining, killed, required);
        }
        else
        {
            Debug.Log("[RunManager] Defeat! Timer expired.");
            ShowDefeatClientRpc("Time Expired", killed, required);
        }
    }

    [ClientRpc]
    private void FreezePlayersClientRpc()
    {
        // Freeze local player
        var playerController = FindFirstObjectByType<Player.PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // Freeze rigidbody
        var playerRb = FindFirstObjectByType<Rigidbody2D>();
        if (playerRb != null && playerRb.CompareTag("Player"))
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.simulated = false; // disables physics
        }
    }

    [ClientRpc]
    private void ShowVictoryClientRpc(float timeRemaining, int killed, int total)
    {
        if(victoryScreen != null) { victoryScreen.ShowVictory(timeRemaining, killed, total); }
    }

    [ClientRpc]
    private void ShowDefeatClientRpc(string reason, int killed, int total)
    {
        if(victoryScreen!= null)
        {
            victoryScreen.ShowDefeat(reason, killed, total);
        }
    }

    [ClientRpc]
    private void WarnInsufficientMobsClientRpc()
    {
        int killed = mobCounter != null ? mobCounter.EnemiesKilled : 0;
        int total = mobCounter != null ? mobCounter.TotalEnemies : 0;
        int needed = Mathf.CeilToInt(total * requiredMobPercentage);

        Debug.Log($"[RunManager] Boss defeated, but only {killed}/{needed} enemies killed! Keep clearing!");
    }

    public void ServerNotifyTimeExpired()
    {
        if (!IsServer) return;
        CompleteRun(false);
    }
}
