using System;
using UnityEngine;

[Serializable]
public class GameData
{
    public string lastScenePlayed;
    public Vector3 lastPlayerPosition;
    public bool hasLastPlayerPosition;

    public GameData()
    {
        lastScenePlayed = string.Empty;
        lastPlayerPosition = Vector3.zero;
        hasLastPlayerPosition = false;
    }
}
