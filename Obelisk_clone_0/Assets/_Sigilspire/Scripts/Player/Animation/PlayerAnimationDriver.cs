using UnityEngine;
using Unity.Netcode;

namespace Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationDriver : NetworkBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private bool allowAnimationCanceling = true;

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
                SetAnimSpeed(1f);
            }
            else
            {
                pendingUnlock = true;
                SetAnimSpeed(1f);
            }
        }

        private void LateUpdate()
        {
            if (!IsOwner) return;
            if (playerAnimator == null) return;

            AnimatorStateInfo s = playerAnimator.GetCurrentAnimatorStateInfo(0);

            if (localIsShielding && s.IsName("RaiseShield") && s.normalizedTime >= 1f)
            {
                playerAnimator.Play("RaiseShield", 0, 1f);
                SetAnimSpeed(0f);
                return;
            }

            if (!localIsShielding)
            {
                if (playerAnimator.speed == 0f) SetAnimSpeed(1f);

                if (pendingUnlock && s.IsName("LowerShield") && s.normalizedTime >= 1f)
                {
                    pendingUnlock = false;
                    if (_player != null) _player.SetMovementLocked(false);
                }
            }
        }

        // ===== ANIMATION CANCELING SYSTEM =====

        private void ForceAnimatorsToIdle()
        {
            if (!allowAnimationCanceling) return;

            if (playerAnimator != null)
                playerAnimator.Play("Idle", 0, 0f);

            if (swordAnimator != null)
                swordAnimator.Play("Idle", 0, 0f);

            if (shieldAnimator != null)
                shieldAnimator.Play("Idle", 0, 0f);

            if (grappleAnimator != null)
                grappleAnimator.Play("Idle", 0, 0f);
        }

        private void SetTriggerOnAllAnimators(string triggerName)
        {
            if (playerAnimator != null)
            {
                playerAnimator.ResetTrigger(triggerName);
                playerAnimator.SetTrigger(triggerName);
            }

            if (swordAnimator != null)
            {
                swordAnimator.ResetTrigger(triggerName);
                swordAnimator.SetTrigger(triggerName);
            }

            if (shieldAnimator != null)
            {
                shieldAnimator.ResetTrigger(triggerName);
                shieldAnimator.SetTrigger(triggerName);
            }

            if (grappleAnimator != null)
            {
                grappleAnimator.ResetTrigger(triggerName);
                grappleAnimator.SetTrigger(triggerName);
            }
        }

        private void ResetAllTriggers(string triggerName)
        {
            if (playerAnimator != null) playerAnimator.ResetTrigger(triggerName);
            if (swordAnimator != null) swordAnimator.ResetTrigger(triggerName);
            if (shieldAnimator != null) shieldAnimator.ResetTrigger(triggerName);
            if (grappleAnimator != null) grappleAnimator.ResetTrigger(triggerName);
        }

        // ===== ABILITY ANIMATIONS =====

        public void PlaySwordSlash()
        {
            if (!IsOwner) return;

            if (allowAnimationCanceling)
            {
                ResetAllTriggers("SwordSlash");
                ForceAnimatorsToIdle();
                SetTriggerOnAllAnimators("SwordSlash");
            }
            else
            {
                SetTriggerOnAllAnimators("SwordSlash");
            }
        }

        public void PlayRaiseShield()
        {
            if (!IsOwner) return;

            if (allowAnimationCanceling)
            {
                ResetAllTriggers("RaiseShield");
                ForceAnimatorsToIdle();
                SetTriggerOnAllAnimators("RaiseShield");
            }
            else
            {
                if (playerAnimator != null)
                {
                    playerAnimator.speed = 1f;
                    playerAnimator.ResetTrigger("LowerShield");
                    playerAnimator.SetTrigger("RaiseShield");
                }
                if (swordAnimator != null)
                {
                    swordAnimator.ResetTrigger("LowerShield");
                    swordAnimator.SetTrigger("RaiseShield");
                }
                if (shieldAnimator != null)
                {
                    shieldAnimator.ResetTrigger("LowerShield");
                    shieldAnimator.SetTrigger("RaiseShield");
                }
            }
        }

        public void PlayLowerShield()
        {
            if (!IsOwner) return;

            if (allowAnimationCanceling)
            {
                ResetAllTriggers("LowerShield");
                ForceAnimatorsToIdle();
                SetTriggerOnAllAnimators("LowerShield");
            }
            else
            {
                if (playerAnimator != null)
                {
                    if (playerAnimator.speed == 0f) playerAnimator.speed = 1f;
                    playerAnimator.ResetTrigger("RaiseShield");
                    playerAnimator.SetTrigger("LowerShield");
                }
                if (swordAnimator != null)
                {
                    swordAnimator.ResetTrigger("RaiseShield");
                    swordAnimator.SetTrigger("LowerShield");
                }
                if (shieldAnimator != null)
                {
                    shieldAnimator.ResetTrigger("RaiseShield");
                    shieldAnimator.SetTrigger("LowerShield");
                }
            }
        }

        public void PlayGrappleCast()
        {
            if (!IsOwner) return;

            if (allowAnimationCanceling)
            {
                ResetAllTriggers("GrappleCast");
                ForceAnimatorsToIdle();
                SetTriggerOnAllAnimators("GrappleCast");
            }
            else
            {
                SetTriggerOnAllAnimators("GrappleCast");
            }
        }

        public void PlayGrappleRetract()
        {
            if (!IsOwner) return;

            if (allowAnimationCanceling)
            {
                ResetAllTriggers("GrappleRetract");
                ForceAnimatorsToIdle();
                SetTriggerOnAllAnimators("GrappleRetract");
            }
            else
            {
                SetTriggerOnAllAnimators("GrappleRetract");
            }
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

            if (shieldAnimator != null)
            {
                shieldAnimator.ResetTrigger("Death");
                shieldAnimator.SetTrigger("Death");
            }

            if (grappleAnimator != null)
            {
                grappleAnimator.ResetTrigger("Death");
                grappleAnimator.SetTrigger("Death");
            }
        }
    }
}