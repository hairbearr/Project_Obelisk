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

        public event System.Action<string, int, int, int> OnXpChanged;
        public event System.Action<string, int> OnLevelChanged;


        public SigilProgressData GetProgress(string sigilId)
        {
            return sigils.FirstOrDefault(s => s.sigilId == sigilId);
        }

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
                var networkObj = GetComponent<Unity.Netcode.NetworkObject>();
                if (networkObj != null && networkObj.IsOwner)
                    SigilNotificationUI.Instance?.ShowLevelUp(def, progress.level);

                OnLevelChanged?.Invoke(sigilId, progress.level);
            }

            int xpReq = (int)def.GetXpRequiredForLevel(progress.level);
            OnXpChanged?.Invoke(sigilId, progress.currentXp, xpReq, progress.level);
        }

        public void AddSigilDrop(SigilDefinition sigil)
        {
            if (sigil == null) return;

            var progress = GetOrCreateProgress(sigil.id);

            if (sigil.sigilType == SigilType.Minor)
            {
                // Minors: level up via duplicates (max 3)
                if (progress.level < sigil.minorMaxLevel)
                {
                    progress.level++;
                    Debug.Log($"[Sigil] {sigil.displayName} upgraded to Level {progress.level}!");

                    var networkObj = GetComponent<Unity.Netcode.NetworkObject>();
                    if (networkObj != null && networkObj.IsOwner)
                        SigilNotificationUI.Instance?.ShowLevelUp(sigil, progress.level);
                }
                else
                {
                    Debug.Log($"[Sigil] {sigil.displayName} already maxed (Level {sigil.minorMaxLevel})");
                }
            }
            else if (sigil.sigilType == SigilType.Major)
            {
                // Majors: unlock at level 1 if new (XP gained through combat)
                if (progress.level < 1)
                {
                    progress.level = 1;
                    Debug.Log($"[Sigil] {sigil.displayName} unlocked!");

                    var networkObj = GetComponent<Unity.Netcode.NetworkObject>();
                    if (networkObj != null && networkObj.IsOwner)
                        SigilNotificationUI.Instance?.ShowPickup(sigil);
                }
                else
                {
                    Debug.Log($"[Sigil] {sigil.displayName} already unlocked");
                }
            }
        }
    }
}

