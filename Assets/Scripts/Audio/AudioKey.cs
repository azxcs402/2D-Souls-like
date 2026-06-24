using System;
using System.Collections.Generic;

public enum AudioKey
{
    PlayerAttackHit = 0,
    PlayerAttackMiss = 1,
    PlayerBlock = 2,
    PlayerCounterSuccess = 3,
    PlayerDash = 4,
    PlayerPotionUse = 5,
    PlayerPotionComplete = 6,
    PlayerJump = 7,
    PlayerHurt = 8,
    PlayerDeath = 9,
    EnemyHurt = 10,
    EnemyDeath = 11,
    AbyssMageFireballExplosion = 12,
    ButtonClick = 13,
    ArenaDoorOpen = 14,
    ArenaDoorClose = 15,
    MageSpellCast = 16,
    PlaylistMainMenu = 17,
    PlaylistLevels = 18,
    BonfireIgnite = 19,
    BonfireRest = 20,
    BonfireMenuOpen = 21,
    BonfireMenuClose = 22,
    BonfireTravel = 23,
    BonfireFlameLoop = 24,
    AbyssFireAppear = 25,
    AbyssFireDisappear = 26,
    AbyssFireFlameLoop = 27,
    SpellWindLoop = 28,
    PlaylistDoorBattle = 29,
    PlaylistBossBattle = 30
}

public static class AudioKeyMap
{
    private static readonly Dictionary<AudioKey, string> KeyToName = new()
    {
        [AudioKey.PlayerAttackHit] = "player_attackHit",
        [AudioKey.PlayerAttackMiss] = "player_attackMiss",
        [AudioKey.PlayerBlock] = "player_block",
        [AudioKey.PlayerCounterSuccess] = "player_counterSuccess",
        [AudioKey.PlayerDash] = "player_dash",
        [AudioKey.PlayerPotionUse] = "player_potion_use",
        [AudioKey.PlayerPotionComplete] = "player_potion_complete",
        [AudioKey.PlayerJump] = "player_jump",
        [AudioKey.PlayerHurt] = "player_hurt",
        [AudioKey.PlayerDeath] = "player_death",
        [AudioKey.EnemyHurt] = "enemy_hurt",
        [AudioKey.EnemyDeath] = "enemy_death",
        [AudioKey.AbyssMageFireballExplosion] = "abyss_mage_fireball_explosion",
        [AudioKey.ButtonClick] = "button_click",
        [AudioKey.ArenaDoorOpen] = "door_open",
        [AudioKey.ArenaDoorClose] = "door_close",
        [AudioKey.MageSpellCast] = "mage_spell_cast",
        [AudioKey.PlaylistMainMenu] = "playlist_mainMenu",
        [AudioKey.PlaylistLevels] = "playlist_levels",
        [AudioKey.PlaylistDoorBattle] = "playlist_doorBattle",
        [AudioKey.PlaylistBossBattle] = "playlist_bossBattle",
        [AudioKey.BonfireIgnite] = "bonfire_ignite",
        [AudioKey.BonfireRest] = "bonfire_rest",
        [AudioKey.BonfireMenuOpen] = "bonfire_menu_open",
        [AudioKey.BonfireMenuClose] = "bonfire_menu_close",
        [AudioKey.BonfireTravel] = "bonfire_travel",
        [AudioKey.BonfireFlameLoop] = "bonfire_flame_loop",
        [AudioKey.AbyssFireAppear] = "abyss_fire_appear",
        [AudioKey.AbyssFireDisappear] = "abyss_fire_disappear",
        [AudioKey.AbyssFireFlameLoop] = "abyss_fire_flame_loop",
        [AudioKey.SpellWindLoop] = "spell_wind_loop"
    };

    private static readonly Dictionary<string, AudioKey> NameToKey = new(StringComparer.Ordinal)
    {
        ["player_attackHit"] = AudioKey.PlayerAttackHit,
        ["player_attackMiss"] = AudioKey.PlayerAttackMiss,
        ["player_block"] = AudioKey.PlayerBlock,
        ["player_counterSuccess"] = AudioKey.PlayerCounterSuccess,
        ["player_dash"] = AudioKey.PlayerDash,
        ["player_potion_use"] = AudioKey.PlayerPotionUse,
        ["player_potion_complete"] = AudioKey.PlayerPotionComplete,
        ["player_jump"] = AudioKey.PlayerJump,
        ["player_hurt"] = AudioKey.PlayerHurt,
        ["player_death"] = AudioKey.PlayerDeath,
        ["enemy_hurt"] = AudioKey.EnemyHurt,
        ["enemy_death"] = AudioKey.EnemyDeath,
        ["abyss_mage_fireball_explosion"] = AudioKey.AbyssMageFireballExplosion,
        ["button_click"] = AudioKey.ButtonClick,
        ["door_open"] = AudioKey.ArenaDoorOpen,
        ["door_close"] = AudioKey.ArenaDoorClose,
        ["mage_spell_cast"] = AudioKey.MageSpellCast,
        ["playlist_mainMenu"] = AudioKey.PlaylistMainMenu,
        ["playlist_levels"] = AudioKey.PlaylistLevels,
        ["playlist_doorBattle"] = AudioKey.PlaylistDoorBattle,
        ["playlist_bossBattle"] = AudioKey.PlaylistBossBattle,
        ["bonfire_ignite"] = AudioKey.BonfireIgnite,
        ["bonfire_rest"] = AudioKey.BonfireRest,
        ["bonfire_menu_open"] = AudioKey.BonfireMenuOpen,
        ["bonfire_menu_close"] = AudioKey.BonfireMenuClose,
        ["bonfire_travel"] = AudioKey.BonfireTravel,
        ["bonfire_flame_loop"] = AudioKey.BonfireFlameLoop,
        ["abyss_fire_appear"] = AudioKey.AbyssFireAppear,
        ["abyss_fire_disappear"] = AudioKey.AbyssFireDisappear,
        ["abyss_fire_flame_loop"] = AudioKey.AbyssFireFlameLoop,
        ["spell_wind_loop"] = AudioKey.SpellWindLoop
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
