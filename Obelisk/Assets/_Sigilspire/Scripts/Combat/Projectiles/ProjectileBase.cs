using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

namespace Combat.Projectiles
{
    public enum ProjectileType
    {
        Arrow,
        Fireball
    }

    public class ProjectileBase : NetworkBehaviour
    {
        [Header("Projectile Type")]
        [SerializeField] private ProjectileType projectileType = ProjectileType.Arrow;

        [Header("Stats")]
        [SerializeField] protected float speed = 10f;
        [SerializeField] private float _damage = 10f;
        public float damage { get => _damage; set => _damage = value; }
        [SerializeField] protected float lifetime = 3f;
        [SerializeField] protected LayerMask hitLayers;

        private float spawnTime;
        private Vector2 moveDirection = Vector2.right;

        public ProjectileType GetProjectileType() => projectileType;

        private void OnEnable()
        {
            spawnTime = Time.time;
            if (moveDirection == Vector2.zero)
                moveDirection = Vector2.right;
        }

        public void SetDirection(Vector2 direction)
        {
            moveDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;
        }

        private void Update()
        {
            if (!IsServer) return;

            Vector2 currentPos = transform.position;
            Vector2 newPos = currentPos + moveDirection * (speed * Time.deltaTime);
            transform.position = new Vector3(newPos.x, newPos.y, 0f);

            if (Time.time - spawnTime > lifetime)
            {
                NetworkObject.Despawn();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsServer) return;

            if (((1 << other.gameObject.layer) & hitLayers) == 0)
                return;

            var dmg = other.GetComponent<IDamageable>();
            bool hitFlesh = (dmg != null);

            PlayImpactSoundClientRpc(transform.position, projectileType, hitFlesh);

            if (dmg != null && damage > 0f)
            {
                dmg.TakeDamage(damage);
            }

            NetworkObject.Despawn();
        }

        [ClientRpc]
        private void PlayImpactSoundClientRpc(Vector2 impactPos, ProjectileType type, bool hitFlesh)
        {
            if (AudioManager.Instance == null) return;

            switch (type)
            {
                case ProjectileType.Arrow:
                    if (hitFlesh)
                        AudioManager.Instance.PlayArrowImpactFlesh(impactPos);
                    else
                        AudioManager.Instance.PlayArrowImpactWall(impactPos);
                    break;

                case ProjectileType.Fireball:
                    AudioManager.Instance.PlayMageFireballHit(impactPos);
                    break;
            }
        }
    }
}
