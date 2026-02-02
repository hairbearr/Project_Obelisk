using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Combat;

public class PlayerShieldUI : MonoBehaviour
{
    [SerializeField] private Slider shieldSlider;
    [SerializeField] private TextMeshProUGUI shieldText;

    private ShieldController shield;

    private void OnEnable()
    {
        StartCoroutine(BindWhenLocalPlayerSpawns());
    }

    private void OnDisable()
    {
        Unbind();
    }

    private System.Collections.IEnumerator BindWhenLocalPlayerSpawns()
    {
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) yield return null;

        NetworkObject localPlayerNO = null;
        while (localPlayerNO == null)
        {
            localPlayerNO = NetworkManager.Singleton.LocalClient?.PlayerObject;
            if(localPlayerNO == null)
                localPlayerNO = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            yield return null;
        }

        var sc = localPlayerNO.GetComponentInChildren<ShieldController>();
        if(sc == null)
        {
            yield break;
        }

        Bind(sc);
    }

    private void Bind(ShieldController sc)
    {
        Unbind();
        shield = sc;
        shield.ShieldEnergy.OnValueChanged += OnShieldChanged;
        UpdateDisplay();
    }

    private void Unbind()
    {
        if (shield != null)
        {
            shield.ShieldEnergy.OnValueChanged -= OnShieldChanged;
            shield = null;
        }
    }

    private void OnShieldChanged(float oldValue, float newValue)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (shield == null) return;

        float current = shield.ShieldEnergy.Value;
        float max = shield.GetEffectiveMaxShieldEnergy();

        // Update slider
        if (shieldSlider != null && max > 0f)
            shieldSlider.value = current / max;

        // Update Text
        if (shieldText != null)
            shieldText.text = $"{Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
    }
}
