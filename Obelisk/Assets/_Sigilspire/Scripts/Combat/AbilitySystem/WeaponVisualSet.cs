using UnityEngine;

namespace Combat.AbilitySystem
{
    [CreateAssetMenu(fileName = "WeaponVisualSet", menuName = "Sigilspire/Weapon Visual Set")]
    public class WeaponVisualSet : ScriptableObject
    {
        [Header("Animator Overrides")]
        public AnimatorOverrideController overrideController;

        [Header("Sprites (optional if using Animator + Sprite Library)")]
        public Sprite idleSprite;
        public Sprite[] directionalSlashes; // optional 8-way sprites
        public Sprite specialSprite;

        [Header("VFX")]
        public GameObject attackVfx;
        public GameObject specialVfx;

        [Header("Audio")]
        public AudioClip swingSound;
        public AudioClip hitSound;
        public AudioClip specialSound;
    }
}

