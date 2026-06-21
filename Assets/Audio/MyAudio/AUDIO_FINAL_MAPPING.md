# Audio Final Mapping

## BGM

| AudioKey | Resource | Trigger |
| --- | --- | --- |
| `PlaylistMainMenu` | `Goblins_Den_(Regular).wav` | Main menu |
| `PlaylistLevels` | `Goblins_Dance_(Battle).wav` | Level scenes |

## Combat SFX

| AudioKey | Resource | Trigger |
| --- | --- | --- |
| `PlayerAttackHit` | `26_sword_hit_1/2/3.wav` | Player hit confirmation |
| `PlayerAttackMiss` | `27_sword_miss_1/2/3.wav` | Player missed attack |
| `PlayerBlock` | `02_chest_close_1.wav` | Player starts block/counter |
| `PlayerCounterSuccess` | `26_sword_hit_2.wav` | Player block/parry success |
| `PlayerDash` | `15_human_dash_1/2.wav` | Player dash start |
| `PlayerPotionUse` | `08_human_charge_1/2.wav` | Player uses healing potion |
| `PlayerPotionComplete` | `08_human_charge_2.wav` | Player potion heal complete |
| `PlayerJump` | `12_human_jump_1/2/3.wav` | Player jump start |
| `PlayerHurt` | `11_human_damage_1/2/3.wav` | Player takes damage |
| `PlayerDeath` | `14_human_death_spin.wav` | Player death |
| `EnemyHurt` | `21_orc_damage_1/2/3.wav` | Enemy takes damage |
| `EnemyDeath` | `24_orc_death_spin.wav` | Enemy death |

## UI / Interaction SFX

| AudioKey | Resource | Trigger |
| --- | --- | --- |
| `ButtonClick` | `03_crate_open_1.wav` | General UI button click |
| `BonfireMenuOpen` | `01_chest_open_1/2/3/4.wav` | Bonfire menu open |
| `BonfireMenuClose` | `02_chest_close_1/2/3.wav` | Bonfire menu close |
| `BonfireTravel` | `10_human_special_atk_1/2.wav` | Bonfire travel confirm |
| `BonfireIgnite` | `08_human_charge_1/2.wav` | Bonfire ignite |
| `BonfireRest` | `04_sack_open_1/2/3.wav` | Bonfire rest |
| `ArenaDoorOpen` | `05_door_open_1.mp3` | Arena door open |
| `ArenaDoorClose` | `06_door_close_1.mp3` | Arena door close |
| `MageSpellCast` | `10_human_special_atk_1/2.wav` | Mage fireball summon |
| `BonfireFlameLoop` | `Fire_Burning.mp3` | Bonfire ambient flame |
| `AbyssFireFlameLoop` | `Fire_Burning.mp3` | Abyss fire ambient flame |

## Current Status

- Audio keys are mapped in `Assets/Scripts/Audio/AudioKey.cs`.
- Resources are mounted in `Assets/Resources/Audio/AUDIO DATABASE.asset`.
- UI button click audio is centralized through `UIAudio.PlayButtonClick()`.
- Bonfire and arena door audio are driven by `AudioKey`, not raw strings.

## Notes

- This pack covers the core needs for the current project scope.
- The `ButtonClick` sound is intentionally short and light.
- If you later import dedicated UI/door/bonfire assets, replace only the corresponding entries in `AUDIO DATABASE.asset` and keep the `AudioKey` contract unchanged.
