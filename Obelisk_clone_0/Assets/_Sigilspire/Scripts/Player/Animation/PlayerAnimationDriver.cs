using UnityEngine;
using Unity.Netcode;
using Shared; // For DirectionResolver if you want to use it later

namespace Player
{
    /// <summary>
    /// Drives the player and weapon animators based on PlayerController input.
    /// Assumes:
    /// - PlayerAnimator has parameters: MoveX, MoveY, Speed
    /// - WeaponAnimator has the same parameters and a state named "WeaponLocomotion"
    /// - Player locomotion state is named "PlayerLocomotion"
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationDriver : NetworkBehaviour
    {
        [Header("Animator References")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator weaponAnimator;

        private PlayerController _playerController;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();

            if (playerAnimator == null)
            {
                // Try to auto-find under VisualRoot
                playerAnimator = GetComponentInChildren<Animator>();
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;
            if (_playerController == null) return;

            Vector2 move = _playerController.CurrentMoveInput;

            float speed = move.magnitude;

            if (playerAnimator != null)
            {
                playerAnimator.SetFloat("MoveX", move.x);
                playerAnimator.SetFloat("MoveY", move.y);
                playerAnimator.SetFloat("Speed", speed);
            }

            if (weaponAnimator != null)
            {
                weaponAnimator.SetFloat("MoveX", move.x);
                weaponAnimator.SetFloat("MoveY", move.y);
                weaponAnimator.SetFloat("Speed", speed);

                // Optional: sync locomotion normalized time
                if (playerAnimator != null)
                {
                    var stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
                    float t = stateInfo.normalizedTime;

                    weaponAnimator.Play("WeaponLocomotion", 0, t % 1f);
                }
            }
        }
    }
}
