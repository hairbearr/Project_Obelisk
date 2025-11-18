using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Combat.AbilitySystem
{
    /// <summary>
    /// Per-player sigil progression store.
    /// Tracks which sigils are unlocked and their XP/level.
    /// </summary>
    public class SigilInventory : MonoBehaviour
    {
        [SerializeField] private List<SigilProgressData> sigils = new List<SigilProgressData>();

        // In a real implementation you'd probably have a database of all sigils
        // and link by ID -> SigilDefinition via some registry.
        [SerializeField] private List<SigilDefinition> knownSigilDefinitions = new List<SigilDefinition>();

        public SigilProgressData GetOrCreateProgress(string sigilId)
        {
            var existing = sigils.FirstOrDefault(s => s.sigilId == sigilId);
            if (existing != null) return existing;

            var newData = new SigilProgressData(sigilId);
            sigils.Add(newData);
            return newData;
        }

        public SigilDefinition GetDefinition(string sigilId)
        {
            return knownSigilDefinitions.FirstOrDefault(s => s.id == sigilId);
        }

        public void AddXp(string sigilId, int amount)
        {
            var progress = GetOrCreateProgress(sigilId);
            var def = GetDefinition(sigilId);
            if (def == null) return;

            progress.currentXp += amount;

            // Level up loop
            while (progress.level < def.maxLevel)
            {
                float xpRequired = def.GetXpRequiredForLevel(progress.level);
                if (progress.currentXp < xpRequired) break;

                progress.currentXp -= (int)xpRequired;
                progress.level++;
                // TODO: notify UI / player about level up
            }
        }
    }
}

