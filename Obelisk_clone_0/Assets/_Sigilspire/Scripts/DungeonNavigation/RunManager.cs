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
    [SerializeField] private float deathPenaltySeconds = 10f;

    [Header("UI")]
    [SerializeField] private VictoryScreen victoryScreen;

    private NetworkVariable<bool> bossDefeated = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> runComplete = new NetworkVariable<bool>(false);
    private NetworkVariable<int> playerDeaths = new NetworkVariable<int>(0);

    public float GetRequiredMobPercentage() => requiredMobPercentage;
    public int GetPlayerDeaths() => playerDeaths.Value;
    public float GetDeathPenaltySeconds() => deathPenaltySeconds;

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
        int deaths = playerDeaths.Value;

        if (victory)
        {
            ShowVictoryClientRpc(timeRemaining, killed, required, deaths);
        }
        else
        {
            ShowDefeatClientRpc("Time Expired", killed, required, deaths);
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
    private void ShowVictoryClientRpc(float timeRemaining, int killed, int total, int deaths)
    {
        if(victoryScreen != null) { victoryScreen.ShowVictory(timeRemaining, killed, total, deaths); }
    }

    [ClientRpc]
    private void ShowDefeatClientRpc(string reason, int killed, int total, int deaths)
    {
        if(victoryScreen!= null)
        {
            victoryScreen.ShowDefeat(reason, killed, total, deaths);
        }
    }

    [ClientRpc]
    private void WarnInsufficientMobsClientRpc()
    {
        int killed = mobCounter != null ? mobCounter.EnemiesKilled : 0;
        int total = mobCounter != null ? mobCounter.TotalEnemies : 0;
        int needed = Mathf.CeilToInt(total * requiredMobPercentage);

    }

    public void ServerNotifyTimeExpired()
    {
        if (!IsServer) return;
        CompleteRun(false);
    }

    public void ServerNotifyPlayerDeath()
    {
        if (!IsServer) return;
        playerDeaths.Value++;
        Debug.Log($"[RunManager] Player died! Todal deaths: {playerDeaths.Value}");

        var timer = FindFirstObjectByType<RunTimerUI>();
        if( timer != null)
        {
            timer.ServerAddPenalty(deathPenaltySeconds);
            Debug.Log($"RunManager] Applied {deathPenaltySeconds}s death penalty");
        }
    }
}
