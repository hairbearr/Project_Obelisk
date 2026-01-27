using Combat.DamageInterfaces;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GrappleTarget : NetworkBehaviour, IGrapplePullable
{
    [Header("Behavior")]
    [SerializeField] private bool pullToPlayer = true;

    [Header("Stop Tuning")]
    [SerializeField] private float stopDistanceFromSurface = 0.25f;
    [SerializeField] private float minDistanceToStop = 0.02f;

    private Rigidbody2D rb;
    private Collider2D col;

    // Track multiple grappling players
    private readonly NetworkVariable<int> activeGrappleCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Track which player colliders we're ignoring (server only)
    private readonly HashSet<Collider2D> ignoredPlayerColliders = new HashSet<Collider2D>();

    public bool IsBeingGrappled => activeGrappleCount.Value > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public bool ShouldPullToPlayer() => pullToPlayer;

    // Called by GrapplingHookController when grapple begins
    public void ServerBeginGrapple()
    {
        if (!IsServer) return;
        activeGrappleCount.Value++;
    }

    // Called by GrapplingHookController when grapple ends
    public void ServerEndGrapple()
    {
        if (!IsServer) return;
        activeGrappleCount.Value = Mathf.Max(0, activeGrappleCount.Value - 1);
    }

    // New: Let grapple controller tell us which player collider to ignore
    public void ServerIgnoreCollisionWith(Collider2D playerCol)
    {
        if (!IsServer) return;
        if (col == null || playerCol == null) return;

        if (!ignoredPlayerColliders.Contains(playerCol))
        {
            Physics2D.IgnoreCollision(col, playerCol, true);
            ignoredPlayerColliders.Add(playerCol);
        }
    }

    // New: Restore collision with specific player
    public void ServerRestoreCollisionWith(Collider2D playerCol)
    {
        if (!IsServer) return;
        if (col == null || playerCol == null) return;

        if (ignoredPlayerColliders.Contains(playerCol))
        {
            Physics2D.IgnoreCollision(col, playerCol, false);
            ignoredPlayerColliders.Remove(playerCol);
        }
    }

    public void PullTowards(Vector2 point, float speed)
    {
        if (!IsServer) return;
        if (rb == null) return;

        float step = speed * Time.deltaTime;
        Vector2 pos = rb.position;
        Vector2 toPoint = point - pos;
        float dist = toPoint.magnitude;

        if (dist < minDistanceToStop) return;

        Vector2 desired = point;
        if (dist > 0.0001f)
            desired = point - (toPoint.normalized * stopDistanceFromSurface);

        Vector2 toDesired = desired - pos;
        float d = toDesired.magnitude;

        if (d <= minDistanceToStop || d <= step)
        {
            rb.MovePosition(desired);
            return;
        }

        rb.MovePosition(pos + toDesired.normalized * step);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        // Restore all ignored collisions
        // Create a copy to avoid modification during iteration issues
        var collidersCopy = new List<Collider2D>(ignoredPlayerColliders);
        foreach (var playerCol in collidersCopy)
        {
            if (playerCol != null && col != null)
                Physics2D.IgnoreCollision(col, playerCol, false);
        }

        ignoredPlayerColliders.Clear();
        activeGrappleCount.Value = 0;
    }
}
