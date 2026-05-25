using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiClaw.Core
{

public class BranchBuilder : EditorWindow
{

    [System.Serializable]
    public class BuildConfig { public GameBranch configAsset; public bool buildEnabled; }

    class PlatformInfo
    {
        public bool isSteamDeck;
        public BuildTarget target;
        public string folder;
        public string extension;
        public PlatformInfo(bool steamDeck, BuildTarget target, string folder, string ext)
        {
            this.isSteamDeck = steamDeck;
            this.target = target;
            this.folder = folder;
            this.extension = ext;
        }
    }

    List<BuildConfig> buildBranches = new();
    Vector2 scroll;
    GameBranch inEditorBranch;
    bool buildWindows, buildMac, buildLinux, buildSteamDeck;

    readonly PlatformInfo[] platforms = {
        new(false, BuildTarget.StandaloneWindows64, "Windows", ".exe"),
        new(false, BuildTarget.StandaloneLinux64, "Linux", ".x86_64"),
        new(true, BuildTarget.StandaloneLinux64, "Steam Deck", ".x86_64"),
        new(false, BuildTarget.StandaloneOSX, "macOS", ".app")
    };

    [MenuItem("Tools/MultiClaw/Version Builder", false, 0)]
    static void ShowWindow() => GetWindow<BranchBuilder>("MultiClaw | Version Builder");

    void OnEnable()
    {
        inEditorBranch = Constants.EnsureActiveBranchExists();
        RefreshBranchesList();
        EnsureActiveBranchMatchesAvailable();
    }

    void OnGUI()
    {
        GUI.backgroundColor = Color.gray;
        if (GUILayout.Button(PlayerSettings.bundleVersion, new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold }))
            EditorWindow.GetWindow<BranchNumeral>("Version Numeral");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build Versions", EditorStyles.boldLabel);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("+ New Version", GUILayout.Width(100)))
            CreateNewBranch();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < buildBranches.Count; i++) BranchEntry(i);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Box("", new GUIStyle(GUI.skin.box) { margin = new RectOffset(0, 0, 4, 4) }, GUILayout.ExpandWidth(true), GUILayout.Height(1));

        EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);
        buildWindows = ColoredToggle("Windows", buildWindows);
        buildLinux = ColoredToggle("Linux", buildLinux);
        buildSteamDeck = ColoredToggle("Steam Deck", buildSteamDeck);
        buildMac = ColoredToggle("macOS", buildMac);

        GUILayout.Space(10);
        bool[] platformStates = { buildWindows, buildLinux, buildSteamDeck, buildMac };
        bool canBuild = platformStates.Any(p => p) && buildBranches.Any(v => v.buildEnabled);
        
        List<string> missingSupport = new List<string>();
        
        if (buildWindows && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            missingSupport.Add("Windows");
        
        if ((buildLinux || buildSteamDeck) && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            missingSupport.Add(buildLinux && buildSteamDeck ? "Linux/Steam Deck" : buildLinux ? "Linux" : "Steam Deck");
        
        if (buildMac && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            missingSupport.Add("macOS");
        
        if (missingSupport.Count > 0)
        {
            canBuild = false;
            EditorGUILayout.HelpBox($"Missing build support for: {string.Join(", ", missingSupport)}. Install the required modules in Unity Hub.", MessageType.Error);
        }

        GUI.enabled = canBuild;
        GUI.backgroundColor = canBuild ? Color.green : Color.red;
        if (GUILayout.Button("Build Selected Versions", GUILayout.Height(40))) BuildAll();
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
    }

    bool ColoredToggle(string label, bool value)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, new GUIStyle(EditorStyles.label) { normal = { textColor = value ? Color.green : Color.red } }, GUILayout.Width(150));
        bool result = EditorGUILayout.Toggle(value);
        EditorGUILayout.EndHorizontal();
        return result;
    }

    void BranchEntry(int build)
    {
        var branch = buildBranches[build];
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        bool isActive = inEditorBranch != null && JsonUtility.ToJson(branch.configAsset) == JsonUtility.ToJson(inEditorBranch);
        bool isActiveAsset = AssetDatabase.GetAssetPath(branch.configAsset) == Constants.Path_Active;
        
        GUI.backgroundColor = isActive ? Color.yellow : Color.white;
        GUI.enabled = !isActiveAsset && branch.configAsset != null;

        if (GUILayout.Button(isActive ? "Active In-Editor" : "Set Active", GUILayout.Width(110)))
        {
            EditorUtility.CopySerialized(branch.configAsset, inEditorBranch);
            EditorUtility.SetDirty(inEditorBranch);
            inEditorBranch.name = "Active Version";
        }

        GUI.enabled = true;
        GUI.backgroundColor = branch.buildEnabled ? Color.green : Color.red;

        if (GUILayout.Button(branch.buildEnabled ? "Enabled for build" : "Disabled for build", GUILayout.Width(110)))
            branch.buildEnabled = !branch.buildEnabled;

        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        
        GUI.enabled = !isActiveAsset;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Version", $"Are you sure you want to delete '{branch.configAsset.title}'?", "Delete", "Cancel"))
            {
                var branchToDelete = branch.configAsset;
                EditorApplication.delayCall += () => DeleteBranch(branchToDelete);
            }
        }

        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (branch.configAsset != null)
        {
            GUI.enabled = !isActiveAsset;
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Title:", GUILayout.Width(140));
            branch.configAsset.title = EditorGUILayout.TextField(branch.configAsset.title);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Executable File Name:", GUILayout.Width(140));
            branch.configAsset.fileName = EditorGUILayout.TextField(branch.configAsset.fileName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Debug:", GUILayout.Width(140));
            branch.configAsset.debug = EditorGUILayout.Toggle(branch.configAsset.debug);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Steam API:", GUILayout.Width(140));
            branch.configAsset.steamAPI = (uint)EditorGUILayout.IntField((int)branch.configAsset.steamAPI);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Version Type:", GUILayout.Width(140));
            branch.configAsset.branchType = (BranchType)EditorGUILayout.EnumPopup(branch.configAsset.branchType);
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(branch.configAsset);
                AssetDatabase.SaveAssets();
            }
            
            GUI.enabled = true;
        }
        EditorGUILayout.EndVertical();
    }

    void BuildAll()
    {
        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No scenes enabled in Build Settings.", "OK");
            return;
        }

        string buildsRoot = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
        string originalJson = JsonUtility.ToJson(inEditorBranch);
        bool[] platformStates = { buildWindows, buildLinux, buildSteamDeck, buildMac };

        try
        {
            foreach (var branch in buildBranches.Where(v => v.buildEnabled && v.configAsset != null))
            {
                for (int i = 0; i < platforms.Length; i++)
                {
                    if (!platformStates[i]) continue;

                    EditorUtility.CopySerialized(branch.configAsset, inEditorBranch);
                    inEditorBranch.steamDeck = platforms[i].isSteamDeck;
                    inEditorBranch.name = "Active Version";
                    EditorUtility.SetDirty(inEditorBranch);
                    
                    AssetDatabase.SaveAssets();

                    BuildPlatform(branch, platforms[i], buildsRoot, scenes);
                }
            }
        }
        finally
        {
            JsonUtility.FromJsonOverwrite(originalJson, inEditorBranch);
            inEditorBranch.steamDeck = false;
            inEditorBranch.name = "Active Version";
            EditorUtility.SetDirty(inEditorBranch);
            AssetDatabase.SaveAssets();
        }
        
        EditorUtility.RevealInFinder(buildsRoot);
        
        int choice = EditorUtility.DisplayDialogComplex(
            "Builds Completed", 
            "All builds finished successfully!", 
            "Close", 
            "Open Steam Depoter", 
            "Close even harder"
        );
        
        if (choice == 1)
            EditorWindow.GetWindow<SteamDepoter>("Steam Depoter");
    }

    void BuildPlatform(BuildConfig branch, PlatformInfo platform, string root, string[] scenes)
    {
        string folder = Path.Combine(root, branch.configAsset.title, platform.folder);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, branch.configAsset.fileName + platform.extension);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = platform.target,
            options = BuildOptions.None
        });

        Debug.Log($"Build {(report.summary.result == BuildResult.Succeeded ? "Succeeded" : "Failed")}: {path}");
        
        string burstDebugFolder = Path.Combine(folder, PlayerSettings.productName + "_BurstDebugInformation_DoNotShip");
        if (Directory.Exists(burstDebugFolder))
        {
            try
            {
                Directory.Delete(burstDebugFolder, true);
            }
            catch
            {

            }
        }
    }

    void RefreshBranchesList()
    {
        buildBranches.Clear();
        buildWindows = buildMac = buildLinux = buildSteamDeck = false;
        
        if (!Directory.Exists(Constants.Path_Branches)) Directory.CreateDirectory(Constants.Path_Branches);

        var allBranches = AssetDatabase.FindAssets("t:GameBranch", new[] { Constants.Path_Branches })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<GameBranch>(path))
            .Where(asset => asset != null)
            .Select(asset => new BuildConfig { configAsset = asset })
            .ToList();
        
        var otherBranches = allBranches.Where(v => AssetDatabase.GetAssetPath(v.configAsset) != Constants.Path_Active).ToList();
        
        if (otherBranches.Count > 0)
            buildBranches = otherBranches;
        else
            buildBranches = allBranches;
        
        EnsureActiveBranchMatchesAvailable();
    }

    void CreateNewBranch()
    {
        if (!Directory.Exists(Constants.Path_Branches)) Directory.CreateDirectory(Constants.Path_Branches);
        
        int branchNumber = 1;
        string assetPath;
        
        do
        {
            assetPath = Path.Combine(Constants.Path_Branches, $"Build Version {branchNumber}.asset");
            branchNumber++;
        } while (AssetDatabase.LoadAssetAtPath<GameBranch>(assetPath) != null);
        
        var newBranch = CreateInstance<GameBranch>();
        
        AssetDatabase.CreateAsset(newBranch, assetPath);
        AssetDatabase.SaveAssets();
        
        EditorApplication.delayCall += () =>
        {
            RefreshBranchesList();
            EditorGUIUtility.PingObject(newBranch);
        };
    }

    void DeleteBranch(GameBranch branchToDelete)
    {
        if (branchToDelete == null) return;
        
        string assetPath = AssetDatabase.GetAssetPath(branchToDelete);
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.SaveAssets();
        
        RefreshBranchesList();
    }

    void EnsureActiveBranchMatchesAvailable()
    {
        if (inEditorBranch == null)
            inEditorBranch = Constants.EnsureActiveBranchExists();
        
        if (buildBranches.Count == 0) return;
        
        bool foundMatch = false;
        foreach (var branch in buildBranches)
        {
            if (branch.configAsset != null && JsonUtility.ToJson(branch.configAsset) == JsonUtility.ToJson(inEditorBranch))
            {
                foundMatch = true;
                break;
            }
        }
        
        if (!foundMatch && buildBranches.Count > 0 && buildBranches[0].configAsset != null && inEditorBranch != null)
        {
            EditorUtility.CopySerialized(buildBranches[0].configAsset, inEditorBranch);
            EditorUtility.SetDirty(inEditorBranch);
            inEditorBranch.name = "Active Version";
            AssetDatabase.SaveAssets();
        }
    }
    
}

}