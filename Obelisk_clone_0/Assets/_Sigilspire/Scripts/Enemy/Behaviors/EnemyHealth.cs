using UnityEngine;
using Unity.Netcode;
using Combat.Health;
using Combat.DamageInterfaces;

namespace Enemy
{
    /// <summary>
    /// Basic enemy health implementation.
    /// Uses HealthBase for HP/death and implements knockback.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyHealth : HealthBase, IKnockbackable
    {
        [Header("Knockback")]
        [SerializeField] private float knockbackResistance = 0f; // 0 = full, 1 = immune

        private Rigidbody _rigidbody;

        protected override void Start()
        {
            base.Start();
            _rigidbody = GetComponent<Rigidbody>();
        }

        protected override void OnDeath()
        {
            // TODO: death VFX/SFX, drop loot, notify dungeon system, etc.
            if (IsServer)
            {
                NetworkObject.Despawn();
            }
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (!IsServer) return;
            if (_rigidbody == null) return;
            if (force <= 0f) return;

            float effectiveForce = force * (1f - knockbackResistance);
            if (effectiveForce <= 0f) return;

            _rigidbody.AddForce(direction.normalized * effectiveForce, ForceMode.Impulse);
        }
    }
}

