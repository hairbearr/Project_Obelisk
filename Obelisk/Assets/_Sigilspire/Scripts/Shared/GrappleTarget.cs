using Combat.DamageInterfaces;
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

    // Networked so everyone can know (optional, but useful)
    private readonly NetworkVariable<bool> isBeingGrappled = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool IsBeingGrappled => isBeingGrappled.Value;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public bool ShouldPullToPlayer() => pullToPlayer;

    // server-only setters
    public void ServerBeginGrapple()
    {
        if (!IsServer) return;
        isBeingGrappled.Value = true;
    }

    public void ServerEndGrapple()
    {
        if (!IsServer) return;
        isBeingGrappled.Value = false;
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
        // clear if despawned while grappled
        if (IsServer) isBeingGrappled.Value = false;
    }
}
