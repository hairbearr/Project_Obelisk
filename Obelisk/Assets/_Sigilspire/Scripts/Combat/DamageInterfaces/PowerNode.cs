using UnityEngine;
using Unity.Netcode;
using Combat.DamageInterfaces;
using UnityEngine.UI;

public class PowerNode : NetworkBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 50f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color activeColor = Color.cyan;
    [SerializeField] private Color destroyedColor = Color.gray;
    [SerializeField] private Slider slider;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            currentHealth.Value = maxHealth;
            isDestroyed.Value = false;
        }

        isDestroyed.OnValueChanged += OnDestroyedChanged;
        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isDestroyed.OnValueChanged -= OnDestroyedChanged;
    }

    // IDamageable implementation
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, 0);
    }

    public void TakeDamage(float amount, ulong attackerId)
    {
        if (!IsServer) return;
        if (isDestroyed.Value) return;

        currentHealth.Value -= amount;

        // Visual feedback
        FlashClientRpc();

        if(currentHealth.Value <= 0f)
        {
            currentHealth.Value = 0f;
            DestroyNode();
        }
    }

    private void DestroyNode()
    {
        if (!IsServer)
        {
            return;
        }

        isDestroyed.Value = true;

        // Notify puzzle manager
        var puzzleManager = FindFirstObjectByType<PuzzleManager>();
        if (puzzleManager != null)
        {
            puzzleManager.ServerNotifyNodeDestroyed();
        }

    }

    [ClientRpc]
    private void FlashClientRpc()
    {
        if (sprite != null)
        {
            StartCoroutine(FlashEffect());
        }
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        if (sprite == null) yield break;

        Color original = sprite.color;
        sprite.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        sprite.color = original;
    }

    private void OnDestroyedChanged(bool oldVal, bool newVal)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (sprite == null) return;

        sprite.color = isDestroyed.Value ? destroyedColor : activeColor;

        // disable collider when destroyed
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = !isDestroyed.Value;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
