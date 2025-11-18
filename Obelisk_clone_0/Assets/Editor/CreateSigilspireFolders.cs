using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateSigilspireFolders : EditorWindow
{
    [MenuItem("Tools/Sigilspire/Create Folder Structure")]
    public static void CreateFolders()
    {
        string root = "Assets/_Sigilspire";

        string[] folders = new string[]
        {
            $"{root}",
            $"{root}/Animations",
            $"{root}/Animations/Player",
            $"{root}/Animations/Player/Idle",
            $"{root}/Animations/Player/Run",
            $"{root}/Animations/Player/Actions",
            $"{root}/Animations/Player/BlendTrees",

            $"{root}/Animations/Weapons",
            $"{root}/Animations/Weapons/Sword",
            $"{root}/Animations/Weapons/Sword/Idle",
            $"{root}/Animations/Weapons/Sword/Run",
            $"{root}/Animations/Weapons/Sword/Slash",
            $"{root}/Animations/Weapons/Sword/Sigils",
            $"{root}/Animations/Weapons/Sword/Sigils/Default",
            $"{root}/Animations/Weapons/Sword/Sigils/Fire",
            $"{root}/Animations/Weapons/Sword/Sigils/Ice",
            $"{root}/Animations/Weapons/Sword/Sigils/Shock",
            $"{root}/Animations/Weapons/Sword/BlendTrees",

            $"{root}/Animations/Weapons/Shield",
            $"{root}/Animations/Weapons/Shield/Idle",
            $"{root}/Animations/Weapons/Shield/Run",
            $"{root}/Animations/Weapons/Shield/Block",
            $"{root}/Animations/Weapons/Shield/Break",   // FIXED
            $"{root}/Animations/Weapons/Shield/Sigils",
            $"{root}/Animations/Weapons/Shield/Sigils/Default",
            $"{root}/Animations/Weapons/Shield/Sigils/Fire",
            $"{root}/Animations/Weapons/Shield/BlendTrees",

            $"{root}/Animations/Weapons/Grapple",
            $"{root}/Animations/Weapons/Grapple/Idle",
            $"{root}/Animations/Weapons/Grapple/Run",
            $"{root}/Animations/Weapons/Grapple/Cast",
            $"{root}/Animations/Weapons/Grapple/Retract",
            $"{root}/Animations/Weapons/Grapple/Travel",
            $"{root}/Animations/Weapons/Grapple/Sigils",
            $"{root}/Animations/Weapons/Grapple/Sigils/Default",
            $"{root}/Animations/Weapons/Grapple/Sigils/Arcane",
            $"{root}/Animations/Weapons/Grapple/BlendTrees",

            $"{root}/Animations/Weapons/OverrideControllers",
            $"{root}/Animations/Weapons/OverrideControllers/Sword",
            $"{root}/Animations/Weapons/OverrideControllers/Shield",
            $"{root}/Animations/Weapons/OverrideControllers/Grapple",

            $"{root}/Animations/Enemies",
            $"{root}/Animations/Enemies/Idle",
            $"{root}/Animations/Enemies/Move",
            $"{root}/Animations/Enemies/Attack",
            $"{root}/Animations/Enemies/Death",
            $"{root}/Animations/Enemies/Bosses",

            $"{root}/Art",
            $"{root}/Art/Player",
            $"{root}/Art/Weapons",
            $"{root}/Art/Weapons/Sword",
            $"{root}/Art/Weapons/Shield",
            $"{root}/Art/Weapons/Grapple",
            $"{root}/Art/Environment",
            $"{root}/Art/UI",
            $"{root}/Art/Icons",
            $"{root}/Art/VFX",
            $"{root}/Art/VFX/Slash",
            $"{root}/Art/VFX/Impact",
            $"{root}/Art/VFX/Shield",
            $"{root}/Art/VFX/Grapple",

            $"{root}/Audio",
            $"{root}/Audio/SFX",
            $"{root}/Audio/SFX/Sword",
            $"{root}/Audio/SFX/Shield",
            $"{root}/Audio/SFX/Grapple",
            $"{root}/Audio/SFX/Enemies",
            $"{root}/Audio/SFX/UI",
            $"{root}/Audio/Music",

            $"{root}/Materials",

            $"{root}/Prefabs",
            $"{root}/Prefabs/Player",
            $"{root}/Prefabs/Enemies",
            $"{root}/Prefabs/Enemies/Minions",
            $"{root}/Prefabs/Enemies/Bosses",
            $"{root}/Prefabs/Environment",
            $"{root}/Prefabs/Loot",
            $"{root}/Prefabs/UI",

            $"{root}/Scenes",
            $"{root}/Scenes/Testbed",
            $"{root}/Scenes/MainMenu",
            $"{root}/Scenes/Overworld",
            $"{root}/Scenes/StoryLevels",
            $"{root}/Scenes/RoguelikeDungeons",

            $"{root}/ScriptableObjects",
            $"{root}/ScriptableObjects/Combat",
            $"{root}/ScriptableObjects/Combat/Abilities",
            $"{root}/ScriptableObjects/Combat/AbilityStats",
            $"{root}/ScriptableObjects/Combat/StatusEffects",
            $"{root}/ScriptableObjects/Combat/DamageTypes",

            $"{root}/ScriptableObjects/Sigils",
            $"{root}/ScriptableObjects/Sigils/SwordSigils",
            $"{root}/ScriptableObjects/Sigils/ShieldSigils",
            $"{root}/ScriptableObjects/Sigils/GrappleSigils",
            $"{root}/ScriptableObjects/Sigils/Shared",

            $"{root}/ScriptableObjects/Weapons/Sword/WeaponVisualSets",
            $"{root}/ScriptableObjects/Weapons/Shield/WeaponVisualSets",
            $"{root}/ScriptableObjects/Weapons/Grapple/WeaponVisualSets",

            $"{root}/ScriptableObjects/Progression",
            $"{root}/ScriptableObjects/LootTables",

            $"{root}/Scripts",
            $"{root}/Scripts/Combat",
            $"{root}/Scripts/Combat/AbilitySystem",
            $"{root}/Scripts/Combat/DamageInterfaces",
            $"{root}/Scripts/Combat/Health",
            $"{root}/Scripts/Combat/Projectiles",

            $"{root}/Scripts/Enemy",
            $"{root}/Scripts/Enemy/AI",
            $"{root}/Scripts/Enemy/Behaviors",
            $"{root}/Scripts/Enemy/Spawning",

            $"{root}/Scripts/Player",
            $"{root}/Scripts/Player/Controllers",
            $"{root}/Scripts/Player/Animation",
            $"{root}/Scripts/Player/Input",
            $"{root}/Scripts/Player/Networking",

            $"{root}/Scripts/Weapons/Sword",
            $"{root}/Scripts/Weapons/Shield",
            $"{root}/Scripts/Weapons/Grapple",

            $"{root}/Scripts/Loot",
            $"{root}/Scripts/Shared",
            $"{root}/Scripts/UI",

            $"{root}/Shaders",

            $"{root}/VFX",
            $"{root}/VFX/Particles",
            $"{root}/VFX/Slash",
            $"{root}/VFX/Shield",
            $"{root}/VFX/Grapple",
            $"{root}/VFX/HitEffects"
        };

        foreach (string folder in folders)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                Debug.Log("Created: " + folder);
            }
        }

        AssetDatabase.Refresh();
        Debug.Log("Sigilspire folder structure created under Assets/_Sigilspire!");
    }
}

