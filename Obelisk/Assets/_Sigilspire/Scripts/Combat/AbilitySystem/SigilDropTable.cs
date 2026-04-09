using Combat.AbilitySystem;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SigilDropTable", menuName = "Sigilspire/Sigil Drop Table")]
public class SigilDropTable : ScriptableObject
{
    [System.Serializable]
    public class DropEntry
    {
        public SigilDefinition sigil;
        [Range(0f, 100f)] public float dropWeight = 10f; // Higher = more likely
    }

    [Header("Drop Pool")]
    public List<DropEntry> possibleDrops = new List<DropEntry>();

    [Header("Drop Prefab")]
    public GameObject sigilDropPrefab; // Prefab with SigilDrop component

    public SigilDefinition RollDropForPlayer(SigilInventory playerInventory)
    {
        if (possibleDrops.Count == 0) return null;
        if (playerInventory == null) return null;

        // Build a filtered list of valid drops for this player
        List<DropEntry> validDrops = new List<DropEntry>();

        foreach (var entry in possibleDrops)
        {
            if (entry.sigil == null) continue;

            // Check if player can still level this sigil
            var progress = playerInventory.GetProgress(entry.sigil.id);

            if (entry.sigil.sigilType == SigilType.Minor)
            {
                // Minor: skip if already at max level
                if (progress != null && progress.level >= entry.sigil.minorMaxLevel)
                    continue;
            }
            else if (entry.sigil.sigilType == SigilType.Major)
            {
                // Major: skip if already unlocked (Majors don't drop, they unlock at level 1)
                if (progress != null && progress.level >= 1)
                    continue;
            }

            validDrops.Add(entry);
        }

        // No valid drops for this player
        if (validDrops.Count == 0) return null;

        // Weighted random selection
        float totalWeight = 0f;
        foreach (var entry in validDrops)
        {
            totalWeight += entry.dropWeight;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var entry in validDrops)
        {
            cumulative += entry.dropWeight;
            if (roll <= cumulative)
            {
                return entry.sigil;
            }
        }

        // Fallback: return last entry
        return validDrops[validDrops.Count - 1].sigil;
    }
}
