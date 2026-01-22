using Combat.DamageInterfaces;
using UnityEngine;

public class GrappleAnchor : MonoBehaviour, IGrapplePullable
{
    public bool ShouldPullToPlayer() => false;
    public void PullTowards(Vector2 point, float speed) { } // intentionally unused
}
