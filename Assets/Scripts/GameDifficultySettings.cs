using UnityEngine;

public enum GameDifficulty
{
    Normal = 0,
    Easy = 1
}

public static class GameDifficultySettings
{
    private const string DifficultyKey = "game_difficulty";

    private static bool isLoaded;
    private static GameDifficulty currentDifficulty = GameDifficulty.Normal;

    public static GameDifficulty CurrentDifficulty
    {
        get
        {
            EnsureLoaded();
            return currentDifficulty;
        }
    }

    public static float IncomingDamageMultiplier
    {
        get
        {
            return CurrentDifficulty == GameDifficulty.Easy ? 0.5f : 1f;
        }
    }

    public static bool IsEasy
    {
        get
        {
            return CurrentDifficulty == GameDifficulty.Easy;
        }
    }

    public static void SetDifficulty(GameDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        isLoaded = true;
        PlayerPrefs.SetInt(DifficultyKey, (int)difficulty);
        PlayerPrefs.Save();
    }

    public static void ResetToSavedDifficulty()
    {
        isLoaded = false;
        EnsureLoaded();
    }

    private static void EnsureLoaded()
    {
        if (isLoaded)
        {
            return;
        }

        currentDifficulty = (GameDifficulty)PlayerPrefs.GetInt(DifficultyKey, (int)GameDifficulty.Normal);
        if (currentDifficulty != GameDifficulty.Easy)
        {
            currentDifficulty = GameDifficulty.Normal;
        }

        isLoaded = true;
    }
}
