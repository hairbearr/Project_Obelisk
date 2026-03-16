using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationDialog : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirmCallback;
    private Action onCancelCallback;

    private void Awake()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesClicked);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(OnNoClicked);
        }
    }

    public void Show(string title, string message, Action onConfirm, Action onCancel = null)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (messageText != null)
        {
            messageText.text = message;
        }

        onConfirmCallback = onConfirm;
        onCancelCallback = onCancel;

        if (dialogPanel != null)
        {
            dialogPanel.SetActive(true);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }
    }

    public void Hide()
    {
        if (dialogPanel != null)
        {
            dialogPanel.SetActive(false);
        }

        onConfirmCallback = null;
        onCancelCallback = null;
    }

    private void OnYesClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }

        onConfirmCallback?.Invoke();

        Hide();
    }

    private void OnNoClicked()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick();
        }

        onCancelCallback?.Invoke();

        Hide();
    }
}
