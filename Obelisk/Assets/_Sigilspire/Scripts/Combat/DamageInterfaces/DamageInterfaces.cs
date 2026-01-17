using UnityEngine;

namespace Combat.DamageInterfaces
{
    public interface IThreatReceiver
    {
        // Source is the player that generated the threat
        // Amount is "threat points"
        void AddThreat(ulong sourceNetworkObjectId, float amount);
    }

    public interface IDamageable
    {
        void TakeDamage(float amount);
        void TakeDamage(float amount, ulong attackerId);
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
