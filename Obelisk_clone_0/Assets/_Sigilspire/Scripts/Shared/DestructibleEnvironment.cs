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
        UpdateVisuals();
    }

    public override void OnNetworkDespawn()
    {
        isDestroyed.OnValueChanged -= OnDestroyedChanged;
    }

    public void ServerHitByCharge(BossAbilityController boss)
    {
        if (!IsServer) return;
        if (isDestroyed.Value) return;

        // stun boss
        boss.ServerApplyStun(stunDuration);

        // apply damage debuff to boss
        boss.ServerApplyDamageBuff(damageBuffAmount, buffDuration, buff);

        // Destroy Pillar
        isDestroyed.Value = true;

        Debug.Log($"DestructibleEnv] Hit! Boss gets {damageBuffAmount * 100}% damage taken for {buffDuration}s");
    }

    private void OnDestroyedChanged(bool oldVal, bool newVal)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if(sprite != null) sprite.sprite = isDestroyed.Value ? brokenSprite : normalSprite;
    }

    public bool IsDestroyed => isDestroyed.Value;
}
