using UnityEngine;

namespace MultiClaw.Core
{

public class BranchIndicator : MonoBehaviour
{

    public static GameBranch branch { get; private set; }
    static bool showGUI = true;
    
    public static event System.Action<GameBranch> OnBranchLoaded;
    public static bool branchLoaded = false; // To check on runtime if the branch is loaded, for custom stuff if you want.

    public static void ShowBranch() => showGUI = !showGUI;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        var cloud = GameObject.Find("MultiClaw");
        if (cloud == null)
        {
            cloud = new GameObject("MultiClaw");
            DontDestroyOnLoad(cloud);
        }
        
        var branchObject = new GameObject("VersionSystem");
        branchObject.transform.SetParent(cloud.transform);
        branchObject.AddComponent<BranchIndicator>();

        branch = Resources.Load<GameBranch>(Constants.Resources_Active);

        if (branch == null)
            Debug.LogError("Game Version => Active Version.asset not found in Resources folder. (Assets/Plugins/MultiClaw/Resources)");
        
        branchLoaded = true;
        OnBranchLoaded?.Invoke(branch);
    }

    GUIStyle branchStyle;
    Font customFont;
    void Awake()
    {
        customFont = Resources.Load<Font>(Constants.Resources_Indicator);
        if (customFont == null)
            Debug.LogWarning("Version Indication => GUI Font not found in Assets/Plugins/MultiClaw/Resources/GUI. Using default GUI font.");

        branchStyle = new GUIStyle
        {
            normal = { textColor = new Color(1f, 1f, 1f, 0.1f) },
            alignment = TextAnchor.MiddleCenter,
        };
        
        if (customFont != null)
            branchStyle.font = customFont;
    }

    void OnGUI()
    {
        if (!showGUI || !Constants.IsDebugBranch(branch))
            return;

        branchStyle.fontSize = Mathf.Max(5, Screen.height / 50);

        string branchText = $"{branch.title}.{Application.version}";

        Vector2 textSize = branchStyle.CalcSize(new GUIContent(branchText));
        float gap = Screen.height * 0.01f;

        float x = (Screen.width - textSize.x) / 2;
        float y = Screen.height - textSize.y - gap;

        GUI.Label(new Rect(x, y, textSize.x, textSize.y), branchText, branchStyle);
    }

}

}