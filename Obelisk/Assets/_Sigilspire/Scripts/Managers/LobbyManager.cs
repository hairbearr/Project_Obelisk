using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float countdownDuration = 30f;
    [SerializeField] private string spireSceneName = "Spire";

    private NetworkList<ulong> readyPlayers;

    private NetworkVariable<float> countdownTimeRemaining = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> countdownActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event System.Action<ulong, bool> OnPlayerReadyChanged;
    public event System.Action OnCountdownStarted;
    public event System.Action OnCountdownCancelled;
    public event System.Action<float> OnCountdownTick;

    public bool IsPlayerReady(ulong clientId) => readyPlayers.Contains(clientId);
    public int ReadyCount => readyPlayers.Count;
    public bool IsCountdownActive => countdownActive.Value;
    public float CountdownTimeRemaining => countdownTimeRemaining.Value;
    public bool AllPlayersReady() => readyPlayers.Count >= NetworkManager.ConnectedClientsIds.Count && NetworkManager.ConnectedClientsIds.Count > 0;

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        readyPlayers = new NetworkList<ulong>(new List<ulong>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    }

    private void Update()
    {
        if (!IsServer || !countdownActive.Value) return;

        countdownTimeRemaining.Value -= Time.deltaTime;

        if (countdownTimeRemaining.Value <= 0f)
        {
            countdownTimeRemaining.Value = 0f;
            countdownActive.Value = false;
            ServerStartRun();
        }
    }
    #endregion

    #region Network Lifecycle
    public override void OnNetworkSpawn()
    {
        countdownActive.OnValueChanged += OnCountdownActiveChanged;
        countdownTimeRemaining.OnValueChanged += OnCountdownTimeChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkDespawn()
    {
        countdownActive.OnValueChanged -= OnCountdownActiveChanged;
        countdownTimeRemaining.OnValueChanged -= OnCountdownTimeChanged;

        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
    #endregion

    #region Server Callbacks
    private void OnClientConnected(ulong clientId)
    {
        // Nothing Yet
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (readyPlayers.Contains(clientId)) readyPlayers.Remove(clientId);

        if (countdownActive.Value) ServerCancelCountdown();
    }
    #endregion

    #region ServerRpcs
    [ServerRpc]
    public void SetReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (isReady)
        {
            if (!readyPlayers.Contains(clientId)) readyPlayers.Add(clientId);
        }
        else
        {
            if (readyPlayers.Contains(clientId)) readyPlayers.Remove(clientId);
        }

        NotifyReadyChangedClientRpc(clientId, isReady);
    }

    [ServerRpc]
    public void StartCountdownServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
        if (countdownActive.Value) return;

        countdownTimeRemaining.Value = countdownDuration;
        countdownActive.Value = true;
    }

    [ServerRpc]
    public void CancelCountdownServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
        ServerCancelCountdown();
    }
    #endregion

    #region ClientRpc
    [ClientRpc]
    private void NotifyReadyChangedClientRpc(ulong clientId, bool isReady)
    {
        OnPlayerReadyChanged?.Invoke(clientId, isReady);
    }
    #endregion

    #region NetworkVariable Callbacks
    private void OnCountdownActiveChanged(bool oldVal, bool newVal)
    {
        if (newVal) OnCountdownStarted?.Invoke();
        else OnCountdownCancelled?.Invoke();
    }

    private void OnCountdownTimeChanged(float oldVal, float newVal)
    {
        OnCountdownTick?.Invoke(newVal);
    }
    #endregion

    #region Server Helpers
    private void ServerCancelCountdown()
    {
        if (!countdownActive.Value) return;
        countdownActive.Value = false;
        countdownTimeRemaining.Value = 0f;
    }

    private void ServerStartRun()
    {
        if (!IsServer) return;
        NetworkManager.SceneManager.LoadScene(spireSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
    #endregion
}
