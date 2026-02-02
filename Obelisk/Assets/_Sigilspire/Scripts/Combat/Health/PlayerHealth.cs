using System;
using Unity.Netcode;
using UnityEngine;

namespace Combat.Health
{
    public class PlayerHealth : HealthBase
    {
        [Header("Death Settings")]
        [SerializeField] private float deathPenaltySeconds = 15f;
        [SerializeField] private float invulnerabilityDuration = 2f;

        private bool isInvulnerable = false;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        public override void TakeDamage(float amount, ulong attackerId)
        {

            if (isInvulnerable) return;
            base.TakeDamage(amount, attackerId);

        }


        protected override void OnDeath()
        {

            if (!IsServer) return;


            var timer = FindFirstObjectByType<RunTimerUI>();
            if ((timer!= null))
            {
                timer.ServerAddPenalty(deathPenaltySeconds);
            }



            StartCoroutine(DeathSequence());
        }

        private System.Collections.IEnumerator DeathSequence()
        {
            var animDriver = GetComponentInChildren<Player.PlayerAnimationDriver>();
            if(animDriver != null)
            {
                PlayDeathAnimClientRpc();
            }

            // wait for animation to finish
            yield return new WaitForSeconds(1.1f);

            RespawnAtCheckpoint();
        }

        [ClientRpc]
        private void PlayDeathAnimClientRpc()
        {
            var animDriver = GetComponentInChildren<Player.PlayerAnimationDriver>();
            if (animDriver != null) animDriver.PlayDeath();
        }

        private void RespawnAtCheckpoint()
        {
            Checkpoint cp = Checkpoint.Current;

            Vector2 spawnPos;
            if (cp != null)
            {
                spawnPos = cp.transform.position;
            }
            else
            {
                spawnPos = Vector2.zero;
            }

            if (rb != null)
                rb.position = spawnPos;
            else
                transform.position = new Vector3(spawnPos.x, spawnPos.y, 0f);

            CurrentHealth.Value = maxHealth;

            if (slider != null)
                slider.enabled = true;

            StartCoroutine(InvulnerabilityRoutine());
        }

        private System.Collections.IEnumerator InvulnerabilityRoutine()
        {
            isInvulnerable = true;

            var sprite = GetComponentInChildren<SpriteRenderer>();
            float elapsed = 0f;
            bool visible = true;

            while (elapsed < invulnerabilityDuration)
            {
                if (sprite != null)
                    sprite.enabled = visible;

                visible = !visible;

                yield return new WaitForSeconds(0.15f);
                elapsed += 0.15f;
            }

            if (sprite != null)
                sprite.enabled = true;

            isInvulnerable = false;
        }
    }
}


