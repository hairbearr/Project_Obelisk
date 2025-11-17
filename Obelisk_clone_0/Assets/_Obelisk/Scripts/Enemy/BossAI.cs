using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.Health;

namespace Enemy
{
    public class BossAI : EnemyAI
    {
        private enum BossPhase
        {
            Phase1,
            Phase2,
            Phase3
        }

        [Header("Boss Phases")]
        [SerializeField] private float phase2HealthThreshold = 0.66f;
        [SerializeField] private float phase3HealthThreshold = 0.33f;

        [Header("Boss Abilities")]
        [SerializeField] private Ability phase2Ability;
        [SerializeField] private Ability phase3Ability;

        private BossPhase _currentPhase = BossPhase.Phase1;
        private HealthBase _health;   // use base type

        private void Start()
        {
            _health = GetComponent<HealthBase>();
        }

        private void LateUpdate()
        {
            if (!IsServer) return;
            if (_health == null) return;

            UpdatePhase();
        }

        private void UpdatePhase()
        {
            if (_health.CurrentHealth.Value <= 0f) return;

            float healthPercent = _health.CurrentHealth.Value / _health.MaxHealth;

            if (healthPercent <= phase3HealthThreshold && _currentPhase != BossPhase.Phase3)
            {
                SwitchPhase(BossPhase.Phase3);
            }
            else if (healthPercent <= phase2HealthThreshold && _currentPhase == BossPhase.Phase1)
            {
                SwitchPhase(BossPhase.Phase2);
            }
        }

        private void SwitchPhase(BossPhase newPhase)
        {
            _currentPhase = newPhase;

            // TODO: swap abilities / buff stats / change behavior patterns
            // Example:
            // if (newPhase == BossPhase.Phase2 && phase2Ability != null) { ... }
        }

        // Later you can override PerformAbilityAttack or TryAttack to use
        // phase-specific abilities.
    }
}

