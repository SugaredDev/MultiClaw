using UnityEngine;
using System.Collections.Generic;

namespace MultiClaw
{

public enum DepotNumber
{

    NotSet = 0, 
    Depot1 = 1, Depot2 = 2, Depot3 = 3, 
    Depot4 = 4, Depot5 = 5, Depot6 = 6, 
    Depot7 = 7, Depot8 = 8, Depot9 = 9

}

[CreateAssetMenu(fileName = "Steam Branches", menuName = "Builds/Steam Branches")]
public class SteamBranches : ScriptableObject
{

    public DepotNumber depotWindows = DepotNumber.NotSet;
    public DepotNumber depotMacOS = DepotNumber.NotSet;
    public DepotNumber depotLinux = DepotNumber.NotSet;
    public DepotNumber depotSteamDeck = DepotNumber.NotSet;
    public List<string> branches = new List<string>{};

}

}