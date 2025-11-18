using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Simple enemy animation driver based on movement.
    /// Uses local position delta to infer movement direction.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EnemyAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private Vector3 _lastPosition;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            _lastPosition = transform.position;
        }

        private void Update()
        {
            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;

            Vector2 move = new Vector2(delta.x, delta.z);
            float speed = move.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

            if (animator != null)
            {
                animator.SetFloat("MoveX", move.x);
                animator.SetFloat("MoveY", move.y);
                animator.SetFloat("Speed", speed);
            }
        }
    }
}
