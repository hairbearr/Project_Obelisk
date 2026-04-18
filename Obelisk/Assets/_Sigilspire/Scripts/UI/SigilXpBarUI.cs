using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Combat.AbilitySystem;
using Player;

public class SigilXpBarUI : MonoBehaviour
{
    [Header("Sigil Slot")]
    [Tooltip("Which weapon slot this bar tracks")]
    [SerializeField] private WeaponSlot trackedSlot;

    [Header("References")]
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI sigilNameText;
    [SerializeField] private Image iconImage;

    private SigilInventory boundInventory;
    private PlayerLoadout boundLoadout;
    private string trackedSigilId;
    private Coroutine bindRoutine;

    #region Lifecycle

    private void OnEnable()
    {
        bindRoutine = StartCoroutine(BindWhenLocalPlayerSpawns());
    }

    private void OnDisable()
    {
        if (bindRoutine != null)
        {
            StopCoroutine(bindRoutine);
            bindRoutine = null;
        }

        Unbind();
    }

    #endregion

    #region Binding

    private IEnumerator BindWhenLocalPlayerSpawns()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        NetworkObject localPlayerNO = null;
        while (localPlayerNO == null)
        {
            localPlayerNO = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if (localPlayerNO == null)
                localPlayerNO = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            yield return null;
        }

        var inventory = localPlayerNO.GetComponent<SigilInventory>();
        var loadout   = localPlayerNO.GetComponent<PlayerLoadout>();

        if (inventory == null || loadout == null)
        {
            Debug.LogWarning("[SigilXpBarUI] Local player missing SigilInventory or PlayerLoadout.");
            yield break;
        }

        Bind(inventory, loadout);
        bindRoutine = null;
    }

    private void Bind(SigilInventory inventory, PlayerLoadout loadout)
    {
        Unbind();

        boundInventory = inventory;
        boundLoadout   = loadout;

        boundInventory.OnXpChanged    += OnXpChanged;
        boundInventory.OnLevelChanged += OnLevelChanged;

        RefreshAll();
    }

    private void Unbind()
    {
        if (boundInventory != null)
        {
            boundInventory.OnXpChanged    -= OnXpChanged;
            boundInventory.OnLevelChanged -= OnLevelChanged;
            boundInventory = null;
        }

        boundLoadout   = null;
        trackedSigilId = null;
    }

    #endregion

    #region Event Handlers

    private void OnXpChanged(string sigilId, int currentXp, int xpRequired, int level)
    {
        if (sigilId != trackedSigilId) return;
        UpdateBar(currentXp, xpRequired, level);
    }

    private void OnLevelChanged(string sigilId, int newLevel)
    {
        if (sigilId != trackedSigilId) return;
        RefreshAll();
    }

    #endregion

    #region Display

    private void RefreshAll()
    {
        if (boundInventory == null || boundLoadout == null) return;

        trackedSigilId = boundLoadout.GetEquippedMajorId(trackedSlot);

        if (string.IsNullOrEmpty(trackedSigilId))
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        var def      = boundInventory.GetDefinition(trackedSigilId);
        var progress = boundInventory.GetProgress(trackedSigilId);

        if (def == null || progress == null) return;

        if (sigilNameText != null) sigilNameText.text = def.displayName;
        if (iconImage     != null) { iconImage.sprite = def.icon; iconImage.enabled = def.icon != null; }

        int xpReq = (int)def.GetXpRequiredForLevel(progress.level);
        UpdateBar(progress.currentXp, xpReq, progress.level);
    }

    private void UpdateBar(int currentXp, int xpRequired, int level)
    {
        if (levelText != null) levelText.text = "Lv " + level;

        if (xpSlider != null)
            xpSlider.value = xpRequired > 0 ? (float)currentXp / xpRequired : 1f;
    }

    #endregion
}
