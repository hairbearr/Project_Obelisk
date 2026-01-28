using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Bridge script for forwarding animation events from the enemy's animator
    /// to the EnemyAnimationDriver on the same or parent object.
    /// Attach this to the same GameObject as the enemy's Animator.
    /// </summary>
    public class EnemyAnimationEvents : MonoBehaviour
    {
        [SerializeField] private EnemyAnimationDriver animDriver;

        private void Awake()
        {
            // Auto-find animation driver if not assigned
            if (animDriver == null)
                animDriver = GetComponent<EnemyAnimationDriver>();

            // Try parent if not found on same object
            if (animDriver == null)
                animDriver = GetComponentInParent<EnemyAnimationDriver>();
        }

        /// <summary>
        /// Called by Animation Event. Forwards to EnemyAnimationDriver.
        /// </summary>
        public void OnAttackHitFrame()
        {
            if (animDriver != null)
                animDriver.OnAttackHitFrame();
        }
    }
}
