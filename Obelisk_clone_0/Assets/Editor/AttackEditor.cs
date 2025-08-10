using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Attack))]
public class AttackEditor : Editor
{
    private readonly string[] directionLabels =
    {
        "East", "North", "NorthEast", "NorthWest",
        "South", "SouthEast", "SouthWest", "West"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty attackName = serializedObject.FindProperty("attackName");
        SerializedProperty isRanged = serializedObject.FindProperty("isRanged");
        SerializedProperty directionalAnimations = serializedObject.FindProperty("directionalAnimations");
        SerializedProperty baseDamage = serializedObject.FindProperty("baseDamage");
        SerializedProperty damageModifier = serializedObject.FindProperty("damageModifier");
        SerializedProperty knockbackForce = serializedObject.FindProperty("knockbackForce");
        SerializedProperty cooldown = serializedObject.FindProperty("cooldown");
        SerializedProperty weight = serializedObject.FindProperty("weight");
        SerializedProperty projectilePrefab = serializedObject.FindProperty("projectilePrefab");

        EditorGUILayout.PropertyField(attackName);
        EditorGUILayout.PropertyField(isRanged);

        // Show directional animations with custom labels
        for (int i = 0; i < directionalAnimations.arraySize; i++)
        {
            SerializedProperty anim = directionalAnimations.GetArrayElementAtIndex(i);
            EditorGUILayout.PropertyField(anim, new GUIContent(directionLabels[i] + " Animation"));
        }

        EditorGUILayout.PropertyField(baseDamage);
        EditorGUILayout.PropertyField(damageModifier);
        EditorGUILayout.PropertyField(knockbackForce);
        EditorGUILayout.PropertyField(cooldown);
        EditorGUILayout.PropertyField(weight);
        EditorGUILayout.PropertyField(projectilePrefab);

        serializedObject.ApplyModifiedProperties();
    }
}

