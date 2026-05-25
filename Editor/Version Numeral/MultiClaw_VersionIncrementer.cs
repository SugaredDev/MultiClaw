using UnityEditor;
using UnityEngine;

namespace MultiClaw.Core
{

[InitializeOnLoad]
public class BranchIncrementer
{
    
    static BranchIncrementer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Constants.EnsureActiveBranchExists();
            IncrementRevision();
        }
    }

    static void IncrementRevision()
    {
        string branch = PlayerSettings.bundleVersion;
        string[] parts = branch.Split('.');

        if (parts.Length != 4)
        {
            Debug.LogWarning("Build branch is not in the correct format. => Correcting to '0.0.0.000'.");
            parts = new string[] { "0", "0", "0", "000" };
        }

        if (!int.TryParse(parts[3], out int revision))
            revision = 0;

        revision++;

        string newBranch = $"{parts[0]}.{parts[1]}.{parts[2]}.{revision:D3}";

        PlayerSettings.bundleVersion = newBranch;

        Debug.Log($"Project Version updated to {newBranch}");
    }

}

}