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
        [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0f;

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
            if (IsServer)
            {
                Debug.Log("I've Died");
                NetworkObject.Despawn();
            }
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            if (!IsServer) return;
            if (_rb2D == null) return;
            if (force <= 0f) return;

            float effectiveForce = force * (1f - knockbackResistance);
            if (effectiveForce <= 0f) return;

            _rb2D.AddForce(direction.normalized * effectiveForce, ForceMode2D.Impulse);
        }
    }
}
