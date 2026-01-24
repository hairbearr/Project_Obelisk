using UnityEngine;

namespace Net
{
    public class PlayerSpawnPoint : MonoBehaviour
    {
        [Tooltip("Lower = higher priority. Optional.")]
        public int index = 0;

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.6f);
        }
    }
}