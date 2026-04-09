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

        [Header("Drops")]
        [SerializeField] private int xpReward = 10;
        [SerializeField] private SigilDropTable dropTable;
        [SerializeField, Range(0f, 100f)] private float dropChance = 5f; // 5% chance a drop event happens

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

                StopBossMusicClientRpc();

                // Notify RunManager
                var runManager = FindFirstObjectByType<RunManager>();
                if(runManager != null)
                {
                    runManager.ServerNotifyBossDeath();
                }

                var bossRoom = FindFirstObjectByType<BossRoom>();
                if (bossRoom != null)
                {
                    bossRoom.ServerNotifyBossDefeated();
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

            AwardSigilXp();
            TryDropSigil();


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

            // notify enemyAI that it took damage
            var ai = GetComponent<EnemyAI>();
            if (ai != null)
            {
                ai.NotifyDamageTaken();
            }

            // Apply boss damage taken multiplier if this is a boss
            var bossAbility = GetComponentInChildren<BossAbilityController>();
            if (bossAbility != null)
            {
                amount *= bossAbility.damageTakenMultiplier.Value;

                if (bossAbility.shieldFromAddActive)
                {
                    amount *= bossAbility.damageReductionFromAdd;
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

        private void AwardSigilXp()
        {
            if (!IsServer) return;
            if (xpReward <= 0f) return;

            // Find all players in the game
            var allPlayers = FindObjectsByType<Player.PlayerLoadout>(FindObjectsSortMode.None);

            foreach (var loadout in allPlayers)
            {
                if (loadout == null) continue;

                var inventory = loadout.GetComponent<Combat.AbilitySystem.SigilInventory>();
                if(inventory == null) continue;

                // Award XP to all equipped major sigils for this player
                string[] majorIds = new string[3]
                {
                    loadout.GetEquippedMajorId(Combat.AbilitySystem.WeaponSlot.Sword),
                    loadout.GetEquippedMajorId(Combat.AbilitySystem.WeaponSlot.Shield),
                    loadout.GetEquippedMajorId(Combat.AbilitySystem.WeaponSlot.Grapple)
                };

                foreach (string majorId in majorIds)
                {
                    if(string.IsNullOrEmpty(majorId)) continue;

                    var def = inventory.GetDefinition(majorId);
                    if (def != null && def.sigilType == Combat.AbilitySystem.SigilType.Major)
                    {
                        inventory.AddXp(majorId, xpReward);
                    }
                }
            }
        }

        private void TryDropSigil()
        {
            if (!IsServer) return;
            if (dropTable == null) return;
            if (dropTable.sigilDropPrefab == null) return;

            // Roll: does a drop event happen?
            float roll = Random.Range(0f, 100f);
            if (roll > dropChance) return; // No drop

            // Find all players
            var allPlayers = FindObjectsByType<Player.PlayerLoadout>(FindObjectsSortMode.None);
            if (allPlayers.Length == 0) return;

            // Roll a sigil for each player based on their inventory
            var playerSigilMap = new System.Collections.Generic.Dictionary<ulong, string>();

            foreach (var loadout in allPlayers)
            {
                if (loadout == null) continue;

                var inventory = loadout.GetComponent<Combat.AbilitySystem.SigilInventory>();
                if (inventory == null) continue;

                var networkObj = loadout.GetComponent<NetworkObject>();
                if (networkObj == null) continue;

                ulong clientId = networkObj.OwnerClientId;

                // Roll from this player's valid loot pool
                var sigil = dropTable.RollDropForPlayer(inventory);
                if (sigil != null)
                {
                    playerSigilMap[clientId] = sigil.id;
                }
            }

            // If no one got a valid drop, don't spawn anything
            if (playerSigilMap.Count == 0) return;

            // Spawn ONE drop object with all player assignments
            var dropObj = Instantiate(dropTable.sigilDropPrefab, transform.position, Quaternion.identity);
            var netObj = dropObj.GetComponent<NetworkObject>();

            if (netObj != null)
            {
                netObj.Spawn();

                var drop = dropObj.GetComponent<SigilDrop>();
                if (drop != null)
                {
                    drop.Initialize(playerSigilMap);
                }
            }
        }

        protected override void PlayHurtSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyHit(transform.position);
            }
        }

        protected override void PlayDeathSound()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyDeath(transform.position);
            }
        }

        [ClientRpc]
        private void StopBossMusicClientRpc()
        {
            if(AudioManager.Instance != null)
            {
                AudioManager.Instance.StopMusic();
                AudioManager.Instance.PlayGameplayMusic();
            }
        }
    }
}
