using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

public class EnemyThreatTracker : NetworkBehaviour, IThreatReceiver
{
    [Header("Threat Rules")]
    [Tooltip("New target must have this much more threat (multiplier) to force a swap.")]
    [SerializeField] private float aggroThreshold = 1.2f;

    [Tooltip("Threat decays by this amount per second (0 = no decay).")]
    [SerializeField] private float threatDecayPerSecond = 0f;

    private readonly Dictionary<ulong, float> threat = new();
    private readonly List<ulong> _keysBuffer = new(16);

    private ulong currentTargetId;

    public float AggroThreshold => aggroThreshold;

    public ulong GetTargetId() => currentTargetId;

    public void SetCurrentTargetId(ulong id)
    {
        if (!IsServer) return;
        currentTargetId = id;
    }

    public void AddThreat(ulong sourceNetworkObjectId, float amount)
    {
        if (!IsServer) return;
        if (amount <= 0f) return;

        threat.TryGetValue(sourceNetworkObjectId, out float cur);
        threat[sourceNetworkObjectId] = cur + amount;
    }

    public void SetTargetToBest(ulong existing, List<ulong> candidates)
    {
        if (!IsServer) return;

        // Prune invalid candidates & stale keys before picking
        PruneInvalidThreat();

        currentTargetId = PickBestTargetId(existing, candidates);
    }

    public ulong PickBestTargetId(ulong existing, List<ulong> candidates)
    {
        ulong bestId = existing;
        float bestValue = GetThreat(existing);

        for (int i = 0; i < candidates.Count; i++)
        {
            ulong id = candidates[i];
            float val = GetThreat(id);

            // Hysteresis rule: only switch if new > current * threshold
            if (bestId == 0 || val > bestValue * aggroThreshold)
            {
                bestId = id;
                bestValue = val;
            }
        }

        return bestId;
    }

    public ulong GetTopThreatTargetId()
    {
        ulong bestId = 0;
        float best = 0f;

        foreach (var kvp in threat)
        {
            if (kvp.Value > best)
            {
                best = kvp.Value;
                bestId = kvp.Key;
            }
        }

        return bestId;
    }

    public float GetThreat(ulong targetNetworkObjectId)
    {
        if (targetNetworkObjectId == 0) return 0f;
        return threat.TryGetValue(targetNetworkObjectId, out float v) ? v : 0f;
    }

    public void RemoveThreat(ulong id)
    {
        if (!IsServer) return;

        threat.Remove(id);
        if (currentTargetId == id) currentTargetId = 0;
    }

    public void ClearAllThreat()
    {
        if (!IsServer) return;

        threat.Clear();
        currentTargetId = 0;

        Debug.Log("[ThreatTracker] All threat cleared.");
    }




    /// Server-only: remove threat entries whose NetworkObjects no longer exist (despawn/disconnect).
    /// Also clears currentTargetId if it's invalid.
    /// Call this before choosing targets or occasionally in Update.
    public void PruneInvalidThreat()
    {
        if (!IsServer) return;
        if (NetworkManager == null || NetworkManager.SpawnManager == null) return;

        var spawned = NetworkManager.SpawnManager.SpawnedObjects;

        _keysBuffer.Clear();
        foreach (var kvp in threat)
            _keysBuffer.Add(kvp.Key);

        for (int i = 0; i < _keysBuffer.Count; i++)
        {
            ulong id = _keysBuffer[i];
            if (!spawned.ContainsKey(id))
            {
                threat.Remove(id);
                if (currentTargetId == id) currentTargetId = 0;
            }
        }

        if (currentTargetId != 0 && !spawned.ContainsKey(currentTargetId))
            currentTargetId = 0;

        if(currentTargetId != 0 && !threat.ContainsKey(currentTargetId))
        {
            currentTargetId = 0;
        }
    }

    /// Server-only: prints current threat table for debugging.
    public void DebugThreatDump(string label = "ThreatDump")
    {
        if (!IsServer) return;

        var sb = new StringBuilder(256);
        sb.Append('[').Append(label).Append("] currentTargetId=").Append(currentTargetId).Append(" | ");

        foreach (var kvp in threat)
        {
            sb.Append(kvp.Key).Append('=').Append(kvp.Value.ToString("0.##")).Append(" ");
        }

        Debug.Log(sb.ToString());
    }

    private void Update()
    {
        if (!IsServer) return;

        // Keeping the table clean is cheap and prevents ghost targets
        PruneInvalidThreat();

        if (threatDecayPerSecond <= 0f) return;

        _keysBuffer.Clear();
        foreach (var kvp in threat)
            _keysBuffer.Add(kvp.Key);

        for (int i = 0; i < _keysBuffer.Count; i++)
        {
            ulong k = _keysBuffer[i];

            float newVal = Mathf.Max(0f, threat[k] - threatDecayPerSecond * Time.deltaTime);
            if (newVal <= 0f)
            {
                threat.Remove(k);
                if (currentTargetId == k) currentTargetId = 0;
            }
            else
            {
                threat[k] = newVal;
            }
        }
    }
}

