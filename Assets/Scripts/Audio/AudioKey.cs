using System;
using System.Collections.Generic;

public enum AudioKey
{
    PlayerAttackHit,
    PlayerAttackMiss,
    PlayerHurt,
    PlayerDeath,
    EnemyHurt,
    EnemyDeath,
    ButtonClick,
    PlaylistMainMenu,
    PlaylistLevels,
    BonfireIgnite,
    BonfireRest,
    BonfireMenuOpen,
    BonfireMenuClose,
    BonfireTravel
}

public static class AudioKeyMap
{
    private static readonly Dictionary<AudioKey, string> KeyToName = new()
    {
        [AudioKey.PlayerAttackHit] = "player_attackHit",
        [AudioKey.PlayerAttackMiss] = "player_attackMiss",
        [AudioKey.PlayerHurt] = "player_hurt",
        [AudioKey.PlayerDeath] = "player_death",
        [AudioKey.EnemyHurt] = "enemy_hurt",
        [AudioKey.EnemyDeath] = "enemy_death",
        [AudioKey.ButtonClick] = "button_click",
        [AudioKey.PlaylistMainMenu] = "playlist_mainMenu",
        [AudioKey.PlaylistLevels] = "playlist_levels",
        [AudioKey.BonfireIgnite] = "bonfire_ignite",
        [AudioKey.BonfireRest] = "bonfire_rest",
        [AudioKey.BonfireMenuOpen] = "bonfire_menu_open",
        [AudioKey.BonfireMenuClose] = "bonfire_menu_close",
        [AudioKey.BonfireTravel] = "bonfire_travel"
    };

    private static readonly Dictionary<string, AudioKey> NameToKey = new(StringComparer.Ordinal)
    {
        ["player_attackHit"] = AudioKey.PlayerAttackHit,
        ["player_attackMiss"] = AudioKey.PlayerAttackMiss,
        ["player_hurt"] = AudioKey.PlayerHurt,
        ["player_death"] = AudioKey.PlayerDeath,
        ["enemy_hurt"] = AudioKey.EnemyHurt,
        ["enemy_death"] = AudioKey.EnemyDeath,
        ["button_click"] = AudioKey.ButtonClick,
        ["playlist_mainMenu"] = AudioKey.PlaylistMainMenu,
        ["playlist_levels"] = AudioKey.PlaylistLevels,
        ["bonfire_ignite"] = AudioKey.BonfireIgnite,
        ["bonfire_rest"] = AudioKey.BonfireRest,
        ["bonfire_menu_open"] = AudioKey.BonfireMenuOpen,
        ["bonfire_menu_close"] = AudioKey.BonfireMenuClose,
        ["bonfire_travel"] = AudioKey.BonfireTravel
    };

    public static bool TryGetAudioName(AudioKey key, out string audioName)
    {
        return KeyToName.TryGetValue(key, out audioName);
    }

    public static string GetAudioName(AudioKey key)
    {
        return KeyToName.TryGetValue(key, out string audioName) ? audioName : key.ToString();
    }

    public static bool TryParse(string audioName, out AudioKey key)
    {
        if (string.IsNullOrWhiteSpace(audioName))
        {
            key = default;
            return false;
        }

        return NameToKey.TryGetValue(audioName, out key);
    }
}
