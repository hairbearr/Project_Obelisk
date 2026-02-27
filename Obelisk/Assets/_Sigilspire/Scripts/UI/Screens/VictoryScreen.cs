using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI deathsText;
    [SerializeField] private TextMeshProUGUI deathPenaltyTotalText;

    private void Awake()
    {
        if(victoryPanel != null) victoryPanel.SetActive(false);
    }

    public void ShowVictory(float timeRemaining, int enemiesKilled, int totalEnemies, int deaths)
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = "VICTORY!";
            resultText.color = Color.green;
        }

        if(statsText != null)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);

            float percentage = totalEnemies > 0 ? (float)enemiesKilled / totalEnemies * 100f : 0f;

            statsText.text = $"Time Remaining: {minutes:00}:{seconds:00}\n" +
                       $"Enemies Defeated: {enemiesKilled}/{totalEnemies} ({percentage:F1}%)";
        }

        if (deathsText != null) 
        {
            deathsText.text = ($"Deaths: {deaths}");
            deathPenaltyTotalText.text = ($"Penalty: {(FindFirstObjectByType<RunManager>().GetDeathPenaltySeconds())*deaths}s");
        }
    }

    public void ShowDefeat(string reason, int enemiesKilled, int totalEnemies, int deaths)
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);

        if(resultText != null)
        {
            resultText.text = "DEFEAT";
            resultText.color = Color.red;
        }

        if (statsText != null)
        {
            float percentage = totalEnemies > 0
                ? (float)enemiesKilled / totalEnemies * 100f
                : 0f;

            statsText.text = $"Reason: {reason}\n" +
                           $"Enemies Defeated: {enemiesKilled}/{totalEnemies} ({percentage:F1}%)";
        }

        if(deathsText != null)
        {
            deathsText.text = ($"Deaths: {deaths}");
            deathPenaltyTotalText.text = ($"Penalty: {(FindFirstObjectByType<RunManager>().GetDeathPenaltySeconds()) * deaths}s");
        }
    }

    public void OnRetryButton()
    {
        // reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnQuitButton()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
