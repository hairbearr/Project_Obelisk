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

        private bool pendingUnlock;

        private bool localIsShielding;

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
            float speed = Mathf.Clamp01(move.magnitude);

            Vector2 facing = _player.LastFacingDir;

            SetAnimFloats(playerAnimator, facing, speed);
            SetAnimFloats(swordAnimator, facing, speed);
            SetAnimFloats(shieldAnimator, facing, speed);
            SetAnimFloats(grappleAnimator, facing, speed);
        }

        private void SetAnimFloats(Animator anim, Vector2 facing, float speed)
        {
            if (anim == null) return;

            anim.SetFloat("MoveX", facing.x);
            anim.SetFloat("MoveY", facing.y);
            anim.SetFloat("Speed", speed);
        }

        public void SetShielding(bool shielding)
        {
            localIsShielding = shielding;

            if (localIsShielding)
            {
                pendingUnlock = false;
                // Make sure RaiseShield can play
                SetAnimSpeed(1f);
            }
            else
            {
                // We want to unlock movement when LowerShield completes
                pendingUnlock = true;

                // Ensure LowerShield can play
                SetAnimSpeed(1f);
            }
        }


        private void LateUpdate()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;

            AnimatorStateInfo s = playerAnimator.GetCurrentAnimatorStateInfo(0);

            // While holding block: once RaiseShield finishes, freeze everything.
            if (localIsShielding && s.IsName("RaiseShield") && s.normalizedTime >= 1f)
            {
                // Pin the state to last frame (prevents drift)
                playerAnimator.Play("RaiseShield", 0, 1f);

                // Freeze player + weapons (since you don't allow other actions anyway)
                SetAnimSpeed(0f);
                return;
            }

            // When releasing: ensure anims can play LowerShield
            if (!localIsShielding)
            {
                // Always unfreeze so LowerShield can progress
                if (playerAnimator.speed == 0f) SetAnimSpeed(1f);

                // Unlock movement after LowerShield ends (once)
                if (pendingUnlock && s.IsName("LowerShield") && s.normalizedTime >= 1f)
                {
                    pendingUnlock = false;
                    if (_player != null) _player.SetMovementLocked(false);
                }
            }
        }




        private void SetWeaponFloats(Animator anim, Vector2 facing, float speed)
        {
            if (anim == null) return;
            anim.SetFloat("MoveX", facing.x);
            anim.SetFloat("MoveY", facing.y);
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

        private void SetAnimSpeed(float speed)
        {
            if (playerAnimator != null) playerAnimator.speed = speed;
            if (swordAnimator != null) swordAnimator.speed = speed;
            if (shieldAnimator != null) shieldAnimator.speed = speed;
            if (grappleAnimator != null) grappleAnimator.speed = speed;
        }

        public void PlayDeath()
        {
            if (playerAnimator != null)
            {
                playerAnimator.ResetTrigger("Death");
                playerAnimator.SetTrigger("Death");
            }

            if (swordAnimator != null) 
            {
                swordAnimator.ResetTrigger("Death");
                swordAnimator.SetTrigger("Death");
            }

            if(shieldAnimator != null)
            {
                shieldAnimator.ResetTrigger("Death");
                shieldAnimator.SetTrigger("Death");
            }

            if(grappleAnimator != null)
            {
                grappleAnimator.ResetTrigger("Death");
                grappleAnimator.SetTrigger("Death");
            }
        }
    }
}
