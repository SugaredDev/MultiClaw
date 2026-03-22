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

public class Rememberer : ScriptableObject
{

    [HideInInspector] public DepotNumber depotWindows = DepotNumber.NotSet;
    [HideInInspector] public DepotNumber depotMacOS = DepotNumber.NotSet;
    [HideInInspector] public DepotNumber depotLinux = DepotNumber.NotSet;
    [HideInInspector] public DepotNumber depotSteamDeck = DepotNumber.NotSet;
    [HideInInspector] public bool enableWindows = true;
    [HideInInspector] public bool enableMacOS = true;
    [HideInInspector] public bool enableLinux = true;
    [HideInInspector] public bool enableSteamDeck = true;
    [HideInInspector] public List<string> branches = new List<string>{};

}

}