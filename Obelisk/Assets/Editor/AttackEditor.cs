using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Attack))]
public class AttackEditor : Editor
{
    // Basic Info
    SerializedProperty attackName;
    SerializedProperty isRanged;
    SerializedProperty isSpecialAttack;
    SerializedProperty canChainCombo;

    // Animation Arrays
    SerializedProperty attackAnimations;
    SerializedProperty blockAnimations;
    SerializedProperty grappleAnimations;
    SerializedProperty runAnimations;
    SerializedProperty climbAnimations;
    SerializedProperty potionAnimations;
    SerializedProperty interactAnimations;
    SerializedProperty jumpAnimations;
    SerializedProperty shootAnimations;
    SerializedProperty useItemAnimations;
    SerializedProperty deathAnimations;

    // Combat Stats
    SerializedProperty baseDamage;
    SerializedProperty damageModifier;
    SerializedProperty knockbackForce;
    SerializedProperty cooldown;
    SerializedProperty weight;

    // Projectile / Grapple Data
    SerializedProperty projectilePrefab;
    SerializedProperty pullSpeed;
    SerializedProperty maxRange;
    SerializedProperty hookSprite;

    // Foldout states
    bool showAnimations = true;
    bool showCombatStats = true;
    bool showProjectileData = false;
    bool showGrappleData = false;

    void OnEnable()
    {
        // Basic Info
        attackName = serializedObject.FindProperty("attackName");
        isRanged = serializedObject.FindProperty("isRanged");
        isSpecialAttack = serializedObject.FindProperty("isSpecialAttack");
        canChainCombo = serializedObject.FindProperty("canChainCombo");

        // Animations
        attackAnimations = serializedObject.FindProperty("attackAnimations");
        blockAnimations = serializedObject.FindProperty("blockAnimations");
        grappleAnimations = serializedObject.FindProperty("grappleAnimations");
        runAnimations = serializedObject.FindProperty("runAnimations");
        climbAnimations = serializedObject.FindProperty("climbAnimations");
        potionAnimations = serializedObject.FindProperty("potionAnimations");
        interactAnimations = serializedObject.FindProperty("interactAnimations");
        jumpAnimations = serializedObject.FindProperty("jumpAnimations");
        shootAnimations = serializedObject.FindProperty("shootAnimations");
        useItemAnimations = serializedObject.FindProperty("useItemAnimations");
        deathAnimations = serializedObject.FindProperty("deathAnimations");

        // Combat Stats
        baseDamage = serializedObject.FindProperty("baseDamage");
        damageModifier = serializedObject.FindProperty("damageModifier");
        knockbackForce = serializedObject.FindProperty("knockbackForce");
        cooldown = serializedObject.FindProperty("cooldown");
        weight = serializedObject.FindProperty("weight");

        // Projectile / Grapple
        projectilePrefab = serializedObject.FindProperty("projectilePrefab");
        pullSpeed = serializedObject.FindProperty("pullSpeed");
        maxRange = serializedObject.FindProperty("maxRange");
        hookSprite = serializedObject.FindProperty("hookSprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // --- Basic Info ---
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(attackName);
        EditorGUILayout.PropertyField(isRanged);
        EditorGUILayout.PropertyField(isSpecialAttack);
        EditorGUILayout.PropertyField(canChainCombo);

        EditorGUILayout.Space();

        // --- Animations ---
        showAnimations = EditorGUILayout.Foldout(showAnimations, "Animations (8 Directions)", true);
        if (showAnimations)
        {
            DrawAnimationArray("Attack Animations", attackAnimations);
            DrawAnimationArray("Block Animations", blockAnimations);
            DrawAnimationArray("Grapple Animations", grappleAnimations);
            DrawAnimationArray("Run Animations", runAnimations);
            DrawAnimationArray("Climb Animations", climbAnimations);
            DrawAnimationArray("Potion Animations", potionAnimations);
            DrawAnimationArray("Interact Animations", interactAnimations);
            DrawAnimationArray("Jump Animations", jumpAnimations);
            DrawAnimationArray("Shoot Animations", shootAnimations);
            DrawAnimationArray("UseItem Animations", useItemAnimations);
            DrawAnimationArray("Death Animations", deathAnimations);
        }

        EditorGUILayout.Space();

        // --- Combat Stats ---
        showCombatStats = EditorGUILayout.Foldout(showCombatStats, "Combat Stats", true);
        if (showCombatStats)
        {
            EditorGUILayout.PropertyField(baseDamage);
            EditorGUILayout.PropertyField(damageModifier);
            EditorGUILayout.PropertyField(knockbackForce);
            EditorGUILayout.PropertyField(cooldown);
            EditorGUILayout.PropertyField(weight);
        }

        // --- Projectile Data ---
        if (isRanged.boolValue)
        {
            EditorGUILayout.Space();
            showProjectileData = EditorGUILayout.Foldout(showProjectileData, "Projectile Data", true);
            if (showProjectileData)
            {
                EditorGUILayout.PropertyField(projectilePrefab);
            }
        }

        // --- Grapple Data ---
        if (attackName.stringValue.ToLower().Contains("grapple"))
        {
            EditorGUILayout.Space();
            showGrappleData = EditorGUILayout.Foldout(showGrappleData, "Grapple Data", true);
            if (showGrappleData)
            {
                EditorGUILayout.PropertyField(pullSpeed);
                EditorGUILayout.PropertyField(maxRange);
                EditorGUILayout.PropertyField(hookSprite);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Helper function to draw arrays of 8-direction animation clips
    private void DrawAnimationArray(string label, SerializedProperty arrayProp)
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            EditorGUILayout.PropertyField(arrayProp.GetArrayElementAtIndex(i), new GUIContent(((Direction)i).ToString()));
        }
        EditorGUI.indentLevel--;
    }
}

