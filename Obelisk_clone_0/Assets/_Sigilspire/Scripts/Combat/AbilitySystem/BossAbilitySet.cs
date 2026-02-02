using UnityEngine;
using Combat.AbilitySystem;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BossAbilitySet", menuName = "Sigilspire/Boss Ability Set")]
public class BossAbilitySet : ScriptableObject
{
    [System.Serializable]
    public class PhaseAbilities
    {
        public string phaseName = "Phase 1";

        [Header("Ability Pool")]
        public List<Ability> primaryAbilities = new List<Ability>(); // A/B pool
        public List<Ability> secondaryAbilities = new List<Ability>(); // C pool (priority)

        [Header("Rotation Settings")]
        public RotationMode rotationMode = RotationMode.Alternate;
        public float abilityCooldown = 8f;
        public float secondaryAbilityCooldown = 8f; // For A/B -> C -> A/B pattern
    }

    public enum RotationMode
    {
        Alternate,      // A -> B -> A -> B (Phase 1)
        PriorityRotate, // (A or B) -> C -> (A or B) -> C (Phase 2)
        Random          // Any ability, any time (Phase 3)
    }

    public List<PhaseAbilities> phases = new List<PhaseAbilities>();

    public PhaseAbilities GetPhaseAbilities(int phaseIndex)
    {
        if(phaseIndex <0 || phaseIndex >= phases.Count)
        {
            return null;
        }

        return phases[phaseIndex];
    }

}
