using UnityEngine;
using Unity.Netcode;

public class RoomDoor : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private Transform teleportDestination;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;

    private NetworkVariable<bool> isLocked = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isLocked.Value = startsLocked;
        }

        isLocked.OnValueChanged += OnLockStateChanged;
        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isLocked.OnValueChanged -= OnLockStateChanged;
    }

    private void OnLockStateChanged(bool oldVal, bool newVal)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (doorSprite != null)
        {
            doorSprite.color = isLocked.Value ? lockedColor : unlockedColor;
        }
    }

    public void ServerUnlock()
    {
        if (!IsServer) return;
        isLocked.Value = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (isLocked.Value)
        {
            return;
        }

        // teleport player to destination
        if(teleportDestination != null)
        {
            var playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.position = teleportDestination.position;
            }
            else
            {
                collision.transform.position = teleportDestination.position;
            }
        }
    }
}
