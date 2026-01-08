using UnityEngine;


namespace Combat.DamageInterfaces
{
    public interface IGrapplePullable
    {
        // retrun true if this object should be pulled to the player.
        bool ShouldPullToPlayer();

        // server-side: perform the pull.
        void PullTowards(Vector2 point, float speed);
    }

}