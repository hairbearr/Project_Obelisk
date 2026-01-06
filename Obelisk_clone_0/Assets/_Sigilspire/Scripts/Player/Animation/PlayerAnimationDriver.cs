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

        private Vector2 _lastFacing = Vector2.down;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            Debug.Log("SwordController: weaponAnimator = " + (weaponAnimator != null ? weaponAnimator.name : "null"));

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

            Vector2 facing = move;

            if(facing.sqrMagnitude > 0.0001f)
            {
                _lastFacing  = facing.normalized;
            }
            else
            {
                facing = _lastFacing;
            }

            if (playerAnimator != null)
            {
                playerAnimator.SetFloat("MoveX", facing.x);
                playerAnimator.SetFloat("MoveY", facing.y);
                playerAnimator.SetFloat("Speed", speed);
            }

            if (weaponAnimator != null)
            {
                weaponAnimator.SetFloat("MoveX", facing.x);
                weaponAnimator.SetFloat("MoveY", facing.y);
                weaponAnimator.SetFloat("Speed", speed);

                /*if (playerAnimator != null)
                {
                    var stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
                    float t = stateInfo.normalizedTime;
                    weaponAnimator.Play("Locomotion", 0, t % 1f);
                }*/

                if (playerAnimator != null)
                {
                    AnimatorStateInfo p = playerAnimator.GetCurrentAnimatorStateInfo(0);
                    AnimatorStateInfo w = weaponAnimator.GetCurrentAnimatorStateInfo(0);

                    if (p.IsName("Locomotion") && w.IsName("Locomotion"))
                    {
                        float t = p.normalizedTime;
                        weaponAnimator.Play("Locomotion", 0, t % 1f);
                    }
                }
            }
        }
    }
}
