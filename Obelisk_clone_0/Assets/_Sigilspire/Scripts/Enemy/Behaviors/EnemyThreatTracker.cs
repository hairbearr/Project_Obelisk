using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

public class EnemyThreatTracker : NetworkBehaviour, IThreatReceiver
{
    [SerializeField] private float switchHysteresis = 1.2f; // 20% higher to switch
    [SerializeField] private float threatDecayPerSecond = 0f;

    private readonly Dictionary<ulong, float> threat = new();
    private ulong currentTargetId;

    public void AddThreat(ulong sourceNetworkObjectId, float amount)
    {
        if (!IsServer) return;
        if (amount <= 0f) return;

        threat.TryGetValue(sourceNetworkObjectId, out float cur);
        threat[sourceNetworkObjectId] = cur + amount;

        // Update target right away (can do this in EnemyAI tick)
        currentTargetId = PickBestTargetId(currentTargetId);
    }

    public ulong GetTargetId() => currentTargetId;

    private ulong PickBestTargetId(ulong existing)
    {
        ulong bestId = existing;
        float bestValue = existing != 0 && threat.TryGetValue(existing, out float eVal) ? eVal : 0f;

        foreach (var kvp in threat)
        {
            ulong id = kvp.Key;
            float val = kvp.Value;

            if (bestId == 0 || val > bestValue * switchHysteresis)
            {
                bestId = id;
                bestValue = val;
            }
        }

        return bestId;
    }



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
            if (threat[k] <= 0) threat.Remove(k);
        }
    }
}
