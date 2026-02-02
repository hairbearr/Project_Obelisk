using Unity.Netcode;
using UnityEngine;

public class PuzzleManager : NetworkBehaviour
{
    [Header("Puzzle Config")]
    [SerializeField] private PowerNode[] nodes;
    [SerializeField] private RoomDoor doorToUnlock;

    [Header("UI")]
    [SerializeField] private TMPro.TextMeshProUGUI progressText;

    private NetworkVariable<int> nodesDestroyed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        nodesDestroyed.OnValueChanged += OnProgressChanged;
        UpdateDisplay();
    }

    public override void OnNetworkDespawn()
    {
        nodesDestroyed.OnValueChanged -= OnProgressChanged;
    }

    public void ServerNotifyNodeDestroyed()
    {
        if (!IsServer) return;

        nodesDestroyed.Value++;

        Debug.Log($"[PuzzleManager] Nodes destroyed: {nodesDestroyed.Value}/{nodes.Length}");

        // Check if puzzle complete
        if(nodesDestroyed.Value >= nodes.Length)
        {
            CompletePuzzle();
        }
    }

    private void CompletePuzzle()
    {
        if (!IsServer) return;

        Debug.Log("[PuzzleManager] Puzzle complete! Unlocking Door.");

        if(doorToUnlock != null) { doorToUnlock.ServerUnlock(); }

        // play sound, vfx, etc
        PuzzleCompleteClientRpc();
    }

    [ClientRpc]
    private void PuzzleCompleteClientRpc()
    {
        Debug.Log("[PuzzleManager] Puzzle complete (Client)");
        // TODO: Play completion sound/VFX
    }

    private void OnProgressChanged(int oldVal, int newVal)
    {
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if(progressText != null && nodes != null)
        {
            progressText.text = $"Power Nodes: {nodesDestroyed.Value}/{nodes.Length}";
        }
    }
}
