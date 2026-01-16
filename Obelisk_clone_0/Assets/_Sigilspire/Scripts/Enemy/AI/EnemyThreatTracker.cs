using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

public class EnemyThreatTracker : NetworkBehaviour, IThreatReceiver
{
    // How much higher a new target's threat must be (multiplier) before the enemy will switch targets
    [SerializeField] private float aggroThreshold = 1.2f; // Only switch targets if the new target has 20% more aggro than the current one
    [SerializeField] private float threatDecayPerSecond = 0f;

    private readonly Dictionary<ulong, float> threat = new();
    private ulong currentTargetId;

    public float AggroThreshold => aggroThreshold;

    public void AddThreat(ulong sourceNetworkObjectId, float amount)
    {
        if (!IsServer) return;
        if (amount <= 0f) return;

        threat.TryGetValue(sourceNetworkObjectId, out float cur);
        threat[sourceNetworkObjectId] = cur + amount;
    }

    public ulong GetTargetId() => currentTargetId;

    public ulong PickBestTargetId(ulong existing, List<ulong> candidates)
    {
        ulong bestId = existing;
        float bestValue = GetThreat(existing);

        foreach (ulong id in candidates)
        {
            float val = GetThreat(id);

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
            if(kvp.Value > best)
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


    // Update is called once per frame
    void Update()
    {
        if(!IsServer) return;
        if (threatDecayPerSecond <= 0f) return;

        // Decay
        var keys = new List<ulong>(threat.Keys);
        foreach(var k in keys)
        {
            threat[k] = Mathf.Max(0f, threat[k] - threatDecayPerSecond * Time.deltaTime);
            if (threat[k] <= 0)
            {
                threat.Remove(k);
                if(currentTargetId == k) currentTargetId = 0;
            }
        }
    }
}
