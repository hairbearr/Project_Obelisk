using UnityEngine;

namespace Combat
{
    /// <summary>
    /// Bridge script for forwarding animation events from the sword visuals
    /// to the SwordController on the parent object.
    /// Attach this to the same GameObject as the sword's Animator.
    /// </summary>
    public class SwordAnimationEvents : MonoBehaviour
    {
        [SerializeField] private SwordController swordController;

        private void Awake()
        {
            // Auto-find sword controller in parent if not assigned
            if (swordController == null)
                swordController = GetComponentInParent<SwordController>();
        }

        /// <summary>
        /// Called by Animation Event. Forwards to SwordController.
        /// </summary>
        public void OnSwordHitFrame()
        {
            if (swordController != null)
                swordController.OnSwordHitFrame();
        }
    }
}
