using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Combat.AbilitySystem;

public class SigilNotificationUI : MonoBehaviour
{
    public static SigilNotificationUI Instance { get; private set; }

    [Header("Toast Prefab")]
    [SerializeField] private GameObject toastPrefab;

    [Header("Layout")]
    [SerializeField] private Transform toastContainer;
    [SerializeField] private float holdDuration = 2.5f;
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private int maxVisible = 3;

    [Header("Colors")]
    [SerializeField] private Color minorColor = Color.cyan;
    [SerializeField] private Color majorColor = new Color(1f, 0.84f, 0f);

    private Queue<ToastData> queue = new Queue<ToastData>();
    private List<GameObject> activeToasts = new List<GameObject>();
    private bool isProcessing = false;

    private struct ToastData
    {
        public string title;
        public string subtitle;
        public Sprite icon;
        public Color accentColor;
    }

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    #endregion

    #region Public API

    public void ShowPickup(SigilDefinition def)
    {
        if (def == null) return;

        bool isMinor = def.sigilType == SigilType.Minor;

        Enqueue(new ToastData
        {
            title = isMinor ? "Sigil Upgraded" : "Sigil Unlocked",
            subtitle = def.displayName,
            icon = def.icon,
            accentColor = isMinor ? minorColor : majorColor
        });
    }

    public void ShowLevelUp(SigilDefinition def, int newLevel)
    {
        if (def == null) return;

        Enqueue(new ToastData
        {
            title = def.displayName,
            subtitle = "Level " + newLevel,
            icon = def.icon,
            accentColor = majorColor
        });
    }

    #endregion

    #region Queue

    private void Enqueue(ToastData data)
    {
        queue.Enqueue(data);

        if (!isProcessing)
            StartCoroutine(ProcessQueue());
    }

    private IEnumerator ProcessQueue()
    {
        isProcessing = true;

        while (queue.Count > 0)
        {
            if (activeToasts.Count >= maxVisible)
            {
                yield return null;
                continue;
            }

            var data = queue.Dequeue();
            StartCoroutine(ShowToast(data));
            yield return new WaitForSeconds(slideDuration * 0.5f);
        }

        isProcessing = false;
    }

    #endregion

    #region Toast Lifecycle

    private IEnumerator ShowToast(ToastData data)
    {
        if (toastPrefab == null || toastContainer == null) yield break;

        var toast = Instantiate(toastPrefab, toastContainer);
        activeToasts.Add(toast);

        var icon = toast.transform.Find("Icon")?.GetComponent<Image>();
        var title = toast.transform.Find("Title")?.GetComponent<TextMeshProUGUI>();
        var subtitle = toast.transform.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        var panel = toast.GetComponent<Image>();

        if (icon != null) { icon.sprite = data.icon; icon.enabled = data.icon != null; }
        if (title != null) title.text = data.title;
        if (subtitle != null) subtitle.text = data.subtitle;
        if (panel != null) panel.color = data.accentColor;

        yield return StartCoroutine(SlideIn(toast));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(SlideOut(toast));

        activeToasts.Remove(toast);
        Destroy(toast);
    }

    #endregion

    #region Animation

    private IEnumerator SlideIn(GameObject toast)
    {
        var rt = toast.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float width = rt.rect.width;
        if (width == 0f) width = 300f;

        Vector2 start = new Vector2(width, rt.anchoredPosition.y);
        Vector2 end = new Vector2(0f, rt.anchoredPosition.y);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            rt.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        rt.anchoredPosition = end;
    }

    private IEnumerator SlideOut(GameObject toast)
    {
        var rt = toast.GetComponent<RectTransform>();
        if (rt == null) yield break;

        float width = rt.rect.width;
        if (width == 0f) width = 300f;

        Vector2 start = rt.anchoredPosition;
        Vector2 end = new Vector2(width, rt.anchoredPosition.y);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            rt.anchoredPosition = Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        rt.anchoredPosition = end;
    }

    #endregion
}
