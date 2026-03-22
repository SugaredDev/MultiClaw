using UnityEditor;
using UnityEngine;
using System.IO;

namespace MultiClaw
{

public class VersionNumeral : EditorWindow
{

    Font chosenFont;

    [MenuItem("Tools/MultiClaw/Version Numeral", false, 12)]
    static void ShowWindow()
    {
        GetWindow<VersionNumeral>("MultiClaw | Version Numeral");
    }

    void OnEnable()
    {
        LoadCurrentFont();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Version Indicator Font", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        Font resourceFont = Resources.Load<Font>(Constants.Resources_Indicator);

        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField($"Current Version: {PlayerSettings.bundleVersion}", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = Color.cyan;
        if (GUILayout.Button("Major Update", GUILayout.Height(30)))
            IncrementMajor();
        
        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button("Minor Update", GUILayout.Height(30)))
            IncrementMinor();
        
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Debug Update", GUILayout.Height(30)))
            IncrementDebug();
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Major (0.X.0.000)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Minor (0.0.X.000)", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("Debug (0.0.0.XXX)", EditorStyles.miniLabel);
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");
        
        EditorGUILayout.LabelField("Change Font for Version Indication:", EditorStyles.label);
        chosenFont = (Font)EditorGUILayout.ObjectField(chosenFont, typeof(Font), false);
        
        EditorGUILayout.Space();
        
        GUI.enabled = chosenFont != null;
        GUI.backgroundColor = resourceFont != null ? Color.green : Color.red;
        
        if (GUILayout.Button("Update Indicator Font"))
            UpdateFont();
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        if (IsFontSetCorrectly())
        {
            EditorGUILayout.Space(5);
            GUI.color = Color.green;
            EditorGUILayout.LabelField("✓ Font is set correctly", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        
        EditorGUILayout.EndVertical();
        
        if (resourceFont == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("No Indicator font is currently set. The version indicator will use the default Unity font.", MessageType.Warning);
        }
    }

    void LoadCurrentFont()
    {
        chosenFont = Resources.Load<Font>(Constants.Resources_Indicator);
    }

    bool IsFontSetCorrectly()
    {
        if (chosenFont == null)
            return false;

        Font resourceFont = Resources.Load<Font>(Constants.Resources_Indicator);
        if (resourceFont == null)
            return false;

        string chosenPath = AssetDatabase.GetAssetPath(chosenFont);
        string resourcePath = AssetDatabase.GetAssetPath(resourceFont);

        return chosenPath == resourcePath;
    }

    void UpdateFont()
    {
        if (chosenFont == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a font first.", "OK");
            return;
        }

        if (!Directory.Exists(Constants.Path_Indicator))
            Directory.CreateDirectory(Constants.Path_Indicator);

        string sourcePath = AssetDatabase.GetAssetPath(chosenFont);
        
        if (string.IsNullOrEmpty(sourcePath))
        {
            EditorUtility.DisplayDialog("Error", "Could not find the selected font asset.", "OK");
            return;
        }

        string extension = Path.GetExtension(sourcePath);
        string destinationPath = Constants.Path_Indicator + "/Indicator" + extension;

        if (File.Exists(destinationPath))
            AssetDatabase.DeleteAsset(destinationPath);

        AssetDatabase.CopyAsset(sourcePath, destinationPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Indicator font updated successfully!", "OK");
    }

    void IncrementMajor()
    {
        string version = PlayerSettings.bundleVersion;
        string[] parts = version.Split('.');

        if (parts.Length != 4)
        {
            Debug.LogWarning("Build version is not in the correct format. Resetting to '0.1.0.000'.");
            PlayerSettings.bundleVersion = "0.1.0.000";
            return;
        }

        if (!int.TryParse(parts[1], out int major))
            major = 0;

        major++;

        string newVersion = $"{parts[0]}.{major}.0.000";
        PlayerSettings.bundleVersion = newVersion;

        Debug.Log($"Project Version updated to {newVersion} (Major Update)");
    }

    void IncrementMinor()
    {
        string version = PlayerSettings.bundleVersion;
        string[] parts = version.Split('.');

        if (parts.Length != 4)
        {
            Debug.LogWarning("Build version is not in the correct format. Resetting to '0.0.1.000'.");
            PlayerSettings.bundleVersion = "0.0.1.000";
            return;
        }

        if (!int.TryParse(parts[1], out int major))
            major = 0;
        if (!int.TryParse(parts[2], out int minor))
            minor = 0;

        minor++;

        string newVersion = $"{parts[0]}.{major}.{minor}.000";
        PlayerSettings.bundleVersion = newVersion;

        Debug.Log($"Project Version updated to {newVersion} (Minor Update)");
    }

    void IncrementDebug()
    {
        string version = PlayerSettings.bundleVersion;
        string[] parts = version.Split('.');

        if (parts.Length != 4)
        {
            Debug.LogWarning("Build version is not in the correct format. Resetting to '0.0.0.001'.");
            PlayerSettings.bundleVersion = "0.0.0.001";
            return;
        }

        if (!int.TryParse(parts[1], out int major))
            major = 0;
        if (!int.TryParse(parts[2], out int minor))
            minor = 0;
        if (!int.TryParse(parts[3], out int debug))
            debug = 0;

        debug++;

        string newVersion = $"{parts[0]}.{major}.{minor}.{debug:D3}";
        PlayerSettings.bundleVersion = newVersion;

        Debug.Log($"Project Version updated to {newVersion} (Debug Update)");
    }

}

}