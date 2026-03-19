using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MultiClaw
{

public class VersionBuilder : EditorWindow
{

    [System.Serializable]
    public class BuildConfig { public GameVersion configAsset; public bool buildEnabled; }

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

    List<BuildConfig> buildVersions = new();
    Vector2 scroll;
    GameVersion inEditorVersion;
    bool buildWindows, buildMac, buildLinux, buildSteamDeck;

    readonly PlatformInfo[] platforms = {
        new(false, BuildTarget.StandaloneWindows64, "Windows", ".exe"),
        new(false, BuildTarget.StandaloneOSX, "macOS", ".app"),
        new(false, BuildTarget.StandaloneLinux64, "Linux", ".x86_64"),
        new(true, BuildTarget.StandaloneLinux64, "Steam Deck", ".x86_64")
    };

    [MenuItem("Tools/MultiClaw/Version Builder")]
    static void ShowWindow() => GetWindow<VersionBuilder>("MultiClaw | Version Builder");

    void OnEnable()
    {
        inEditorVersion = Constants.EnsureActiveVersionExists();
        RefreshVersionsList();
        EnsureActiveVersionMatchesAvailable();
    }

    void OnGUI()
    {
        GUI.backgroundColor = Color.gray;
        if (GUILayout.Button(PlayerSettings.bundleVersion, new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold }))
            EditorWindow.GetWindow<VersionNumeral>("Version Numeral");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Build Versions", EditorStyles.boldLabel);
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("+ New Version", GUILayout.Width(100)))
            CreateNewVersion();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < buildVersions.Count; i++) VersionEntry(i);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Box("", new GUIStyle(GUI.skin.box) { margin = new RectOffset(0, 0, 4, 4) }, GUILayout.ExpandWidth(true), GUILayout.Height(1));

        EditorGUILayout.LabelField("Platforms", EditorStyles.boldLabel);
        buildWindows = ColoredToggle("Windows", buildWindows);
        buildMac = ColoredToggle("macOS", buildMac);
        buildLinux = ColoredToggle("Linux", buildLinux);
        buildSteamDeck = ColoredToggle("Steam Deck", buildSteamDeck);

        GUILayout.Space(10);
        bool[] platformStates = { buildWindows, buildMac, buildLinux, buildSteamDeck };
        bool canBuild = platformStates.Any(p => p) && buildVersions.Any(v => v.buildEnabled);
        
        List<string> missingSupport = new List<string>();
        
        if (buildWindows && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            missingSupport.Add("Windows");
        
        if (buildMac && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            missingSupport.Add("macOS");
        
        if ((buildLinux || buildSteamDeck) && !BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            missingSupport.Add(buildLinux && buildSteamDeck ? "Linux/Steam Deck" : buildLinux ? "Linux" : "Steam Deck");
        
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

    void VersionEntry(int build)
    {
        var version = buildVersions[build];
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.BeginHorizontal();

        bool isActiveAsset = AssetDatabase.GetAssetPath(version.configAsset) == Constants.Path_Active;
        bool isActive = isActiveAsset || (inEditorVersion != null && version.configAsset != null && 
                                          version.configAsset.title == inEditorVersion.title &&
                                          version.configAsset.fileName == inEditorVersion.fileName &&
                                          version.configAsset.debug == inEditorVersion.debug &&
                                          version.configAsset.steamAPI == inEditorVersion.steamAPI);
        
        GUI.backgroundColor = isActive ? Color.yellow : Color.white;
        GUI.enabled = !isActiveAsset && version.configAsset != null;

        if (GUILayout.Button(isActive ? "Active In-Editor" : "Set Active", GUILayout.Width(110)))
        {
            EditorUtility.CopySerialized(version.configAsset, inEditorVersion);
            inEditorVersion.name = "Active Version";
            EditorUtility.SetDirty(inEditorVersion);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        GUI.enabled = true;
        GUI.backgroundColor = version.buildEnabled ? Color.green : Color.red;

        if (GUILayout.Button(version.buildEnabled ? "Enabled for build" : "Disabled for build", GUILayout.Width(110)))
            version.buildEnabled = !version.buildEnabled;

        GUI.backgroundColor = Color.white;
        GUILayout.FlexibleSpace();
        
        GUI.enabled = !isActiveAsset;
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            if (EditorUtility.DisplayDialog("Delete Version", $"Are you sure you want to delete '{version.configAsset.title}'?", "Delete", "Cancel"))
            {
                var versionToDelete = version.configAsset;
                EditorApplication.delayCall += () => DeleteVersion(versionToDelete);
            }
        }

        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        if (version.configAsset != null)
        {
            GUI.enabled = !isActiveAsset;
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Title:", GUILayout.Width(140));
            version.configAsset.title = EditorGUILayout.TextField(version.configAsset.title);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Executable File Name:", GUILayout.Width(140));
            version.configAsset.fileName = EditorGUILayout.TextField(version.configAsset.fileName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Debug:", GUILayout.Width(140));
            version.configAsset.debug = EditorGUILayout.Toggle(version.configAsset.debug);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Steam API:", GUILayout.Width(140));
            version.configAsset.steamAPI = (uint)EditorGUILayout.IntField((int)version.configAsset.steamAPI);
            EditorGUILayout.EndHorizontal();
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(version.configAsset);
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
        bool[] platformStates = { buildWindows, buildMac, buildLinux, buildSteamDeck };

        var backupVersion = CreateInstance<GameVersion>();
        EditorUtility.CopySerialized(inEditorVersion, backupVersion);

        try
        {
            foreach (var version in buildVersions.Where(v => v.buildEnabled && v.configAsset != null))
            {
                for (int i = 0; i < platforms.Length; i++)
                {
                    if (!platformStates[i]) continue;

                    EditorUtility.CopySerialized(version.configAsset, inEditorVersion);
                    inEditorVersion.steamDeck = platforms[i].isSteamDeck;
                    inEditorVersion.name = "Active Version";
                    EditorUtility.SetDirty(inEditorVersion);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    BuildPlatform(version, platforms[i], buildsRoot, scenes);
                }
            }
        }
        finally
        {
            EditorUtility.CopySerialized(backupVersion, inEditorVersion);
            inEditorVersion.name = "Active Version";
            EditorUtility.SetDirty(inEditorVersion);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            DestroyImmediate(backupVersion);
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

    void BuildPlatform(BuildConfig version, PlatformInfo platform, string root, string[] scenes)
    {
        string folder = Path.Combine(root, version.configAsset.title, platform.folder);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, version.configAsset.fileName + platform.extension);

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = platform.target,
            options = BuildOptions.CleanBuildCache
        });

        Debug.Log($"Build {(report.summary.result == BuildResult.Succeeded ? "Succeeded" : "Failed")}: {path}");
        
        string burstDebugFolder = Path.Combine(folder, version.configAsset.fileName + "_BurstDebugInformation_DoNotShip");
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

    void RefreshVersionsList()
    {
        buildVersions.Clear();
        buildWindows = buildMac = buildLinux = buildSteamDeck = false;
        
        if (!Directory.Exists(Constants.Path_Versions)) Directory.CreateDirectory(Constants.Path_Versions);

        var allVersions = AssetDatabase.FindAssets("t:GameVersion", new[] { Constants.Path_Versions })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(path => AssetDatabase.LoadAssetAtPath<GameVersion>(path))
            .Where(asset => asset != null)
            .Select(asset => new BuildConfig { configAsset = asset })
            .ToList();
        
        var otherVersions = allVersions.Where(v => AssetDatabase.GetAssetPath(v.configAsset) != Constants.Path_Active).ToList();
        
        if (otherVersions.Count > 0)
            buildVersions = otherVersions;
        else
            buildVersions = allVersions;
        
        EnsureActiveVersionMatchesAvailable();
    }

    void CreateNewVersion()
    {
        if (!Directory.Exists(Constants.Path_Versions)) Directory.CreateDirectory(Constants.Path_Versions);
        
        int versionNumber = 1;
        string assetPath;
        
        do
        {
            assetPath = Path.Combine(Constants.Path_Versions, $"Build Version {versionNumber}.asset");
            versionNumber++;
        } while (AssetDatabase.LoadAssetAtPath<GameVersion>(assetPath) != null);
        
        var newVersion = CreateInstance<GameVersion>();
        
        AssetDatabase.CreateAsset(newVersion, assetPath);
        AssetDatabase.SaveAssets();
        
        EditorApplication.delayCall += () =>
        {
            RefreshVersionsList();
            EditorGUIUtility.PingObject(newVersion);
        };
    }

    void DeleteVersion(GameVersion versionToDelete)
    {
        if (versionToDelete == null) return;
        
        string assetPath = AssetDatabase.GetAssetPath(versionToDelete);
        AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.SaveAssets();
        
        RefreshVersionsList();
    }

    void EnsureActiveVersionMatchesAvailable()
    {
        if (inEditorVersion == null)
            inEditorVersion = Constants.EnsureActiveVersionExists();
    }
    
}

}