using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI statsText;

    private RunManager runManager;

    private void Awake()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        runManager = FindFirstObjectByType<RunManager>();
    }

    public void ShowVictory(float timeRemaining, int enemiesKilled, int totalEnemies, int deaths)
    {
        // Play Victory Sound
        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayVictory();
        }

        if (victoryPanel != null) victoryPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = "VICTORY!";
            resultText.color = Color.green;
        }

        if (statsText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            float percentage = totalEnemies > 0 ? (float)enemiesKilled / totalEnemies * 100f : 0f;

            // Calculate death penalty
            float totalPenalty = runManager != null ? runManager.GetDeathPenaltySeconds() * deaths : 0f;
            string penaltyFormatted = FormatTime(totalPenalty);

            statsText.text = $"Time Remaining: {minutes:00}:{seconds:00}\n" +
                           $"Enemies Defeated: {enemiesKilled}/{totalEnemies} ({percentage:F1}%)\n" +
                           $"Deaths: {deaths} (Penalty: -{penaltyFormatted})";
        }
    }

    public void ShowDefeat(string reason, int enemiesKilled, int totalEnemies, int deaths)
    {
        if(AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayDefeat();
            AudioManager.Instance.StopMusic();
        }

        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = "DEFEAT";
            resultText.color = Color.red;
        }

        if (statsText != null)
        {
            float percentage = totalEnemies > 0
                ? (float)enemiesKilled / totalEnemies * 100f
                : 0f;

            // Calculate death penalty
            float totalPenalty = runManager != null ? runManager.GetDeathPenaltySeconds() * deaths : 0f;
            string penaltyFormatted = FormatTime(totalPenalty);

            statsText.text = $"Reason: {reason}\n" +
                           $"Enemies Defeated: {enemiesKilled}/{totalEnemies} ({percentage:F1}%)\n" +
                           $"Deaths: {deaths} (Penalty: -{penaltyFormatted})";
        }
    }

    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int secs = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{secs:00}";
    }

    public void OnRetryButton()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitButton()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
