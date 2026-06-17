using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameData
{
    public string lastScenePlayed;
    public Vector3 lastPlayerPosition;
    public bool hasLastPlayerPosition;
    public List<string> litBonfireIds;

    public GameData()
    {
        lastScenePlayed = string.Empty;
        lastPlayerPosition = Vector3.zero;
        hasLastPlayerPosition = false;
        litBonfireIds = new List<string>();
    }
}
