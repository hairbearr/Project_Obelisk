using UnityEngine;
using UnityEditor;
using System.IO;

public class SetIsometricPivots : EditorWindow
{
    private string folderPath = "Assets/_Sigilspire/Art/Tiles"; // Change this to your tile folder path
    private Vector2 pivotPoint = new Vector2(0.5f, 0.18f);

    [MenuItem("Tools/Set Isometric Pivots")]
    static void ShowWindow()
    {
        GetWindow<SetIsometricPivots>("Set Isometric Pivots");
    }

    void OnGUI()
    {
        GUILayout.Label("Isometric Tile Pivot Setter", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        folderPath = EditorGUILayout.TextField("Tile Folder Path:", folderPath);
        pivotPoint = EditorGUILayout.Vector2Field("Pivot Point:", pivotPoint);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This will set pivot points for ALL sprites in the specified folder.\n\nDefault: X=0.5, Y=0.18 for isometric tiles", MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Set Pivots", GUILayout.Height(30)))
        {
            SetPivots();
        }
    }

    void SetPivots()
    {
        if (!Directory.Exists(folderPath))
        {
            EditorUtility.DisplayDialog("Error", $"Folder not found: {folderPath}", "OK");
            return;
        }

        // Find all texture assets in folder and subfolders
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("No Sprites Found", "No texture assets found in the specified folder.", "OK");
            return;
        }

        int count = 0;

        EditorUtility.DisplayProgressBar("Setting Pivots", "Processing sprites...", 0f);

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            // Show progress
            float progress = (float)i / guids.Length;
            EditorUtility.DisplayProgressBar("Setting Pivots", $"Processing {i + 1}/{guids.Length}: {Path.GetFileName(path)}", progress);

            // Get texture importer
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null && importer.textureType == TextureImporterType.Sprite)
            {
                // Get sprite metadata
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                // Set pivot
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = pivotPoint;

                // Apply settings
                importer.SetTextureSettings(settings);

                // Mark as dirty and reimport
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                count++;
            }
        }

        EditorUtility.ClearProgressBar();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Complete", $"Set pivot points for {count} sprites!", "OK");
    }
}
