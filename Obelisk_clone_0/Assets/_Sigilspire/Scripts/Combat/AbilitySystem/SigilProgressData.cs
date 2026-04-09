using System;

namespace Combat.AbilitySystem
{
    /// <summary>
    /// Per-player, per-sigil runtime progression data.
    /// This should be serialized in your save / profile, not in the SO.
    /// </summary>
    [Serializable]
    public class SigilProgressData
    {
        public string sigilId;
        public int level = 0;
        public int currentXp = 0;

        public SigilProgressData(string sigilId)
        {
            this.sigilId = sigilId;
            level = 0;
            currentXp = 0;
        }
    }
}

