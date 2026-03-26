using Combat.AbilitySystem;
using System;
using Unity.Netcode;
using UnityEngine;

namespace Player
{
    public class PlayerLoadout : NetworkBehaviour
    {
        #region Data Structures

        [Serializable]
        public class EquippedSigils
        {
            [Header("Majors")]
            public string swordMajorId;
            public string shieldMajorId;
            public string grappleMajorId;

            [Header("Minors")]
            public int amountEquippable = 2;
            public string[] swordMinorIds = new string[2];
            public string[] shieldMinorIds = new string[2];
            public string[] grappleMinorIds = new string[2];
        }

        #endregion

        #region Serialized Fields

        [Header("Current Loadout")]
        [SerializeField] private EquippedSigils equipped = new EquippedSigils();

        [Header("References")]
        [SerializeField] private SigilInventory inventory;
        [SerializeField] private Combat.SwordController sword;
        [SerializeField] private Combat.ShieldController shield;
        [SerializeField] private Combat.GrapplingHookController grapple;

        #endregion

        #region Unity Lifecycle

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

        #endregion

        #region Public API - Equip Sigils

        public void EquipMajor(WeaponSlot slot, string sigilId)
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
                    equipped.swordMajorId = sigilId;
                    break;
                case WeaponSlot.Shield:
                    equipped.shieldMajorId = sigilId;
                    break;
                case WeaponSlot.Grapple:
                    equipped.grappleMajorId = sigilId;
                    break;
            }

            ApplyCurrentLoadout();
        }

        public void EquipMinor(WeaponSlot slot, int minorSlotIndex, string sigilId)
        {
            if (!IsOwner) return;
            if (minorSlotIndex < 0 || minorSlotIndex > 1) return;

            // Validate compatibility
            var def = inventory?.GetDefinition(sigilId);
            if (def != null && def.sigilType == SigilType.Minor)
            {
                // Check if Minor is compatible with this weapon
                bool compatible = def.compatibility == SigilCompatibility.Universal ||
                                 (def.compatibility == SigilCompatibility.SwordOnly && slot == WeaponSlot.Sword) ||
                                 (def.compatibility == SigilCompatibility.ShieldOnly && slot == WeaponSlot.Shield) ||
                                 (def.compatibility == SigilCompatibility.GrappleOnly && slot == WeaponSlot.Grapple);

                if (!compatible)
                {
                    Debug.LogWarning($"Minor {sigilId} is not compatible with {slot}");
                    return;
                }
            }

            switch (slot)
            {
                case WeaponSlot.Sword:
                    equipped.swordMinorIds[minorSlotIndex] = sigilId;
                    break;
                case WeaponSlot.Shield:
                    equipped.shieldMinorIds[minorSlotIndex] = sigilId;
                    break;
                case WeaponSlot.Grapple:
                    equipped.grappleMinorIds[minorSlotIndex] = sigilId;
                    break;
            }

            ApplyCurrentLoadout();
        }

        #endregion

        #region Public API - Getters

        public string GetEquippedMajorId(WeaponSlot slot)
        {
            return slot switch
            {
                WeaponSlot.Sword => equipped.swordMajorId,
                WeaponSlot.Shield => equipped.shieldMajorId,
                WeaponSlot.Grapple => equipped.grappleMajorId,
                _ => null
            };
        }

        public string[] GetEquippedMinorIds(WeaponSlot slot)
        {
            return slot switch
            {
                WeaponSlot.Sword => equipped.swordMinorIds,
                WeaponSlot.Shield => equipped.shieldMinorIds,
                WeaponSlot.Grapple => equipped.grappleMinorIds,
                _ => new string[0]
            };
        }

        #endregion

        #region Apply Loadout

        public void ApplyCurrentLoadout()
        {
            if (inventory == null) return;

            // Sword
            if (sword != null)
            {
                var major = inventory.GetDefinition(equipped.swordMajorId);
                var minors = new System.Collections.Generic.List<SigilDefinition>();

                foreach (var minorId in equipped.swordMinorIds)
                {
                    if (!string.IsNullOrEmpty(minorId))
                    {
                        var minor = inventory.GetDefinition(minorId);
                        if (minor != null) minors.Add(minor);
                    }
                }

                sword.SetEquippedSigils(major, minors);
                if (major != null) sword.ApplyVisualSet(major.visualSet);
            }

            // Shield
            if (shield != null)
            {
                var major = inventory.GetDefinition(equipped.shieldMajorId);
                var minors = new System.Collections.Generic.List<SigilDefinition>();

                foreach (var minorId in equipped.shieldMinorIds)
                {
                    if (!string.IsNullOrEmpty(minorId))
                    {
                        var minor = inventory.GetDefinition(minorId);
                        if (minor != null) minors.Add(minor);
                    }
                }

                shield.SetEquippedSigils(major, minors);
                if (major != null) shield.ApplyVisualSet(major.visualSet);
            }

            // Grapple
            if (grapple != null)
            {
                var major = inventory.GetDefinition(equipped.grappleMajorId);
                var minors = new System.Collections.Generic.List<SigilDefinition>();

                foreach (var minorId in equipped.grappleMinorIds)
                {
                    if (!string.IsNullOrEmpty(minorId))
                    {
                        var minor = inventory.GetDefinition(minorId);
                        if (minor != null) minors.Add(minor);
                    }
                }

                grapple.SetEquippedSigils(major, minors);
                if (major != null) grapple.ApplyVisualSet(major.visualSet);
            }
        }

        #endregion

        #region Testing Helpers

        // TODO: REMOVE WHEN YOU HAVE PROPER UI
        public void EquipSigilsForTesting(SigilDefinition sword, SigilDefinition shield, SigilDefinition grapple)
        {
            if (sword != null) equipped.swordMajorId = sword.id;
            if (shield != null) equipped.shieldMajorId = shield.id;
            if (grapple != null) equipped.grappleMajorId = grapple.id;

            ApplyCurrentLoadout();
        }

        #endregion
    }
}
