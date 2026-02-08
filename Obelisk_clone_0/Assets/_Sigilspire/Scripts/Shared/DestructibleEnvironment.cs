using Enemy;
using System;
using Unity.Netcode;
using UnityEngine;

public class DestructibleEnvironment : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damageBuffAmount = 0.25f; // 25% increased damage taken
    [SerializeField] private float buffDuration = 10f;
    [SerializeField] private bool buff = false; // if it's a buff or debuff
    [SerializeField] private float stunDuration = 2f; // how long boss is stunned for

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite brokenSprite;

    private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        isDestroyed.OnValueChanged += OnDestroyedChanged;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) { col.enabled = true; }
        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isDestroyed.OnValueChanged -= OnDestroyedChanged;
    }

    public void ServerHitByCharge(BossAbilityController boss)
    {
        Debug.Log($"[Pillar] ServerHitByCharge called! IsServer={IsServer}, boss={boss != null}, isDestroyed={isDestroyed.Value}");

        if (!IsServer) return;
        if (isDestroyed.Value) return;
        if (boss == null)
        {
            Debug.LogError("[Pillar] Boss is NULL!");
            return;
        }

        Debug.Log($"[Pillar] About to stun boss for {stunDuration}s");

        // stun boss
        boss.ServerApplyStun(stunDuration);

        Debug.Log($"[Pillar] About to apply damage debuff");

        // apply damage debuff to boss
        boss.ServerApplyDamageBuff(damageBuffAmount, buffDuration, buff);

        // Destroy Pillar
        isDestroyed.Value = true;

        Debug.Log($"DestructibleEnv] Hit! Boss gets {damageBuffAmount * 100}% damage taken for {buffDuration}s");
    }

    private void OnDestroyedChanged(bool oldVal, bool newVal)
    {
        UpdateVisuals();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) { col.enabled = false; }
    }

    private void UpdateVisuals()
    {
        if (sprite != null) sprite.sprite = isDestroyed.Value ? brokenSprite : normalSprite;
        if (normalSprite == null || brokenSprite == null) sprite.color = isDestroyed.Value ? Color.red : Color.white;
    }

    public bool IsDestroyed => isDestroyed.Value;
}
