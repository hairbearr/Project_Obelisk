using UnityEngine;
using Unity.Netcode;

public class RoomDoor : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool startsLocked = true;
    [SerializeField] private Transform teleportDestination;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer doorSprite;
    [SerializeField] private Sprite doorOpen, doorClosed;
    [SerializeField] private Color lockedColor = Color.red;
    [SerializeField] private Color unlockedColor = Color.green;

    [Header("Audio/VFX")]
    [SerializeField] private AudioClip unlockSound;
    [SerializeField] private ParticleSystem unlockParticles;

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
            doorSprite.sprite = isLocked.Value ? doorClosed : doorOpen;
        }
    }

    public void ServerLock()
    {
        if (!IsServer) return;
        if (isLocked.Value) return; // Already locked

        isLocked.Value = true;

        Debug.Log("[RoomDoor] Door locked");
    }

    public void ServerUnlock()
    {
        if (!IsServer) return;
        if (!isLocked.Value) return; // Already unlocked, don't trigger again

        isLocked.Value = false;

        // Play unlock effects on all clients
        PlayUnlockEffectsClientRpc();
    }

    [ClientRpc]
    private void PlayUnlockEffectsClientRpc()
    {
        // Play sound
        if (unlockSound != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuClick(); // Or create PlayDoorUnlock()
        }

        // Play particles
        if (unlockParticles != null)
        {
            unlockParticles.Play();
        }

        // Optional: Flash the sprite
        if (doorSprite != null)
        {
            StartCoroutine(FlashUnlock());
        }
    }

    private System.Collections.IEnumerator FlashUnlock()
    {
        // Flash green 3 times
        for (int i = 0; i < 3; i++)
        {
            doorSprite.color = Color.green;
            yield return new WaitForSeconds(0.1f);
            doorSprite.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
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
