using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TimerPulse : MonoBehaviour
{
    [SerializeField] private RunTimerUI timer;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minScale = 0.95f;
    [SerializeField] private float maxScale = 1.05f;

    private TextMeshProUGUI text;
    private Vector3 originalScale;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (timer == null) return;

        float timeRemaining = timer.GetTimeRemaining();

        // Only pulse when in danger (under 30s)
        if (timeRemaining > 0 && timeRemaining <= 30f)
        {
            float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            transform.localScale = originalScale * pulse;
        }
        else
        {
            transform.localScale = originalScale;
        }
    }
}
