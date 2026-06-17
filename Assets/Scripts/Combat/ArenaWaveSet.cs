using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Arena Wave Set", fileName = "ArenaWaveSet")]
public class ArenaWaveSet : ScriptableObject
{
    [SerializeField] private ArenaWaveDefinition[] waves = Array.Empty<ArenaWaveDefinition>();

    public IReadOnlyList<ArenaWaveDefinition> Waves => waves ?? Array.Empty<ArenaWaveDefinition>();
}

[Serializable]
public class ArenaWaveDefinition
{
    [SerializeField] private ArenaSpawnEntry[] spawns = Array.Empty<ArenaSpawnEntry>();
    [SerializeField, Min(0f)] private float delayBeforeSpawn = 0.25f;
    [SerializeField, Min(0f)] private float delayAfterClear = 0.75f;

    public IReadOnlyList<ArenaSpawnEntry> Spawns => spawns ?? Array.Empty<ArenaSpawnEntry>();
    public float DelayBeforeSpawn => delayBeforeSpawn;
    public float DelayAfterClear => delayAfterClear;
}

[Serializable]
public class ArenaSpawnEntry
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField, Min(1)] private int count = 1;
    [SerializeField] private bool useRandomSpawnPoint = true;

    public GameObject EnemyPrefab => enemyPrefab;
    public int Count => Mathf.Max(1, count);
    public bool UseRandomSpawnPoint => useRandomSpawnPoint;
}
