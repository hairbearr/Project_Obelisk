using TMPro;
using Unity.Netcode;
using UnityEngine;

public class DeathCounterUI : NetworkBehaviour
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
        deathText.text = $"Deaths: {deaths}";
    }
}
