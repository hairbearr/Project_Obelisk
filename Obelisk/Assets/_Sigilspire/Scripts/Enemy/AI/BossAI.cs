using UnityEngine;
using Unity.Netcode;
using Combat.AbilitySystem;
using Combat.Health;
using System;

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

        [Header("Phase Buffs")]
        [SerializeField] private float phase2SpeedMult = 1.3f;
        [SerializeField] private float phase3SpeedMult = 1.6f;
        [SerializeField] private float phase2DmgMult = 1.5f;
        [SerializeField] private float phase3DmgMult = 2.0f;

        private BossPhase _currentPhase = BossPhase.Phase1;
        private HealthBase _health;   // use base type
        private float _baseMoveSpeed;
        private float _baseDamage;

        [Header("Abilities")]
        [SerializeField] private BossAbilityController abilityController;

        private void Start()
        {
            _health = GetComponent<HealthBase>();

            if (abilityController == null)
            {
                abilityController = GetComponentInParent<BossAbilityController>();
            }

            _baseMoveSpeed = moveSpeed;
            _baseDamage = primaryAbility != null ? primaryAbility.damage : 10f;

            resetBehavior = ResetBehavior.Teleport;
        }

        private void LateUpdate()
        {
            if (!IsServer) return;
            if (_health == null || _health.CurrentHealth.Value <= 0f) return;

            UpdatePhase();
            TryUseAbilities();
        }

        private void TryUseAbilities()
        {
            if (abilityController == null) return;
            if (abilityController.IsPerformingAbility) return;
            if (inTransition) return;

            Transform target = FindClosestPlayer();
            if (target != null)
            {
                float distance = Vector2.Distance(transform.position, target.position);

                // Only use abilities when at basic attack range
                // This keeps movement and abilities unified
                if (distance <= attackRange * 1.2f) // Small buffer
                {
                    abilityController.TryUseAbility(target);
                }
            }
        }

        private Transform FindClosestPlayer()
        {
            // finds any player
            var player = FindFirstObjectByType<Player.PlayerController>();
            return player != null ? player.transform : null;
        }

        private void UpdatePhase()
        {
            if (_health.CurrentHealth.Value <= 0f) return;

            float healthPercent = _health.CurrentHealth.Value / _health.MaxHealth;

            Debug.Log($"[Boss] Health: {healthPercent:F2}, Phase: {_currentPhase}");

            // Check for phase transitions
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


            // Tell ability controller about phase change
            if (abilityController != null)
            {
                int phaseIndex = newPhase == BossPhase.Phase1 ? 0
                               : newPhase == BossPhase.Phase2 ? 1
                               : 2;

                abilityController.SetPhase(phaseIndex);
            }

            // Apply phase buffs
            switch (newPhase)
            {
                case BossPhase.Phase2:
                    ApplyPhase2Buffs();
                    break;
                case BossPhase.Phase3:
                    ApplyPhase3Buffs();
                    break;
            }

            PhaseTransitionEffectsClientRpc();
        }

        private void ApplyPhase2Buffs()
        {
            // Increase speed and damage
            moveSpeed = _baseMoveSpeed * phase2SpeedMult;

            if (primaryAbility != null)
                primaryAbility.damage = _baseDamage * phase2DmgMult;
        }

        private void ApplyPhase3Buffs()
        {
            // Further increase speed & damage
            moveSpeed = _baseMoveSpeed * phase3SpeedMult;

            if (primaryAbility != null)
                primaryAbility.damage = _baseDamage * phase3DmgMult;
        }

        public virtual void OnBossReset()
        {
            if (!IsServer) return;

            // Reset phases
            _currentPhase = 0;
            abilityController.SetPhase(0);
            
            Debug.Log("[Boss] Clearing summoned adds on reset");

            // clear add if it exists
            if(abilityController != null && abilityController.summonedAddId != 0)
            {
                var sm = NetworkManager.SpawnManager;
                if(sm != null && sm.SpawnedObjects.TryGetValue(abilityController.summonedAddId, out NetworkObject addObj))
                {
                    if(addObj != null && addObj.IsSpawned)
                    {
                        addObj.Despawn(true);
                        Debug.Log("[Boss] Despawned add on reset");
                    }
                }

                // Clear boss ability controller state
                abilityController.summonedAddId = 0;
                abilityController.shieldFromAddActive = false;
            }
        }

        [ClientRpc]
        private void PhaseTransitionEffectsClientRpc()
        {
            // Screen shake
            var shake = FindFirstObjectByType<CameraShake>();
            if (shake != null)
                shake.Shake(0.3f, 0.3f);

            // flash boss sprite
            StartCoroutine(PhaseTransitionFlash());
        }

        private System.Collections.IEnumerator PhaseTransitionFlash()
        {
            var sprite = GetComponentInChildren<SpriteRenderer>();
            if (sprite == null) yield break;

            Color original = sprite.color;

            // flash white/red 3 times
            for(int i = 0; i < 3; i++)
            {
                sprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                sprite.color = Color.red;
                yield return new WaitForSeconds(0.1f);
            }

            sprite.color = original;
        }
    }
}

