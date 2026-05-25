using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace MultiClaw.Core
{

public class SteamDepoter : EditorWindow
{

    [System.Serializable]
    public class SteamConfig
    {
        public string steamContentBuilderPath = "";
        public string steamBranch = "development";
        public string customDescription = "";
    }

    static SteamConfig config;
    static Rememberer branchPresets;
    static string steamUsername = "";
    static string steamPassword = "";
    Vector2 scrollPos;

    const string CONFIG_KEY = "MultiClaw_SteamConfig";

    // ==============================================================================

    [MenuItem("Tools/MultiClaw/Steam Depoter", false, 1)]
    static void ShowWindow() => GetWindow<SteamDepoter>("Steam Depoter");

    static string GetCurrentAppId()
    {
        GameBranch activeBranch = Resources.Load<GameBranch>(Constants.Resources_Active);
        if (activeBranch != null && activeBranch.steamAPI > 0)
            return activeBranch.steamAPI.ToString();
        return "Not Set";
    }

    static bool ValidateAppId(string appId, out string baseAppId)
    {
        baseAppId = "";
        if (appId == "Not Set" || string.IsNullOrEmpty(appId)) return false;
        
        if (!appId.EndsWith("0"))
        {
            UnityEngine.Debug.LogWarning($"App ID '{appId}' should end with 0. Please update the Steam API in your Active Version.");
            return false;
        }
        
        baseAppId = appId.Substring(0, appId.Length - 1);
        return true;
    }

    static string GetDepotId(DepotNumber depot)
    {
        if (depot == DepotNumber.NotSet)
            return "Not Set";
            
        string appId = GetCurrentAppId();
        if (!ValidateAppId(appId, out string baseAppId))
            return "Invalid";
        
        return baseAppId + ((int)depot).ToString();
    }



    void OnGUI()
    {
        GUI.backgroundColor = Color.gray;
        if (GUILayout.Button(PlayerSettings.bundleVersion, new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold }))
            EditorWindow.GetWindow<BranchNumeral>("Version Numeral");
        GUI.backgroundColor = Color.white;
        GUILayout.Space(5);
        
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        
        EditorGUILayout.LabelField("ContentBuilder Location", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        EditorGUI.BeginChangeCheck();
        config.steamContentBuilderPath = EditorGUILayout.TextField(config.steamContentBuilderPath);
        if (EditorGUI.EndChangeCheck())
            SaveConfig();
        
        if (GUILayout.Button("Browse", GUILayout.Width(70)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Steam ContentBuilder Folder", "", "");
            if (!string.IsNullOrEmpty(path))
            {
                config.steamContentBuilderPath = path;
                SaveConfig();
            }
        }
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Download ContentBuilder"))
            Application.OpenURL("https://partner.steamgames.com/doc/sdk/uploading#1");
        
        if (!string.IsNullOrEmpty(config.steamContentBuilderPath) && !Directory.Exists(config.steamContentBuilderPath))
            EditorGUILayout.HelpBox("Path does not exist.", MessageType.Error);
        
        EditorGUILayout.Space(5);
        
        EditorGUILayout.LabelField("Description", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUILayout.TextField(PlayerSettings.bundleVersion, GUILayout.Width(150));
        GUI.enabled = true;
        
        EditorGUI.BeginChangeCheck();
        config.customDescription = EditorGUILayout.TextField(config.customDescription, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
            SaveConfig();
        
        EditorGUILayout.EndHorizontal();
        
        string fullDescription = string.IsNullOrEmpty(config.customDescription) 
            ? PlayerSettings.bundleVersion 
            : $"{PlayerSettings.bundleVersion} {config.customDescription}";

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Depots", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        string currentAppId = GetCurrentAppId();
        EditorGUILayout.TextField("App ID", currentAppId, GUILayout.Width(300));
        GUI.enabled = true;
        
        if (GUILayout.Button("Open Version Builder", GUILayout.Width(140)))
            EditorWindow.GetWindow<BranchBuilder>("MultiClaw | Version Builder");
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUILayout.TextField("Windows", GetDepotId(branchPresets?.depotWindows ?? DepotNumber.NotSet), GUILayout.Width(300));
        GUI.enabled = true;
        if (branchPresets != null)
            branchPresets.depotWindows = (DepotNumber)EditorGUILayout.EnumPopup(branchPresets.depotWindows, GUILayout.Width(80));
        
        if (branchPresets != null)
        {
            bool isNotSet = branchPresets.depotWindows == DepotNumber.NotSet;
            if (isNotSet && branchPresets.enableWindows)
            {
                branchPresets.enableWindows = false;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            
            GUI.enabled = !isNotSet;
            GUI.backgroundColor = branchPresets.enableWindows ? Color.green : Color.red;
            if (GUILayout.Button("O", GUILayout.Width(25)))
            {
                branchPresets.enableWindows = !branchPresets.enableWindows;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUILayout.TextField("Linux", GetDepotId(branchPresets?.depotLinux ?? DepotNumber.NotSet), GUILayout.Width(300));
        GUI.enabled = true;
        if (branchPresets != null)
            branchPresets.depotLinux = (DepotNumber)EditorGUILayout.EnumPopup(branchPresets.depotLinux, GUILayout.Width(80));
        
        if (branchPresets != null)
        {
            bool isNotSet = branchPresets.depotLinux == DepotNumber.NotSet;
            if (isNotSet && branchPresets.enableLinux)
            {
                branchPresets.enableLinux = false;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            
            GUI.enabled = !isNotSet;
            GUI.backgroundColor = branchPresets.enableLinux ? Color.green : Color.red;
            if (GUILayout.Button("O", GUILayout.Width(25)))
            {
                branchPresets.enableLinux = !branchPresets.enableLinux;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUILayout.TextField("Steam Deck", GetDepotId(branchPresets?.depotSteamDeck ?? DepotNumber.NotSet), GUILayout.Width(300));
        GUI.enabled = true;
        if (branchPresets != null)
            branchPresets.depotSteamDeck = (DepotNumber)EditorGUILayout.EnumPopup(branchPresets.depotSteamDeck, GUILayout.Width(80));
        
        if (branchPresets != null)
        {
            bool isNotSet = branchPresets.depotSteamDeck == DepotNumber.NotSet;
            if (isNotSet && branchPresets.enableSteamDeck)
            {
                branchPresets.enableSteamDeck = false;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            
            GUI.enabled = !isNotSet;
            GUI.backgroundColor = branchPresets.enableSteamDeck ? Color.green : Color.red;
            if (GUILayout.Button("O", GUILayout.Width(25)))
            {
                branchPresets.enableSteamDeck = !branchPresets.enableSteamDeck;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = false;
        EditorGUILayout.TextField("macOS", GetDepotId(branchPresets?.depotMacOS ?? DepotNumber.NotSet), GUILayout.Width(300));
        GUI.enabled = true;
        if (branchPresets != null)
            branchPresets.depotMacOS = (DepotNumber)EditorGUILayout.EnumPopup(branchPresets.depotMacOS, GUILayout.Width(80));
        
        if (branchPresets != null)
        {
            bool isNotSet = branchPresets.depotMacOS == DepotNumber.NotSet;
            if (isNotSet && branchPresets.enableMacOS)
            {
                branchPresets.enableMacOS = false;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            
            GUI.enabled = !isNotSet;
            GUI.backgroundColor = branchPresets.enableMacOS ? Color.green : Color.red;
            if (GUILayout.Button("O", GUILayout.Width(25)))
            {
                branchPresets.enableMacOS = !branchPresets.enableMacOS;
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (EditorGUI.EndChangeCheck() && branchPresets != null)
        {
            EditorUtility.SetDirty(branchPresets);
            AssetDatabase.SaveAssets();
        }
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Branch", EditorStyles.boldLabel);
        
        EditorGUI.BeginChangeCheck();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Requested branch", GUILayout.Width(120));
        config.steamBranch = EditorGUILayout.TextField(config.steamBranch, GUILayout.Width(180));
        
        GUI.backgroundColor = Color.green;
        GUI.enabled = !string.IsNullOrEmpty(config.steamBranch) && 
                      !config.steamBranch.Equals("default", System.StringComparison.OrdinalIgnoreCase) &&
                      (branchPresets == null || !branchPresets.branches.Contains(config.steamBranch));
        if (GUILayout.Button("Save as Preset", GUILayout.Width(120)))
        {
            if (branchPresets != null && !branchPresets.branches.Contains(config.steamBranch))
            {
                branchPresets.branches.Add(config.steamBranch);
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        if (branchPresets != null && branchPresets.branches != null && branchPresets.branches.Count > 0)
        {
            EditorGUILayout.LabelField("Presets:", EditorStyles.miniLabel);
            
            int presetToRemove = -1;
            for (int i = 0; i < branchPresets.branches.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button(branchPresets.branches[i], GUILayout.Width(200)))
                {
                    config.steamBranch = branchPresets.branches[i];
                    SaveConfig();
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    presetToRemove = i;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            if (presetToRemove >= 0)
            {
                branchPresets.branches.RemoveAt(presetToRemove);
                EditorUtility.SetDirty(branchPresets);
                AssetDatabase.SaveAssets();
                Repaint();
            }
        }
        
        if (EditorGUI.EndChangeCheck())
        {
            SaveConfig();
        }

        EditorGUILayout.Space(10);
        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Steam Login", EditorStyles.boldLabel);
        steamUsername = EditorGUILayout.TextField("Username", steamUsername);
        steamPassword = EditorGUILayout.PasswordField("Password", steamPassword);
        
        EditorGUILayout.Space(10);

        bool hasAtLeastOneDepot = (branchPresets?.depotWindows ?? DepotNumber.NotSet) != DepotNumber.NotSet ||
                                   (branchPresets?.depotLinux ?? DepotNumber.NotSet) != DepotNumber.NotSet ||
                                   (branchPresets?.depotSteamDeck ?? DepotNumber.NotSet) != DepotNumber.NotSet ||
                                   (branchPresets?.depotMacOS ?? DepotNumber.NotSet) != DepotNumber.NotSet;

        List<DepotNumber> activeDepots = new List<DepotNumber>();
        if ((branchPresets?.depotWindows ?? DepotNumber.NotSet) != DepotNumber.NotSet)
            activeDepots.Add(branchPresets.depotWindows);
        if ((branchPresets?.depotLinux ?? DepotNumber.NotSet) != DepotNumber.NotSet)
            activeDepots.Add(branchPresets.depotLinux);
        if ((branchPresets?.depotSteamDeck ?? DepotNumber.NotSet) != DepotNumber.NotSet)
            activeDepots.Add(branchPresets.depotSteamDeck);
        if ((branchPresets?.depotMacOS ?? DepotNumber.NotSet) != DepotNumber.NotSet)
            activeDepots.Add(branchPresets.depotMacOS);
        
        bool hasDuplicateDepots = activeDepots.Count != activeDepots.Distinct().Count();

        bool isDefaultBranch = config.steamBranch.Equals("default", System.StringComparison.OrdinalIgnoreCase);

        bool canUpload = !string.IsNullOrEmpty(steamUsername) && 
                         !string.IsNullOrEmpty(steamPassword) &&
                         !string.IsNullOrEmpty(config.steamContentBuilderPath) &&
                         Directory.Exists(config.steamContentBuilderPath) &&
                         Directory.Exists(GetBuildsPath()) &&
                         hasAtLeastOneDepot &&
                         !hasDuplicateDepots &&
                         !isDefaultBranch;

        GUI.enabled = canUpload;
        GUI.backgroundColor = canUpload ? Color.green : Color.gray;
        
        if (GUILayout.Button("Upload To Steam", GUILayout.Height(40)))
            if (EditorUtility.DisplayDialog("Upload", $"Upload to '{config.steamBranch}'?", "Yes", "No"))
                UploadToSteam();
        
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;
        
        if (currentAppId == "Not Set" || currentAppId == "0")
            EditorGUILayout.HelpBox("Steam API not set in Active Version. Configure it in the Version Builder.", MessageType.Warning);
        else if (!currentAppId.EndsWith("0"))
            EditorGUILayout.HelpBox($"App ID must end with 0. Update Steam API in Version Builder.", MessageType.Error);
        
        if (string.IsNullOrEmpty(config.steamContentBuilderPath))
            EditorGUILayout.HelpBox("ContentBuilder path not set.", MessageType.Warning);
        else if (!Directory.Exists(config.steamContentBuilderPath))
            EditorGUILayout.HelpBox("ContentBuilder path does not exist.", MessageType.Error);
        
        if (!Directory.Exists(GetBuildsPath()))
            EditorGUILayout.HelpBox("No builds found.", MessageType.Warning);
        
        if (!hasAtLeastOneDepot)
            EditorGUILayout.HelpBox("At least one depot must be set.", MessageType.Warning);
        
        if (hasDuplicateDepots)
            EditorGUILayout.HelpBox("Depot ID's must be unique. Each platform must have a different depot number.", MessageType.Error);
        
        if (isDefaultBranch)
            EditorGUILayout.HelpBox("Cannot name a branch like that.", MessageType.Error);
        else if (string.IsNullOrEmpty(config.steamBranch))
            EditorGUILayout.HelpBox("Depot won't auto go publicly live on the default branch.", MessageType.Info);
        
        if ((branchPresets?.depotMacOS ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableMacOS ?? false))
            EditorGUILayout.HelpBox("This tool DOES NOT provide Apple Distribution Certificate. You must manually certify your macOS build before depoting.", MessageType.Info);

        EditorGUILayout.EndScrollView();
    }

    // ==============================================================================

    void OnEnable()
    {
        LoadConfig();
        branchPresets = Constants.EnsureSteamBranchPresetsExists();
    }

    void LoadConfig()
    {
        string json = EditorPrefs.GetString(CONFIG_KEY, "");
        if (!string.IsNullOrEmpty(json))
            config = JsonUtility.FromJson<SteamConfig>(json);
        else
            config = new SteamConfig();
    }

    static void SaveConfig()
    {
        string json = JsonUtility.ToJson(config);
        EditorPrefs.SetString(CONFIG_KEY, json);
    }

    public static void UploadAfterBuild()
    {
        string json = EditorPrefs.GetString(CONFIG_KEY, "");
        if (string.IsNullOrEmpty(json)) return;
        
        config = JsonUtility.FromJson<SteamConfig>(json);
        if (string.IsNullOrEmpty(steamUsername) || string.IsNullOrEmpty(steamPassword))
        {
            UnityEngine.Debug.LogError("Cannot auto-upload to Steam: Username or password not entered in Steam Depoter.");
            return;
        }
        
        UploadToSteam();
    }

    static void UploadToSteam()
    {
        SaveConfig();
        
        string buildsPath = GetBuildsPath();
        if (!Directory.Exists(buildsPath))
        {
            EditorUtility.DisplayDialog("Error", "No builds found", "OK");
            return;
        }

        var branchDirs = Directory.GetDirectories(buildsPath);
        if (branchDirs.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No build versions found", "OK");
            return;
        }

        foreach (var branchDir in branchDirs)
        {
            string branchName = Path.GetFileName(branchDir);
            GenerateVDFFiles(branchDir, branchName);
            bool success = ExecuteSteamCmd(branchName);
            
            if (success)
                UnityEngine.Debug.LogWarning($"Uploaded '{branchName}'");
            else
                UnityEngine.Debug.LogError($"Failed '{branchName}'");
        }
    }

    static void GenerateVDFFiles(string branchPath, string branchName)
    {
        string scriptsPath = GetSteamScriptsPath();
        if (!Directory.Exists(scriptsPath))
            Directory.CreateDirectory(scriptsPath);

        string outputPath = GetSteamOutputPath();
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        string appId = GetCurrentAppId();
        string appVdfPath = Path.Combine(scriptsPath, $"app_{appId}_{branchName}.vdf");
        GenerateAppBuildVDF(appVdfPath, branchPath, branchName, outputPath);

        string windowsDepot = GetDepotId(branchPresets?.depotWindows ?? DepotNumber.NotSet);
        string linuxDepot = GetDepotId(branchPresets?.depotLinux ?? DepotNumber.NotSet);
        string steamDeckDepot = GetDepotId(branchPresets?.depotSteamDeck ?? DepotNumber.NotSet);
        string macOSDepot = GetDepotId(branchPresets?.depotMacOS ?? DepotNumber.NotSet);

        string windowsPath = Path.Combine(branchPath, "Windows");
        if (Directory.Exists(windowsPath) && (branchPresets?.depotWindows ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableWindows ?? true))
        {
            string depotVdfPath = Path.Combine(scriptsPath, $"depot_{windowsDepot}_{branchName}.vdf");
            GenerateDepotVDF(depotVdfPath, windowsDepot, windowsPath);
        }

        string linuxPath = Path.Combine(branchPath, "Linux");
        if (Directory.Exists(linuxPath) && (branchPresets?.depotLinux ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableLinux ?? true))
        {
            string depotVdfPath = Path.Combine(scriptsPath, $"depot_{linuxDepot}_{branchName}.vdf");
            GenerateDepotVDF(depotVdfPath, linuxDepot, linuxPath);
        }

        string steamDeckPath = Path.Combine(branchPath, "Steam Deck");
        if (Directory.Exists(steamDeckPath) && (branchPresets?.depotSteamDeck ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableSteamDeck ?? true))
        {
            string depotVdfPath = Path.Combine(scriptsPath, $"depot_{steamDeckDepot}_{branchName}.vdf");
            GenerateDepotVDF(depotVdfPath, steamDeckDepot, steamDeckPath);
        }

        string macPath = Path.Combine(branchPath, "macOS");
        if (Directory.Exists(macPath) && (branchPresets?.depotMacOS ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableMacOS ?? true))
        {
            string depotVdfPath = Path.Combine(scriptsPath, $"depot_{macOSDepot}_{branchName}.vdf");
            GenerateDepotVDF(depotVdfPath, macOSDepot, macPath);
        }
    }

    static void GenerateAppBuildVDF(string filePath, string branchPath, string branchName, string outputPath)
    {
        List<string> depots = new List<string>();
        string appId = GetCurrentAppId();
        
        string windowsDepot = GetDepotId(branchPresets?.depotWindows ?? DepotNumber.NotSet);
        string linuxDepot = GetDepotId(branchPresets?.depotLinux ?? DepotNumber.NotSet);
        string steamDeckDepot = GetDepotId(branchPresets?.depotSteamDeck ?? DepotNumber.NotSet);
        string macOSDepot = GetDepotId(branchPresets?.depotMacOS ?? DepotNumber.NotSet);

        if (Directory.Exists(Path.Combine(branchPath, "Windows")) && (branchPresets?.depotWindows ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableWindows ?? true))
            depots.Add($"\t\t\"{windowsDepot}\"\t\"{GetSteamScriptsPath()}/depot_{windowsDepot}_{branchName}.vdf\"");

        if (Directory.Exists(Path.Combine(branchPath, "Linux")) && (branchPresets?.depotLinux ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableLinux ?? true))
            depots.Add($"\t\t\"{linuxDepot}\"\t\"{GetSteamScriptsPath()}/depot_{linuxDepot}_{branchName}.vdf\"");

        if (Directory.Exists(Path.Combine(branchPath, "Steam Deck")) && (branchPresets?.depotSteamDeck ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableSteamDeck ?? true))
            depots.Add($"\t\t\"{steamDeckDepot}\"\t\"{GetSteamScriptsPath()}/depot_{steamDeckDepot}_{branchName}.vdf\"");

        if (Directory.Exists(Path.Combine(branchPath, "macOS")) && (branchPresets?.depotMacOS ?? DepotNumber.NotSet) != DepotNumber.NotSet && (branchPresets?.enableMacOS ?? true))
            depots.Add($"\t\t\"{macOSDepot}\"\t\"{GetSteamScriptsPath()}/depot_{macOSDepot}_{branchName}.vdf\"");

        string depotsSection = string.Join("\n", depots);

        string description = string.IsNullOrEmpty(config.customDescription)
            ? PlayerSettings.bundleVersion
            : $"{PlayerSettings.bundleVersion} {config.customDescription}";

        string content = $@"""appbuild""
{{
	""appid"" ""{appId}""
	""desc"" ""{description}""
	""buildoutput"" ""{outputPath.Replace("\\", "/")}""
	""contentroot"" """"
	""setlive"" ""{config.steamBranch}""
	""preview"" ""0""
	""local""	""""
	""depots""
	{{
{depotsSection}
	}}
}}";

        File.WriteAllText(filePath, content);
    }

    static void GenerateDepotVDF(string filePath, string depotId, string contentPath)
    {
        string content = $@"""DepotBuildConfig""
{{
	""DepotID"" ""{depotId}""
	""contentroot"" ""{contentPath.Replace("\\", "/")}""
	""FileMapping""
	{{
		""LocalPath"" ""*""
		""DepotPath"" "".""
		""recursive"" ""1""
	}}
	""FileExclusion"" ""*.pdb""
}}";

        File.WriteAllText(filePath, content);
    }

    static bool ExecuteSteamCmd(string branchName)
    {
        string steamCmdPath = GetSteamCmdPath();
        if (!File.Exists(steamCmdPath))
        {
            UnityEngine.Debug.LogError("SteamCmd not found");
            return false;
        }

        #if !UNITY_EDITOR_WIN
        try
        {
            Process chmodProcess = new Process();
            chmodProcess.StartInfo.FileName = "chmod";
            chmodProcess.StartInfo.Arguments = $"-R +x \"{Path.GetDirectoryName(steamCmdPath)}\"";
            chmodProcess.StartInfo.UseShellExecute = false;
            chmodProcess.StartInfo.CreateNoWindow = true;
            chmodProcess.Start();
            chmodProcess.WaitForExit();
        }
        catch { }
        #endif

        string appId = GetCurrentAppId();
        string appVdfPath = Path.Combine(GetSteamScriptsPath(), $"app_{appId}_{branchName}.vdf");
        if (!File.Exists(appVdfPath))
        {
            UnityEngine.Debug.LogError("VDF not found");
            return false;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo();
        string args = $"+login {steamUsername} {steamPassword} +run_app_build \"{appVdfPath}\" +quit";

        #if UNITY_EDITOR_WIN
        startInfo.FileName = steamCmdPath;
        startInfo.Arguments = args;
        startInfo.WorkingDirectory = Path.GetDirectoryName(steamCmdPath);
        startInfo.UseShellExecute = true;
        #else
        startInfo.WorkingDirectory = Path.GetDirectoryName(steamCmdPath);
        startInfo.UseShellExecute = true;
        
        string terminal = File.Exists("/usr/bin/gnome-terminal") ? "gnome-terminal" :
                         File.Exists("/usr/bin/konsole") ? "konsole" :
                         File.Exists("/usr/bin/xterm") ? "xterm" : "x-terminal-emulator";
        
        startInfo.FileName = terminal;
        startInfo.Arguments = terminal == "gnome-terminal" 
            ? $"-- bash -c '\"{steamCmdPath}\" {args}; read'"
            : $"-e bash -c '\"{steamCmdPath}\" {args}; read'";
        #endif

        try
        {
            Process process = Process.Start(startInfo);
            #if UNITY_EDITOR_WIN
            if (process != null)
            {
                process.WaitForExit();
                bool success = process.ExitCode == 0;
                process.Dispose();
                return success;
            }
            return false;
            #else
            return true;
            #endif
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Failed: {e.Message}");
            return false;
        }
    }

    static string GetBuildsPath()
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
    }

    static string GetSteamContentBuilderPath() => config.steamContentBuilderPath ?? "";

    static string GetSteamScriptsPath()
    {
        return Path.Combine(GetSteamContentBuilderPath(), "scripts");
    }

    static string GetSteamOutputPath()
    {
        return Path.Combine(GetSteamContentBuilderPath(), "output");
    }

    static string GetSteamCmdPath()
    {
        string builderPath = Path.Combine(GetSteamContentBuilderPath(), "builder");
        
        #if UNITY_EDITOR_WIN
        return Path.Combine(builderPath, "steamcmd.exe");
        #elif UNITY_EDITOR_OSX
        return Path.Combine(GetSteamContentBuilderPath(), "builder_osx", "steamcmd.sh");
        #else
        return Path.Combine(GetSteamContentBuilderPath(), "builder_linux", "steamcmd.sh");
        #endif
    }

}

}