using UnityEngine;

namespace Combat.DamageInterfaces
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    public interface IKnockbackable
    {
        void ApplyKnockback(Vector3 direction, float force);
    }
}
