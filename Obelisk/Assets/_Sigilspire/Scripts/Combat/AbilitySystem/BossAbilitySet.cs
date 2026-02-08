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

        [Header("Phase Transition")]
        public List<PhaseTransitionType> transitions = new List<PhaseTransitionType>();
        public int chargeCount = 1; // for charge transitions
        public float chargeMissBuffAmount = 0.25f; // damage buff if charge misses
        public float chargeMissBuffDuration = 10f; // how long the buff lasts
        public float shieldAmount = 100f; // Used if shield is on the list

        [Header("Ability Pool")]
        public List<Ability> primaryAbilities = new List<Ability>(); // A/B pool
        public List<Ability> secondaryAbilities = new List<Ability>(); // C pool (priority)

        [Header("Rotation Settings")]
        public RotationMode rotationMode = RotationMode.Alternate;
        public float abilityCooldown = 8f;
        public float secondaryAbilityCooldown = 8f; // For A/B -> C -> A/B pattern

        [Header("Summon Settings")]
        public GameObject summonPrefab;
        public float summonHPMultiplier = 0.5f; // 50% of normal HP
        public bool enableShieldFromAdd = false;
        public float shieldFromAddDamageReduction = 0.5f;
    }

    public enum PhaseTransitionType
    {
        None,   // No special transition
        Charge, // Boss charges at player(s)
        Shield, // Boss shields itself during transition
        Summon, // Could summon adds
        Enrage, // Could just do VFX/Shake
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
