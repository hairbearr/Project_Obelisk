using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// Triggers attack hit frame when the Attack animation state reaches a certain point.
    /// Attach this to the Attack state in the Animator Controller.
    /// Gets timing from the EnemyAI component for per-enemy flexibility.
    /// </summary>
    public class EnemyAttackStateBehaviour : StateMachineBehaviour
    {
        [Header("Fallback Timing")]
        [Tooltip("Used if EnemyAI component not found")]
        [Range(0f, 1f)]
        [SerializeField] private float defaultHitFrameNormalizedTime = 0.5f;

        private bool hasTriggeredHit;
        private float targetNormalizedTime;

        // Called when entering the Attack state
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            hasTriggeredHit = false;

            // Get timing from the enemy AI component
            var enemyAI = animator.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
            {
                targetNormalizedTime = enemyAI.AttackHitTiming;
            }
            else
            {
                // Fallback if no EnemyAI found (shouldn't happen)
                targetNormalizedTime = defaultHitFrameNormalizedTime;
            }
        }

        // Called every frame while in the Attack state
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // Fire once when we reach the hit frame
            if (!hasTriggeredHit && stateInfo.normalizedTime >= targetNormalizedTime)
            {
                hasTriggeredHit = true;

                // Find the animation driver and trigger hit
                var animDriver = animator.GetComponentInParent<EnemyAnimationDriver>();
                if (animDriver != null)
                {
                    animDriver.OnAttackHitFrame();
                }
            }
        }
    }
}