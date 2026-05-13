using UnityEngine;

namespace MultiClaw.Core
{

[CreateAssetMenu(fileName = "Build Version #", menuName = "Builds/Build Version")]
public class GameVersion : ScriptableObject
{

    public string title = "Project";
    public string fileName = "Application";
    public bool debug = true;
    public uint steamAPI = 0;
    public VersionType versionType = VersionType.Development;
    [HideInInspector] public bool steamDeck = false;
    [HideInInspector] public bool buildEnabled = true;

    public bool IsVersionType(VersionType type) => versionType == type;

}

}