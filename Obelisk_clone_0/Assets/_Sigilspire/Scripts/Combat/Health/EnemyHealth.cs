using UnityEngine;
using Unity.Netcode;
using Combat.Health;
using Combat.DamageInterfaces;

namespace Enemy
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyHealth : HealthBase, IKnockbackable
    {
        [Header("Knockback")]
        [SerializeField] private float maxKnockbackSpeed = 12f;
        [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;
        [SerializeField] private float knockbackCooldown = 0.15f;
        private float nextKnockbackTime;

        private Rigidbody2D _rb2D;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        private void Awake()
        {
            _rb2D = GetComponent<Rigidbody2D>();
        }

        protected override void OnDeath()
        {
            if (!IsServer) return;

            // check if this is a boss
            bool isBoss = GetComponent<Enemy.BossAI>() != null;

            if (isBoss)
            {

                // Notify RunManager
                var runManager = FindFirstObjectByType<RunManager>();
                if(runManager != null)
                {
                    runManager.ServerNotifyBossDeath();
                }
            }
            else
            {
                // regular enemy, notify Mob Counter
                var mobCounter = FindFirstObjectByType<MobCounterUI>();
                if (mobCounter != null)
                {
                    mobCounter.ServerIncrementKills();
                }
            }                

            if (TryGetComponent<GrappleTarget>(out var gt))
                gt.ServerEndGrapple();

            slider.gameObject.SetActive(false);
            var enemyAI = GetComponent<EnemyAI>();
            if(enemyAI != null)
            {
                enemyAI.CleanUpTelegraphs();
            }

            StartCoroutine(DeathSequence());
        }

        private System.Collections.IEnumerator DeathSequence()
        {
            // Disable collider and rigidbody so enemy can't be hit/moved
            if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

            if (TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

            // disable AI so it stops moving/attacking
            if(TryGetComponent<Enemy.EnemyAI>(out var ai)) ai.enabled = false;

            // play death animation
            var animDriver = GetComponentInChildren<Enemy.EnemyAnimationDriver>();
            if(animDriver != null)
            {
                // set speed to 0 (dead/idle pose)
                animDriver.SetMovement(Vector2.zero);

                animDriver.PlayDeath();
            }

            // wait for animation to play
            yield return new WaitForSeconds(90f);

            // Fade out effect
            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if(spriteRenderer != null)
            {
                float fadeTime = 0.3f;
                float elapsed = 0f;
                Color originalColor = spriteRenderer.color;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.deltaTime;
                    float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                    spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    yield return null;
                }
            }
            // Despawn
            NetworkObject.Despawn();
        }

        public override void TakeDamage(float amount, ulong attackerId)
        {
            if (!IsServer) return;

            // Apply boss damage taken multiplier if this is a boss
            var bossAbility = GetComponentInChildren<BossAbilityController>();
            if (bossAbility != null)
            {
                amount *= bossAbility.damageTakenMultiplier.Value;

                if (bossAbility.shieldFromAddActive)
                {
                    amount *= bossAbility.damageReductionFromAdd;
                    Debug.Log($"[Boss] Add shield active! Damage reduced: {amount}");
                }
                if(bossAbility.damageTakenMultiplier.Value != 1)
                {
                    Debug.Log($"[Boss] Taking {amount} damage (multiplier: {bossAbility.damageTakenMultiplier.Value}x)");
                }
            }

            base.TakeDamage(amount, attackerId);
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (!IsServer) return;
            if (_rb2D == null) return;
            if (force <= 0f) return;

            if (TryGetComponent<GrappleTarget>(out var gt) && gt.IsBeingGrappled)
                return;

            if (Time.time < nextKnockbackTime) return;
            nextKnockbackTime = Time.time + knockbackCooldown;

            var ai = GetComponent<EnemyAI>();
            if (ai != null) ai.NotifyKnockback();

            float effectiveForce = force * (1f - knockbackResistance);
            if (effectiveForce <= 0f) return;

            Vector2 velBefore = _rb2D.linearVelocity;

            _rb2D.AddForce(direction.normalized * effectiveForce, ForceMode2D.Impulse);
            _rb2D.linearVelocity = Vector2.ClampMagnitude(_rb2D.linearVelocity, maxKnockbackSpeed);

        }



    }
}
