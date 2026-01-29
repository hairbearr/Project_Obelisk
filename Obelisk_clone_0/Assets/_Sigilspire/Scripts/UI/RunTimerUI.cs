using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RunTimerUI : NetworkBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timer Settings")]
    [SerializeField] private float runDurationSeconds = 300f; // 5 minutes default
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private float warningThreshold = 60f; // last minute
    [SerializeField] private float dangerThreshold = 30f; // last 30 seconds

    private NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> timerActive = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining.Value = runDurationSeconds;
            timerActive.Value = true;
        }

        timeRemaining.OnValueChanged += OnTimeChanged;
        UpdateDisplay();
    }

    public override void OnNetworkDespawn()
    {
        timeRemaining.OnValueChanged -= OnTimeChanged;
    }

    private void Update()
    {
        if (!IsServer || !timerActive.Value) return;

        timeRemaining.Value -= Time.deltaTime;

        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;
            timerActive.Value = false;
            OnTimerExpired();
        }
    }

    private void OnTimeChanged(float oldVal, float newVal)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (timerText == null) return;

        float time = timeRemaining.Value;
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timerText.text = $"Time: {minutes:00}:{seconds:00}";

        // Color coding
        if (time <= dangerThreshold)
            timerText.color = dangerColor;
        else if (time <= warningThreshold)
            timerText.color = warningColor;
        else
            timerText.color = normalColor;
    }

    private void OnTimerExpired()
    {
        if (!IsServer) return;

        timerActive.Value = false;

        var runManager = FindFirstObjectByType<RunManager>();
        if (runManager != null)
        {
            runManager.ServerNotifyTimeExpired();
        }
        else
        {
            Debug.Log("[Timer] Run failed - time expired!");
        }

            
        // TODO: Trigger defeat screen
    }

    // Utility methods for future use
    [ContextMenu("Add 30 Seconds (Server)")]
    public void ServerAddTime()
    {
        if (!IsServer) return;
        timeRemaining.Value += 30f;
    }

    public void ServerStopTimer()
    {
        if (!IsServer) return;
        timerActive.Value = false;
    }

    public void ServerStartTimer(float duration)
    {
        if (!IsServer) return;
        timeRemaining.Value = duration;
        timerActive.Value = true;
    }

    public float GetTimeRemaining() => timeRemaining.Value;
    public bool IsActive() => timerActive.Value;
}
