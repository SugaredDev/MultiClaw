using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace MultiClaw.Core
{

public static class Constants
{
    public const string Path_Branches = "Assets/Plugins/MultiClaw/Resources/Game Versions";
    public const string Resources_Active = "Game Versions/Active Version";
    public const string Path_Active = "Assets/Plugins/MultiClaw/Resources/Game Versions/Active Version.asset";
    public const string Resources_Indicator = "Version Indication Font/Indicator";
    public const string Path_Indicator = "Assets/Plugins/MultiClaw/Resources/Version Indication Font";
    public const string Path_SteamBranchPresets = "Assets/Plugins/MultiClaw/Resources/Steam Branch Presets.asset";
    public const string Resources_SteamBranchPresets = "Steam Branch Presets";

    public static GameBranch GetActiveBranch() => Resources.Load<GameBranch>(Resources_Active);

#if UNITY_EDITOR
    public static GameBranch EnsureActiveBranchExists()
    {
        var activeBranch = AssetDatabase.LoadAssetAtPath<GameBranch>(Path_Active);
        
        if (activeBranch == null)
        {
            Debug.LogWarning("Active Version.asset not found in Resources folder. Creating a new one.");
            
            if (!Directory.Exists(Path_Branches))
                Directory.CreateDirectory(Path_Branches);
            
            activeBranch = ScriptableObject.CreateInstance<GameBranch>();
            activeBranch.name = "Active Version";
            activeBranch.title = "Dev";
            activeBranch.fileName = "Development";
            
            AssetDatabase.CreateAsset(activeBranch, Path_Active);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Active Version.asset created successfully.");
        }
        
        return activeBranch;
    }
    
    public static Rememberer EnsureSteamBranchPresetsExists()
    {
        var presets = AssetDatabase.LoadAssetAtPath<Rememberer>(Path_SteamBranchPresets);
        
        if (presets == null)
        {
            string resourcesPath = "Assets/Plugins/MultiClaw/Resources";
            if (!Directory.Exists(resourcesPath))
                Directory.CreateDirectory(resourcesPath);
            
            presets = ScriptableObject.CreateInstance<Rememberer>();
            presets.name = "Steam Branch Presets";
            
            AssetDatabase.CreateAsset(presets, Path_SteamBranchPresets);
            AssetDatabase.SaveAssets();
            
            Debug.Log("Steam Branch Presets.asset created successfully.");
        }
        
        return presets;
    }
#endif

    public static bool IsDebugBranch(GameBranch branch)
    {
        return branch != null && branch.debug;
    }

}

}
