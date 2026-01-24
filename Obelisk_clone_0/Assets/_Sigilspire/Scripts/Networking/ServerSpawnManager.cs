using Net;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ServerSpawnManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private bool useRoundRobin = true;

    private readonly List<Transform> spawnPoints = new();
    private int nextIndex = 0;

    private void Awake()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        // Cache once for editor play convenience (single-player feel).
        CacheSpawnPoints();
    }

    private void OnEnable()
    {
        if (networkManager == null) return;

        networkManager.OnClientConnectedCallback += OnClientConnected;

        // OPTIONAL but recommended:
        // If you later use NGO SceneManager, this ensures spawn points refresh on scene loads.
        if (networkManager.SceneManager != null)
            networkManager.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
    }

    private void OnDisable()
    {
        if (networkManager == null) return;

        networkManager.OnClientConnectedCallback -= OnClientConnected;

        if (networkManager.SceneManager != null)
            networkManager.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
    }

    private void OnLoadEventCompleted(
        string sceneName,
        UnityEngine.SceneManagement.LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        // Only the server should manage spawn points.
        if (!networkManager.IsServer) return;

        CacheSpawnPoints();
        nextIndex = 0;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!networkManager.IsServer) return;

        // If host/server started and spawn points were placed in-scene,
        // cache them now (covers cases where Awake happened before they existed).
        if (spawnPoints.Count == 0)
            CacheSpawnPoints();

        StartCoroutine(WaitForPlayerAndPlace(clientId));
    }

    private IEnumerator WaitForPlayerAndPlace(ulong clientId)
    {
        NetworkObject playerNO = null;

        // Wait until NGO has created the player object for this client.
        while (playerNO == null)
        {
            // NOTE: In some NGO versions, GetPlayerNetworkObject can throw if not ready.
            // So we guard via TryGetValue if needed, but this usually works.
            playerNO = networkManager.SpawnManager.GetPlayerNetworkObject(clientId);
            yield return null;
        }

        Transform spawn = GetSpawnForClient(clientId);

        // Teleport is fine for a test harness. If you later add Rigidbody2D prediction,
        // you may also want to zero velocity here.
        playerNO.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
    }

    private void CacheSpawnPoints()
    {
        spawnPoints.Clear();

        var found = FindObjectsByType<PlayerSpawnPoint>(FindObjectsSortMode.None)
            .OrderBy(s => s.index)
            .Select(s => s.transform);

        spawnPoints.AddRange(found);

        if (spawnPoints.Count == 0)
            Debug.LogWarning("ServerSpawnManager: No PlayerSpawnPoint objects found in scene!", this);
    }

    private Transform GetSpawnForClient(ulong clientId)
    {
        if (spawnPoints.Count == 0) return transform;

        if (!useRoundRobin)
        {
            int idx = (int)(clientId % (ulong)spawnPoints.Count);
            return spawnPoints[idx];
        }

        Transform t = spawnPoints[nextIndex % spawnPoints.Count];
        nextIndex++;
        return t;
    }
}
