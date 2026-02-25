using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int priority = 0;

    private static Checkpoint _current;
    public static Checkpoint Current => _current;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if(_current == null || priority > _current.priority)
        {
            _current = this;
        }

        Debug.Log($"Checkpoint: {this.name}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }
}
