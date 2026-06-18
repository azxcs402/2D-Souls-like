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
    public List<BonfireRecord> litBonfireRecords;

    public GameData()
    {
        lastScenePlayed = string.Empty;
        lastPlayerPosition = Vector3.zero;
        hasLastPlayerPosition = false;
        litBonfireIds = new List<string>();
        litBonfireRecords = new List<BonfireRecord>();
    }

    [Serializable]
    public class BonfireRecord
    {
        public string sceneName;
        public string bonfireId;
        public string displayName;
        public Vector3 worldPosition;

        public string GetKey()
        {
            return $"{sceneName}|{bonfireId}";
        }
    }
}
