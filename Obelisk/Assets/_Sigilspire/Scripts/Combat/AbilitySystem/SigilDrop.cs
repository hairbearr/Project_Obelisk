using Combat.AbilitySystem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SigilDrop : NetworkBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color minorColor = Color.cyan;
    [SerializeField] private Color majorColor = new Color(1f, 0.84f, 0f); // gold

    [Header("Pickup")]
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float despawnTimeout = 120f; // Despawn after 2m if not all picked up

    // Server tracks: which sigil each player gets
    private Dictionary<ulong, string> sigilPerPlayer = new Dictionary<ulong, string>();

    // Server tracks: who has picked up
    private HashSet<ulong> playersWhoPickedUp = new HashSet<ulong>();

    // Client tracks: have I picked this up yet?
    private bool localPlayerPickedUp = false;

    private float spawnTime;

    public void Initialize(Dictionary<ulong, string> playerSigilMap)
    {
        if (!IsServer) return;

        sigilPerPlayer = playerSigilMap;
        spawnTime = Time.time;

        // Tell all clients to render their assigned sigil
        InitializeClientsClientRpc(SerializeDictionary(playerSigilMap));
    }

    private void Update()
    {
        if (!IsServer) return;

        // Timeout: despawn if all players have picked up OR time expired
        if (playersWhoPickedUp.Count >= sigilPerPlayer.Count || Time.time - spawnTime >= despawnTimeout)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            return;
        }

        // Check for nearby players who haven't picked up yet
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, playerLayer);

        foreach (var hit in hits)
        {
            var inventory = hit.GetComponentInParent<SigilInventory>();
            if (inventory == null) continue;

            var networkObj = hit.GetComponentInParent<NetworkObject>();
            if (networkObj == null || !networkObj.IsOwner) continue;

            ulong clientId = networkObj.OwnerClientId;

            // Skip if already picked up
            if (playersWhoPickedUp.Contains(clientId)) continue;

            // Skip if no sigil assigned for this player
            if (!sigilPerPlayer.ContainsKey(clientId)) continue;

            // Process pickup
            ServerProcessPickup(clientId, inventory);
        }
    }

    private void ServerProcessPickup(ulong clientId, SigilInventory inventory)
    {
        if (!IsServer) return;
        if (playersWhoPickedUp.Contains(clientId)) return;

        string sigilId = sigilPerPlayer[clientId];
        var def = inventory.GetDefinition(sigilId);

        if (def != null)
        {
            inventory.AddSigilDrop(def);
            playersWhoPickedUp.Add(clientId);

            // Tell that specific client they picked it up
            NotifyPickupClientRpc(def.displayName, def.sigilType == SigilType.Minor, clientId);
        }
    }

    [ClientRpc]
    private void InitializeClientsClientRpc(string serializedMap)
    {
        var playerSigilMap = DeserializeDictionary(serializedMap);

        // Find my client ID
        if (NetworkManager.Singleton == null) return;
        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        // Do I have a sigil assigned?
        if (!playerSigilMap.ContainsKey(myClientId))
        {
            // No drop for me - hide it
            if (sprite != null) sprite.enabled = false;
            return;
        }

        string mySigilId = playerSigilMap[myClientId];

        // Find the sigil definition to render it
        var allLoadouts = FindObjectsByType<Player.PlayerLoadout>(FindObjectsSortMode.None);
        SigilInventory myInventory = null;

        foreach (var loadout in allLoadouts)
        {
            if (loadout.IsOwner)
            {
                myInventory = loadout.GetComponent<SigilInventory>();
                break;
            }
        }

        if (myInventory == null) return;

        var def = myInventory.GetDefinition(mySigilId);
        if (def == null) return;

        // Render my sigil
        if (sprite != null)
        {
            sprite.color = def.sigilType == SigilType.Minor ? minorColor : majorColor;

            if (def.icon != null)
            {
                sprite.sprite = def.icon;
            }
        }
    }

    [ClientRpc]
    private void NotifyPickupClientRpc(string sigilName, bool isMinor, ulong targetClientId)
    {
        // Only the target client reacts
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        localPlayerPickedUp = true;

        // Hide visually for this client
        if (sprite != null) sprite.enabled = false;

        Debug.Log($"[Sigil] Picked up {(isMinor ? "Minor" : "Major")}: {sigilName}");

        // TODO: Show UI notification
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick(); // Temp - add proper pickup sound
        }
    }

    private string SerializeDictionary(Dictionary<ulong, string> dict)
    {
        var entries = new List<string>();
        foreach (var kvp in dict)
        {
            entries.Add($"{kvp.Key}:{kvp.Value}");
        }
        return string.Join("|", entries);
    }

    private Dictionary<ulong, string> DeserializeDictionary(string serialized)
    {
        var dict = new Dictionary<ulong, string>();
        if (string.IsNullOrEmpty(serialized)) return dict;

        string[] entries = serialized.Split('|');
        foreach (string entry in entries)
        {
            string[] parts = entry.Split(':');
            if (parts.Length == 2)
            {
                if (ulong.TryParse(parts[0], out ulong clientId))
                {
                    dict[clientId] = parts[1];
                }
            }
        }
        return dict;
    }
}
