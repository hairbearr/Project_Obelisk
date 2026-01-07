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

        private bool localIsShielding;

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
            float speed = Mathf.Clamp01(move.magnitude);


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

            SetWeaponFloats(swordAnimator, facing, speed);
            SetWeaponFloats(shieldAnimator, facing, speed);
            SetWeaponFloats(grappleAnimator, facing, speed);

            if (Time.frameCount % 120 == 0)
            {
                if (playerAnimator != null) Debug.Log("PlayerAnimator.speed=" + playerAnimator.speed);
                if (swordAnimator != null) Debug.Log("SwordAnimator.speed=" + swordAnimator.speed);
            }

        }

        private void LateUpdate()
        {
            if (!IsOwner) return;
            if (playerAnimator  == null) return;

            //if (!localIsShielding) return;

            AnimatorStateInfo s = playerAnimator.GetCurrentAnimatorStateInfo(0);

            // freeze body at the end of RaiseShield while held
            if(localIsShielding && s.IsName("RaiseShield") && s.normalizedTime >= 1f)
            {
                playerAnimator.speed = 0f;
                playerAnimator.Play("RaiseShield", 0, 1f);
                return;
            }

            if( !localIsShielding && s.IsName("LowerShield") && s.normalizedTime >= 1f)
            {
                playerAnimator.speed = 1f;

                if(_player != null)
                {
                    print("lowering shield from PlayerAnimationDriver");
                    _player.SetMovementLocked(false);
                }
            }
        }


        private void SetWeaponFloats(Animator anim, Vector2 move, float speed)
        {
            if (anim == null) return;
            anim.SetFloat("MoveX", move.x);
            anim.SetFloat("MoveY", move.y);
            anim.SetFloat("Speed", speed);
        }

        public void PlaySwordSlash()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;
            playerAnimator.ResetTrigger("SwordSlash");
            playerAnimator.SetTrigger("SwordSlash");
        }

        public void PlayRaiseShield()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;

            playerAnimator.speed = 1f; // ensure not frozen
            playerAnimator.ResetTrigger("LowerShield");
            playerAnimator.SetTrigger("RaiseShield");
        }

        public void PlayLowerShield()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;

            // unfreeze so LowerShield can play
            if(playerAnimator.speed == 0f) playerAnimator.speed = 1f;

            playerAnimator.ResetTrigger("RaiseShield");
            playerAnimator.SetTrigger("LowerShield");
        }

        public void PlayGrappleCast()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;
            playerAnimator.ResetTrigger("GrappleCast");
            playerAnimator.SetTrigger("GrappleCast");
        }

        public void PlayGrappleRetract()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;
            playerAnimator.ResetTrigger("GrappleRetract");
            playerAnimator.SetTrigger("GrappleRetract");
        }

        public void SetShielding(bool shielding)
        {
            localIsShielding = shielding;

            // if we are releasing, make sure the animator can play again.
            if (!localIsShielding && playerAnimator != null && playerAnimator.speed == 0f) playerAnimator.speed = 1f;
        }

    }
}
