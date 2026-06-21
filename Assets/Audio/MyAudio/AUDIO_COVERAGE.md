# Audio Coverage

## 已覆盖的音频需求

| 需求 | AudioKey | 当前资源 | 触发位置 |
| --- | --- | --- | --- |
| 玩家攻击命中 | `PlayerAttackHit` | `26_sword_hit_1/2/3.wav` | `Entity_SFX` / `AudioManager` |
| 玩家攻击落空 | `PlayerAttackMiss` | `27_sword_miss_1/2/3.wav` | `Entity_SFX` / `AudioManager` |
| 玩家格挡 / 反弹起手 | `PlayerBlock` | `02_chest_close_1.wav` | `Player_CounterAttackState` |
| 玩家格挡成功 | `PlayerCounterSuccess` | `26_sword_hit_2.wav` | `Player_CounterAttackState` |
| 玩家冲刺 | `PlayerDash` | `15_human_dash_1/2.wav` | `Player_DashState` |
| 玩家使用药水 | `PlayerPotionUse` | `08_human_charge_1/2.wav` | `Player.TryUseHealingPotion` |
| 玩家喝药完成 | `PlayerPotionComplete` | `08_human_charge_2.wav` | `Player.CompleteHealingPotion` |
| 玩家跳跃 | `PlayerJump` | `12_human_jump_1/2/3.wav` | `Player_JumpState` / `Player_WallJumpState` |
| 篝火火焰循环 | `BonfireFlameLoop` | `Fire_Burning.mp3` | `Bonfire` |
| 深渊之火火焰循环 | `AbyssFireFlameLoop` | `Fire_Burning.mp3` | `AbyssFire` |
| 玩家受伤 | `PlayerHurt` | `11_human_damage_1/2/3.wav` | `Entity_Health` / `Enemy` |
| 玩家死亡 | `PlayerDeath` | `14_human_death_spin.wav` | `Entity_Health` / `Enemy` |
| 敌人受伤 | `EnemyHurt` | `21_orc_damage_1/2/3.wav` | `Enemy` |
| 敌人死亡 | `EnemyDeath` | `24_orc_death_spin.wav` | `Enemy` |
| 主菜单按钮点击 | `ButtonClick` | `03_crate_open_1.wav` | `UI_MainMenu` / `UI_Options` |
| 主菜单 BGM | `PlaylistMainMenu` | `Goblins_Den_(Regular).wav` | `UI_MainMenu` / `LevelManager` |
| 关卡 BGM | `PlaylistLevels` | `Goblins_Dance_(Battle).wav` | `LevelManager` |
| 篝火点燃 | `BonfireIgnite` | `08_human_charge_1/2.wav` | `Bonfire` |
| 篝火休息 | `BonfireRest` | `04_sack_open_1/2/3.wav` | `Bonfire` |
| 篝火菜单打开 | `BonfireMenuOpen` | `01_chest_open_1/2/3/4.wav` | `BonfireTravelMenu` / `Bonfire` |
| 篝火菜单关闭 | `BonfireMenuClose` | `02_chest_close_1/2/3.wav` | `BonfireTravelMenu` / `Bonfire` |
| 篝火传送确认 | `BonfireTravel` | `10_human_special_atk_1/2.wav` | `BonfireTravelMenu` / `Bonfire` |
| 竞技场门打开 | `ArenaDoorOpen` | `05_door_open_1.mp3` | `ArenaDoorController` |
| 竞技场门关闭 | `ArenaDoorClose` | `06_door_close_1.mp3` | `ArenaDoorController` |
| 法师施法 | `MageSpellCast` | `10_human_special_atk_1/2.wav` | `Enemy_Mage` / `Enemy_AbyssMage` |

## 说明

- 当前项目已经用 `AudioKey + AudioDatabaseSO` 统一管理音频查找，不再依赖散落的字符串硬编码。
- `PlayerBlock` 先用 `02_chest_close_1.wav` 做占位，先补齐反馈，再根据后续资源再替换成更贴合的格挡音。
- `PlayerCounterSuccess` 先用 `26_sword_hit_2.wav` 做占位，表示格挡成功后的刀刃碰撞反馈。
- `PlayerDash` 先用 `15_human_dash_1/2.wav` 做占位，匹配冲刺起手。
- `PlayerPotionUse` 先用 `08_human_charge_1/2.wav` 做占位，匹配喝药起手。
- `PlayerPotionComplete` 现在使用 `08_human_charge_2.wav`，和喝药起手同风格但不同。
- `PlayerJump` 先用 `12_human_jump_1/2/3.wav` 做占位，匹配普通跳和墙跳起手。
- `BonfireFlameLoop` 和 `AbyssFireFlameLoop` 现在都使用 `Fire_Burning.mp3`，差异只在组件里的音量和传播范围。
- 如果后面导入了更合适的格挡音效，只需要替换 `AUDIO DATABASE.asset` 里的 `player_block` 条目，不需要改调用代码。
- 如果后面导入了更合适的“格挡成功”音效，只需要替换 `player_counterSuccess` 条目，不需要改调用代码。
- 如果后面导入了更合适的冲刺或喝药音效，只需要替换对应的 `player_dash` / `player_potion_use` 条目，不需要改调用代码。
- 如果后面导入了更合适的喝药完成音效，只需要替换 `player_potion_complete` 条目，不需要改调用代码。
- 如果后面导入了更合适的跳跃音效，只需要替换 `player_jump` 条目，不需要改调用代码。
