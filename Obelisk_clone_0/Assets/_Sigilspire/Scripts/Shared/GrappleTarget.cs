using Combat.DamageInterfaces;
using Unity.Netcode;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class GrappleTarget : NetworkBehaviour, IGrapplePullable
{
    [Header("Behavior")]
    [SerializeField] private bool pullToPlayer = true;

    [Header("Stop Tuning")]
    [Tooltip("Stop when collider-to-collider distance is <= this.")]
    [SerializeField] private float stopDistanceFromSurface = 0.25f;

    [Tooltip("Fallback epsilon so we don't jitter forever")]
    [SerializeField] private float minDistanceToStop = 0.02f;

    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    public bool ShouldPullToPlayer() => pullToPlayer;

    public void PullTowards(Vector2 point, float speed)
    {
        if (!IsServer) return;
        if (rb == null) return;

        float step = speed * Time.deltaTime;

        Vector2 pos = rb.position;
        Vector2 toPoint = point - pos;
        float dist = toPoint.magnitude;

        if (dist < minDistanceToStop) return;

        // try to stop short of the point
        Vector2 desired = point;

        Vector2 toDesired = desired - pos;
        float d = toDesired.magnitude;

        if (d <= minDistanceToStop || d <= step)
        {
            rb.MovePosition(desired);
            return;
        }

        rb.MovePosition(pos + toDesired.normalized * step);
    }
}
