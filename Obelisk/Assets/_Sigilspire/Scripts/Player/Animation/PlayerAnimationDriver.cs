using UnityEngine;
using Unity.Netcode;

namespace Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationDriver : NetworkBehaviour
    {
        [Header("Animator References")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator weaponAnimator;

        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
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
            if (_player == null) return;

            Vector2 move = _player.CurrentMoveInput;
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
