using UnityEngine;

namespace Enemy
{
    public class EnemyAnimationDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        public System.Action onAttackHitFrame;

        private const string MoveX = "MoveX";
        private const string MoveY = "MoveY";
        private const string Speed = "Speed";
        private const string Attack = "Attack";

        private void Reset()
        {
            animator = GetComponentInChildren<Animator>();
        }

        public void SetMovement(Vector2 direction)
        {
            if (animator == null) return;

            float speed = direction.magnitude;
            if (speed > 0.001f)
            {
                animator.SetFloat(MoveX, direction.x);
                animator.SetFloat(MoveY, direction.y);
            }

            animator.SetFloat(Speed, speed);
        }

        public void SetFacing(Vector2 direction)
        {
            if (animator == null) return;
            if (direction.sqrMagnitude < 0.0001f) return;

            direction.Normalize();
            animator.SetFloat(MoveX, direction.x);
            animator.SetFloat(MoveY, direction.y);
            // Do NOT touch Speed here.
        }

        public void PlayAttack(Vector2 attackDirection)
        {
            if (animator == null) return;

            if (attackDirection.sqrMagnitude > 0.0001f)
            {
                attackDirection.Normalize();
                animator.SetFloat(MoveX, attackDirection.x);
                animator.SetFloat(MoveY, attackDirection.y);
            }

            animator.SetTrigger(Attack);
        }

        public void OnAttackHitFrame()
        {
            onAttackHitFrame?.Invoke();
        }

    }
}

