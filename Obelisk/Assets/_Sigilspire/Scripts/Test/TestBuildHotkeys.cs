using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using Combat.Health;
using System;
using Enemy;

public class TestBuildHotkeys : MonoBehaviour
{
    [Header("Keys")]
    [SerializeField] private Key resetKey = Key.R;
    [SerializeField] private Key quitKey = Key.Escape;
    [SerializeField] private Key fullHealKey = Key.H;
    [SerializeField] private Key pauseKey = Key.P;

    private bool paused;

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[resetKey].wasPressedThisFrame)
            ResetSceneServer();

        if (kb[fullHealKey].wasPressedThisFrame)
            FullHealLocalPlayerServer();

        if (kb[pauseKey].wasPressedThisFrame)
            TogglePause();

        if (kb[quitKey].wasPressedThisFrame)
            Quit();
    }

    private void MurderEveryone()
    {
        GameObject[] enemiesInScene = GameObject.FindGameObjectsWithTag("Enemy");
        foreach(GameObject obj in enemiesInScene)
        {
            obj.GetComponent<EnemyHealth>().TakeDamage(1000f);
        }
    }

    private void TogglePause()
    {
        paused = !paused;
        Time.timeScale = paused ? 0f : 1f;
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResetSceneServer()
    {
        // Only server should reload networked scene.
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                SceneManager.GetActiveScene().name,
                LoadSceneMode.Single
            );
        }
        else
        {
            // In a quick local test (no SceneManager), fallback:
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void FullHealLocalPlayerServer()
    {
        if (NetworkManager.Singleton == null) return;

        // Find local player's HealthBase and ask the server to heal it.
        // Simplest approach: just do it directly on server/host.
        if (!NetworkManager.Singleton.IsServer) return;

        var localClientId = NetworkManager.Singleton.LocalClientId;
        var playerNO = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(localClientId);
        if (playerNO == null) return;

        var hb = playerNO.GetComponentInChildren<HealthBase>();
        if (hb == null) return;

        hb.ServerSetFullHealth();
    }
}
