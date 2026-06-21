using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioDatabaseSO))]
public class AudioDatabaseSOEditor : Editor
{
    private readonly struct AudioGroupDefinition
    {
        public readonly string Label;
        public readonly string PropertyName;
        public readonly AudioKey[] RequiredKeys;

        public AudioGroupDefinition(string label, string propertyName, AudioKey[] requiredKeys)
        {
            Label = label;
            PropertyName = propertyName;
            RequiredKeys = requiredKeys;
        }
    }

    private static readonly AudioGroupDefinition[] GroupDefinitions =
    {
        new("Player SFX", "player", new[]
        {
            AudioKey.PlayerAttackHit,
            AudioKey.PlayerAttackMiss,
            AudioKey.PlayerBlock,
            AudioKey.PlayerCounterSuccess,
            AudioKey.PlayerDash,
            AudioKey.PlayerPotionUse,
            AudioKey.PlayerPotionComplete,
            AudioKey.PlayerJump
        }),
        new("Combat SFX", "combatAudio", new[]
        {
            AudioKey.PlayerHurt,
            AudioKey.PlayerDeath,
            AudioKey.EnemyHurt,
            AudioKey.EnemyDeath,
            AudioKey.AbyssMageFireballExplosion,
            AudioKey.SpellWindLoop
        }),
        new("UI Audio", "uiAudio", new[]
        {
            AudioKey.ButtonClick
        }),
        new("Bonfire SFX", "bonfireAudio", new[]
        {
            AudioKey.BonfireIgnite,
            AudioKey.BonfireRest,
            AudioKey.BonfireMenuOpen,
            AudioKey.BonfireMenuClose,
            AudioKey.BonfireTravel,
            AudioKey.BonfireFlameLoop,
            AudioKey.AbyssFireFlameLoop
        }),
        new("Main Menu BGM", "mainMenuMusic", new[]
        {
            AudioKey.PlaylistMainMenu
        }),
        new("Level BGM", "levelMusic", new[]
        {
            AudioKey.PlaylistLevels
        })
    };

    private bool[] foldouts =
    {
        true, true, true, true, true, true
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SearchableInspectorDrawer.DrawScriptField(serializedObject);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Open Audio Hub"))
            {
                AudioHubWindow.Open();
            }
        }

        EditorGUILayout.HelpBox(
            "Use AudioKey names as lookup keys in audioName. Each group below can be filled from defaults or edited manually.",
            MessageType.Info
        );

        DrawRequiredSummary();

        DrawAllGroups();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAllGroups()
    {
        for (int i = 0; i < GroupDefinitions.Length; i++)
        {
            DrawGroup(GroupDefinitions[i], ref foldouts[i]);
        }
    }

    private void DrawRequiredSummary()
    {
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Coverage", EditorStyles.boldLabel);
            for (int i = 0; i < GroupDefinitions.Length; i++)
            {
                AudioGroupDefinition definition = GroupDefinitions[i];
                EditorGUILayout.LabelField(definition.Label, GetMissingKeysText(GetGroupProperty(definition), definition.RequiredKeys));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill Required"))
                {
                    for (int i = 0; i < GroupDefinitions.Length; i++)
                    {
                        AudioGroupDefinition definition = GroupDefinitions[i];
                        EnsureEntries(GetGroupProperty(definition), definition.RequiredKeys);
                    }
                }

                if (GUILayout.Button("Clamp Volumes"))
                {
                    for (int i = 0; i < GroupDefinitions.Length; i++)
                    {
                        NormalizeVolumes(GetGroupProperty(GroupDefinitions[i]));
                    }
                }
            }
        }
    }

    private void DrawGroup(AudioGroupDefinition definition, ref bool foldout)
    {
        SerializedProperty listProperty = GetGroupProperty(definition);
        if (listProperty == null)
        {
            return;
        }

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                foldout = EditorGUILayout.Foldout(foldout, $"{definition.Label} ({listProperty.arraySize})", true);
                if (definition.RequiredKeys != null && definition.RequiredKeys.Length > 0 && GUILayout.Button("+ Defaults", GUILayout.Width(90f)))
                {
                    EnsureEntries(listProperty, definition.RequiredKeys);
                }

                if (GUILayout.Button("+ Entry", GUILayout.Width(70f)))
                {
                    AddEntry(listProperty, definition.Label);
                }
            }

            if (!foldout)
            {
                return;
            }

            DrawEntries(listProperty);
        }
    }

    private static void EnsureEntries(SerializedProperty listProperty, params AudioKey[] audioKeys)
    {
        if (listProperty == null || audioKeys == null || audioKeys.Length == 0)
        {
            return;
        }

        for (int i = 0; i < audioKeys.Length; i++)
        {
            AudioKey key = audioKeys[i];
            string audioName = AudioKeyMap.GetAudioName(key);
            if (string.IsNullOrWhiteSpace(audioName) || HasEntry(listProperty, audioName))
            {
                continue;
            }

            listProperty.arraySize++;
            SerializedProperty element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
            SerializedProperty audioNameProperty = element.FindPropertyRelative("audioName");
            SerializedProperty clips = element.FindPropertyRelative("clips");
            SerializedProperty maxVolume = element.FindPropertyRelative("maxVolume");
            SerializedProperty maxHearDistance = element.FindPropertyRelative("maxHearDistance");

            if (audioNameProperty != null)
            {
                audioNameProperty.stringValue = audioName;
            }

            if (clips != null)
            {
                clips.arraySize = 0;
            }

            if (maxVolume != null)
            {
                maxVolume.floatValue = 1f;
            }

            if (maxHearDistance != null)
            {
                maxHearDistance.floatValue = 12f;
            }
        }
    }

    private static void AddEntry(SerializedProperty listProperty, string label)
    {
        listProperty.arraySize++;
        SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
        SerializedProperty audioName = newElement.FindPropertyRelative("audioName");
        SerializedProperty clips = newElement.FindPropertyRelative("clips");
        SerializedProperty maxVolume = newElement.FindPropertyRelative("maxVolume");
        SerializedProperty maxHearDistance = newElement.FindPropertyRelative("maxHearDistance");

        if (audioName != null)
        {
            audioName.stringValue = string.IsNullOrWhiteSpace(label)
                ? "new_audio"
                : $"{label.Replace(" ", "_").ToLowerInvariant()}_new";
        }

        if (clips != null)
        {
            clips.arraySize = 0;
        }

        if (maxVolume != null)
        {
            maxVolume.floatValue = 1f;
        }

        if (maxHearDistance != null)
        {
            maxHearDistance.floatValue = 12f;
        }
    }

    private static void DrawEntries(SerializedProperty listProperty)
    {
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element == null)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                SerializedProperty audioName = element.FindPropertyRelative("audioName");
                SerializedProperty clips = element.FindPropertyRelative("clips");
                SerializedProperty maxVolume = element.FindPropertyRelative("maxVolume");
                SerializedProperty maxHearDistance = element.FindPropertyRelative("maxHearDistance");

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (audioName != null)
                    {
                        audioName.stringValue = EditorGUILayout.TextField("Lookup Key", audioName.stringValue);
                    }

                    if (GUILayout.Button("X", GUILayout.Width(24f)))
                    {
                        listProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                if (maxVolume != null)
                {
                    maxVolume.floatValue = EditorGUILayout.Slider("Volume", maxVolume.floatValue, 0f, 1f);
                }

                if (maxHearDistance != null)
                {
                    maxHearDistance.floatValue = EditorGUILayout.Slider("Max Hear Distance", maxHearDistance.floatValue, 0.01f, 50f);
                }

                if (clips != null)
                {
                    EditorGUILayout.PropertyField(clips, new GUIContent("Clips"), true);
                }
            }
        }
    }

    private SerializedProperty GetGroupProperty(AudioGroupDefinition definition)
    {
        return serializedObject.FindProperty(definition.PropertyName);
    }

    private static bool HasEntry(SerializedProperty listProperty, string audioKey)
    {
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element == null)
            {
                continue;
            }

            SerializedProperty audioName = element.FindPropertyRelative("audioName");
            if (audioName != null && audioName.stringValue == audioKey)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetMissingKeysText(SerializedProperty listProperty, AudioKey[] requiredKeys)
    {
        if (requiredKeys == null || requiredKeys.Length == 0)
        {
            return "None";
        }

        System.Collections.Generic.List<string> missing = new();
        for (int i = 0; i < requiredKeys.Length; i++)
        {
            string audioName = AudioKeyMap.GetAudioName(requiredKeys[i]);
            if (!HasEntry(listProperty, audioName))
            {
                missing.Add(audioName);
            }
        }

        return missing.Count == 0 ? "OK" : string.Join(", ", missing);
    }

    private static void NormalizeVolumes(SerializedProperty listProperty)
    {
        if (listProperty == null)
        {
            return;
        }

        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element == null)
            {
                continue;
            }

            SerializedProperty maxVolume = element.FindPropertyRelative("maxVolume");
            if (maxVolume != null)
            {
                maxVolume.floatValue = Mathf.Clamp01(maxVolume.floatValue);
            }

            SerializedProperty maxHearDistance = element.FindPropertyRelative("maxHearDistance");
            if (maxHearDistance != null)
            {
                maxHearDistance.floatValue = Mathf.Max(0.01f, maxHearDistance.floatValue);
            }
        }
    }
}
