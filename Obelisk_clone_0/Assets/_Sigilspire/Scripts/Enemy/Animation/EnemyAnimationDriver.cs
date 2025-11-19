using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Simple animation driver for enemies.
    /// EnemyAI calls into this to update movement and attack animations.
    /// All animation is server-driven. No network animator required.
    /// </summary>
    public class EnemyAnimationDriver : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

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

            animator.SetFloat(MoveX, direction.x);
            animator.SetFloat(MoveY, direction.y);
            animator.SetFloat(Speed, speed);
        }

        public void PlayAttack(Vector2 attackDirection)
        {
            if (animator == null) return;

            animator.SetFloat(MoveX, attackDirection.x);
            animator.SetFloat(MoveY, attackDirection.y);
            animator.SetTrigger(Attack);
        }
    }
}

