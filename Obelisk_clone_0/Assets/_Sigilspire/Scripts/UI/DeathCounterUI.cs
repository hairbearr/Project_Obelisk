using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DeathCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI deathText;

    private RunManager runManager;

    private void Start()
    {
        runManager = FindFirstObjectByType<RunManager>();
    }

    private void Update()
    {
        if (runManager == null || deathText == null) return;

        int deaths = runManager.GetPlayerDeaths();
        float penaltyPerDeath = runManager.GetDeathPenaltySeconds();
        float totalPenalty = deaths * penaltyPerDeath;

        // Format penalty as MM:SS
        string penaltyFormatted = FormatTime(totalPenalty);

        deathText.text = $"Deaths: {deaths} (-{penaltyFormatted})";
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }
}
