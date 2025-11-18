using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;

namespace Combat.Projectiles
{
    public class ProjectileBase : NetworkBehaviour
    {
        [SerializeField] protected float speed = 10f;
        [SerializeField] protected float damage = 10f;
        [SerializeField] protected float lifetime = 3f;
        [SerializeField] protected LayerMask hitLayers;

        private float spawnTime;

        private void OnEnable()
        {
            spawnTime = Time.time;
        }

        private void Update()
        {
            if (!IsServer) return;

            transform.position += transform.forward * (speed * Time.deltaTime);

            if (Time.time - spawnTime > lifetime)
            {
                NetworkObject.Despawn();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            if (((1 << other.gameObject.layer) & hitLayers) == 0)
                return;

            var dmg = other.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
            }

            NetworkObject.Despawn();
        }
    }
}
