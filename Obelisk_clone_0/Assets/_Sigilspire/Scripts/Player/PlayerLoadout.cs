using Combat.AbilitySystem;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Manages which sigils are currently equipped on this player.
    /// Separate from SigilInventory (which tracks unlocks/progression).
    /// </summary>
    public class PlayerLoadout : NetworkBehaviour
    {
        [Serializable]
        public class EquippedSigils
        {
            public string swordSigilId;
            public string shieldSigilId;
            public string grappleSigilId;
        }

        [Header("Current Loadout")]
        [SerializeField] private EquippedSigils equipped = new EquippedSigils();

        [Header("References")]
        [SerializeField] private SigilInventory inventory;
        [SerializeField] private Combat.SwordController sword;
        [SerializeField] private Combat.ShieldController shield;
        [SerializeField] private Combat.GrapplingHookController grapple;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<SigilInventory>();
            if (sword == null) sword = GetComponentInChildren<Combat.SwordController>();
            if (shield == null) shield = GetComponentInChildren<Combat.ShieldController>();
            if (grapple == null) grapple = GetComponentInChildren<Combat.GrapplingHookController>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            // Apply loadout on spawn
            ApplyCurrentLoadout();
        }

        /// <summary>
        /// Equip a sigil in the specified slot. Call this from UI later.
        /// </summary>
        public void EquipSigil(WeaponSlot slot, string sigilId)
        {
            if (!IsOwner) return;

            // Validate: do we own this sigil?
            if (inventory != null && !string.IsNullOrEmpty(sigilId))
            {
                var progress = inventory.GetOrCreateProgress(sigilId);
                if (progress == null || progress.level < 1)
                {
                    Debug.LogWarning($"Cannot equip {sigilId} - not unlocked or level 0");
                    return;
                }
            }

            // Update loadout
            switch (slot)
            {
                case WeaponSlot.Sword:
                    equipped.swordSigilId = sigilId;
                    break;
                case WeaponSlot.Shield:
                    equipped.shieldSigilId = sigilId;
                    break;
                case WeaponSlot.Grapple:
                    equipped.grappleSigilId = sigilId;
                    break;
            }

            ApplyCurrentLoadout();
        }

        /// <summary>
        /// Apply currently equipped sigils to weapon controllers.
        /// </summary>
        public void ApplyCurrentLoadout()
        {
            if (inventory == null) return;

            // Sword
            if (sword != null)
            {
                var def = inventory.GetDefinition(equipped.swordSigilId);
                if (def != null)
                {
                    sword.SetEquippedSigil(def);
                    sword.ApplyVisualSet(def.visualSet);
                }
                else
                {
                    sword.SetEquippedSigil(null);
                }
            }

            // Shield
            if (shield != null)
            {
                var def = inventory.GetDefinition(equipped.shieldSigilId);
                if (def != null)
                {
                    shield.SetEquippedSigil(def);
                    shield.ApplyVisualSet(def.visualSet);
                }
                else
                {
                    shield.SetEquippedSigil(null);
                }
            }

            // Grapple
            if (grapple != null)
            {
                var def = inventory.GetDefinition(equipped.grappleSigilId);
                if (def != null)
                {
                    grapple.SetEquippedSigil(def);
                    grapple.ApplyVisualSet(def.visualSet);
                }
                else
                {
                    grapple.SetEquippedSigil(null);
                }
            }
        }

        public string GetEquippedSigilId(WeaponSlot slot)
        {
            return slot switch
            {
                WeaponSlot.Sword => equipped.swordSigilId,
                WeaponSlot.Shield => equipped.shieldSigilId,
                WeaponSlot.Grapple => equipped.grappleSigilId,
                _ => null
            };
        }

        /// <summary>
        /// For testing: equip sigils by their ScriptableObject references.
        /// Remove this once you have proper UI.
        /// </summary>
        public void EquipSigilsForTesting(SigilDefinition sword, SigilDefinition shield, SigilDefinition grapple)
        {
            if (sword != null) equipped.swordSigilId = sword.id;
            if (shield != null) equipped.shieldSigilId = shield.id;
            if (grapple != null) equipped.grappleSigilId = grapple.id;

            ApplyCurrentLoadout();
        }
    }
}
