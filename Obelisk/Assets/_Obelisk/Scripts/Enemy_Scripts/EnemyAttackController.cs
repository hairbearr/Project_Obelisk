using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Sigilspire.Combat;

namespace Sigilspire.Enemy
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyAttackController : NetworkBehaviour
    {
        [SerializeField] private Attack attackSO;
        private EnemyController enemyController;

        private void Awake()
        {
            enemyController = GetComponent<EnemyController>();
        }

        public void PerformAttack()
        {
            if (!IsServer) return;

            if (attackSO.attackRadius > 0f)
                ApplyAOEDamage();
            else
                ApplySingleTargetDamage();

            enemyController.SetAttackingState(true);
            PlayAttackAnimationClientRpc(enemyController.Direction);
        }

        private void ApplySingleTargetDamage()
        {
            Transform target = enemyController.CurrentTarget;
            if (target == null) return;

            Health targetHealth = target.GetComponent<Health>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamageServerRpc(
                    attackSO.damage,
                    attackSO.knockback,
                    transform.position
                );
            }
        }

        private void ApplyAOEDamage()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackSO.attackRadius, LayerMask.GetMask("Player"));
            foreach (Collider2D hit in hits)
            {
                Health playerHealth = hit.GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamageServerRpc(
                        attackSO.damage,
                        attackSO.knockback,
                        transform.position
                    );
                }
            }
        }

        [ClientRpc]
        private void PlayAttackAnimationClientRpc(Direction dir, ClientRpcParams rpcParams = default)
        {
            AnimationClip clip = attackSO.GetAttackAnimation(dir);
            if (clip != null)
                enemyController.GetComponent<Animator>().Play(clip.name);
        }

        private void OnDrawGizmosSelected()
        {
            if (attackSO != null && attackSO.attackRadius > 0f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, attackSO.attackRadius);
            }
        }
    }
}
