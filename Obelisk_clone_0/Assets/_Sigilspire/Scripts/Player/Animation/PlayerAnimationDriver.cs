using UnityEngine;
using Unity.Netcode;

namespace Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationDriver : NetworkBehaviour
    {
        [Header("Animator References")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private Animator swordAnimator;
        [SerializeField] private Animator shieldAnimator;
        [SerializeField] private Animator grappleAnimator;


        private PlayerController _player;

        private Vector2 _lastFacing = Vector2.down;

        private void Awake()
        {
            _player = GetComponent<PlayerController>();

            Debug.Log("SwordController: swordAnimator = " + (swordAnimator != null ? swordAnimator.name : "null"));
            Debug.Log("ShieldController: shieldAnimator = " + (shieldAnimator != null ? shieldAnimator.name : "null"));
            Debug.Log("GrappleController: grappleAnimator = " + (grappleAnimator != null ? grappleAnimator.name : "null"));

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

            SetWeaponFloats(swordAnimator, move, speed);
            SetWeaponFloats(shieldAnimator, move, speed);
            SetWeaponFloats(grappleAnimator, move, speed);
        }

        private void SetWeaponFloats(Animator anim, Vector2 move, float speed)
        {
            if (anim == null) return;
            anim.SetFloat("MoveX", move.x);
            anim.SetFloat("MoveY", move.y);
            anim.SetFloat("Speed", speed);
        }

    }
}
