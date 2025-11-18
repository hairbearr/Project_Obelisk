using UnityEngine;

namespace Combat.DamageInterfaces
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }

    public interface IKnockbackable
    {
        /// <summary>
        /// Apply a 2D knockback impulse in world-space.
        /// direction MUST be a Vector2 in XY plane.
        /// </summary>
        void ApplyKnockback(Vector2 direction, float force);
    }
}
