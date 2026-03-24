using Enemy;
using System;
using Unity.Netcode;
using UnityEngine;
using Pathfinding;

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
        if (!IsServer) return;
        if (isDestroyed.Value) return;
        if (boss == null)
        {
            return;
        }

        // stun boss
        boss.ServerApplyStun(stunDuration);

        // apply damage debuff to boss
        boss.ServerApplyDamageBuff(damageBuffAmount, buffDuration, buff);

        // Destroy Pillar
        isDestroyed.Value = true;
    }

    public void ServerResetObject()
    {
        if (!IsServer) return;

        isDestroyed.Value= false;

        // re-enable collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // Update Visuals
        UpdateVisuals();
    }

    private void OnDestroyedChanged(bool oldVal, bool newVal)
    {
        UpdateVisuals();
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) { col.enabled = false; }

        if (newVal) UpdatePathfindingGrid();
    }

    private void UpdateVisuals()
    {
        if (sprite != null) sprite.sprite = isDestroyed.Value ? brokenSprite : normalSprite;
        if (normalSprite == null || brokenSprite == null) sprite.color = isDestroyed.Value ? Color.red : Color.white;
    }

    private void UpdatePathfindingGrid()
    {
        if (AstarPath.active == null) return;
        
        // Get bounds of this pillar's collider
        Collider2D col = GetComponentInParent<Collider2D>();
        if(col == null) return;

        Bounds bounds = col.bounds;

        // add padding to ensure we update surrounding area
        bounds.Expand(1.5f);

        // Update the grid in this area
        var guo = new GraphUpdateObject(bounds);
        AstarPath.active.UpdateGraphs(guo);
    }

    public bool IsDestroyed => isDestroyed.Value;
}
