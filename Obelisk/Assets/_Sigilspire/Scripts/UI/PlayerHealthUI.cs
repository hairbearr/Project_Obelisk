using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Combat.Health;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Optional: leave empty to auto-bind to local player")]
    [SerializeField] private HealthBase playerHealth;

    private Coroutine bindRoutine;

    private void Awake()
    {
        if (hpText == null)
            hpText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        // If you manually assigned playerHealth in the inspector, bind immediately.
        if (playerHealth != null)
        {
            Bind(playerHealth);
            return;
        }

        // Otherwise, wait for the local player object to spawn.
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

    private IEnumerator BindWhenLocalPlayerSpawns()
    {
        // Wait until Netcode exists and is running
        while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            yield return null;

        // Wait until the local player's NetworkObject exists
        NetworkObject localPlayerNO = null;
        while (localPlayerNO == null)
        {
            // LocalClient.PlayerObject is usually the most reliable for “my player”
            localPlayerNO = NetworkManager.Singleton.LocalClient?.PlayerObject;

            // Fallback if needed (rare)
            if (localPlayerNO == null)
                localPlayerNO = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();

            yield return null;
        }

        // Find HealthBase on the player (or in children if you keep health on a child)
        var hb = localPlayerNO.GetComponent<HealthBase>();
        if (hb == null)
            hb = localPlayerNO.GetComponentInChildren<HealthBase>();

        if (hb == null)
        {
            Debug.LogWarning("[PlayerHealthUI] Local player spawned but no HealthBase found.");
            yield break;
        }

        Bind(hb);
        bindRoutine = null;
    }

    private void Bind(HealthBase hb)
    {
        // If we were bound to something else, unbind first
        Unbind();

        playerHealth = hb;

        // Safety: only show for the owner player
        var no = playerHealth.GetComponent<NetworkObject>();
        if (no != null && !no.IsOwner)
        {
            gameObject.SetActive(false);
            return;
        }

        playerHealth.CurrentHealth.OnValueChanged += OnHealthChanged;
        UpdateText(); // immediate refresh
    }

    private void Unbind()
    {
        if (playerHealth != null)
        {
            playerHealth.CurrentHealth.OnValueChanged -= OnHealthChanged;
            playerHealth = null;
        }
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        UpdateText();
    }

    private void UpdateText()
    {
        if (playerHealth == null || hpText == null) return;

        float current = playerHealth.CurrentHealth.Value;
        float max = playerHealth.MaxHealth;

        hpText.text = $"HP: {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}


