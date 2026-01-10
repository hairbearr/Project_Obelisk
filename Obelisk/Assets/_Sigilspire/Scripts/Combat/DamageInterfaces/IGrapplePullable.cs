using UnityEngine;


namespace Combat.DamageInterfaces
{
    public interface IGrapplePullable
    {
        // Return true if this object should be pulled to the player.
        bool ShouldPullToPlayer();

        // Server-side: perform the pull.
        void PullTowards(Vector2 point, float speed);
    }
}