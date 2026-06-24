using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class AudioHubWindow : EditorWindow
{
    private readonly struct AudioGroupDefinition
    {
        public readonly string Label;
        public readonly string PropertyName;

        public AudioGroupDefinition(string label, string propertyName)
        {
            Label = label;
            PropertyName = propertyName;
        }
    }

    private static readonly AudioGroupDefinition[] GroupDefinitions =
    {
        new("Player SFX", "player"),
        new("Combat SFX", "combatAudio"),
        new("UI Audio", "uiAudio"),
        new("Bonfire SFX", "bonfireAudio"),
        new("Main Menu BGM", "mainMenuMusic"),
        new("Scene BGM", "levelMusic"),
        new("Door Battle BGM", "doorBattleMusic"),
        new("Boss Battle BGM", "bossBattleMusic")
    };

    private static readonly BindingFlags BindingFlagsAll =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    private static MethodInfo playPreviewClipMethod;
    private static MethodInfo stopAllPreviewClipsMethod;

    private AudioDatabaseSO database;
    private SerializedObject serializedDatabase;
    private Vector2 scrollPosition;
    private string searchText = string.Empty;
    private bool showEmptyEntries = true;
    private int previewGroupIndex = -1;
    private readonly Dictionary<string, int> previewClipIndexByEntry = new();

    [MenuItem("Tools/Audio/Audio Hub")]
    public static void Open()
    {
        GetWindow<AudioHubWindow>("Audio Hub");
    }

    private void OnEnable()
    {
        LoadDefaultDatabaseIfNeeded();
        RebuildSerializedObject();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (database == null)
        {
            EditorGUILayout.HelpBox("未找到 AudioDatabaseSO。请先指定数据库，或者点击 Load Resources Database。", MessageType.Warning);
            return;
        }

        if (serializedDatabase == null || serializedDatabase.targetObject != database)
        {
            RebuildSerializedObject();
        }

        if (serializedDatabase == null)
        {
            EditorGUILayout.HelpBox("数据库对象无效。", MessageType.Error);
            return;
        }

        serializedDatabase.Update();

        DrawOverview();

        EditorGUILayout.Space(6f);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < GroupDefinitions.Length; i++)
        {
            DrawGroup(i, GroupDefinitions[i]);
        }

        EditorGUILayout.EndScrollView();

        if (serializedDatabase.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                database = (AudioDatabaseSO)EditorGUILayout.ObjectField("Database", database, typeof(AudioDatabaseSO), false);
                if (EditorGUI.EndChangeCheck())
                {
                    RebuildSerializedObject();
                }

                if (GUILayout.Button("Load Resources Database", GUILayout.Width(170f)))
                {
                    LoadDefaultDatabase();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                searchText = EditorGUILayout.TextField("Search", searchText);
                showEmptyEntries = EditorGUILayout.ToggleLeft("Show Empty", showEmptyEntries, GUILayout.Width(100f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Ping Database"))
                {
                    PingDatabase();
                }

                if (GUILayout.Button("Open in Inspector"))
                {
                    Selection.activeObject = database;
                    EditorGUIUtility.PingObject(database);
                }

                if (GUILayout.Button("Stop Preview"))
                {
                    StopPreview();
                }
            }
        }
    }

    private void DrawOverview()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("This page edits the canonical AudioDatabaseSO asset used by AudioManager.");
            EditorGUILayout.LabelField("Preview", previewGroupIndex >= 0 ? GroupDefinitions[previewGroupIndex].Label : "None");
        }
    }

    private void DrawGroup(int groupIndex, AudioGroupDefinition definition)
    {
        SerializedProperty listProperty = serializedDatabase.FindProperty(definition.PropertyName);
        if (listProperty == null)
        {
            return;
        }

        bool searchActive = !string.IsNullOrWhiteSpace(searchText);

        bool hasVisibleEntry = false;
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
            if (element == null)
            {
                continue;
            }

            if (ShouldDisplayEntry(element, definition.Label, searchActive))
            {
                hasVisibleEntry = true;
                break;
            }
        }

        if (!hasVisibleEntry)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{definition.Label} ({listProperty.arraySize})", EditorStyles.boldLabel);

                if (GUILayout.Button("Add Entry", GUILayout.Width(90f)))
                {
                    AddEntry(listProperty, definition.Label);
                }

                if (GUILayout.Button("Fill Defaults", GUILayout.Width(95f)))
                {
                    EnsureDefaults(listProperty, groupIndex);
                }
            }

            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                if (element == null)
                {
                    continue;
                }

                if (!ShouldDisplayEntry(element, definition.Label, searchActive))
                {
                    continue;
                }

                DrawEntry(groupIndex, i, element, definition.PropertyName);
            }
        }
    }

    private void DrawEntry(int groupIndex, int index, SerializedProperty element, string propertyName)
    {
        SerializedProperty audioName = element.FindPropertyRelative("audioName");
        SerializedProperty clips = element.FindPropertyRelative("clips");
        SerializedProperty maxVolume = element.FindPropertyRelative("maxVolume");

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                string nextName = audioName != null ? EditorGUILayout.TextField("Key", audioName.stringValue) : string.Empty;
                if (EditorGUI.EndChangeCheck() && audioName != null)
                {
                    audioName.stringValue = nextName;
                }

                if (GUILayout.Button("Play", GUILayout.Width(50f)))
                {
                    PlayEntry(groupIndex, element);
                }

                if (GUILayout.Button("Stop", GUILayout.Width(50f)))
                {
                    StopPreview();
                }

                if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                {
                    PingFirstClip(clips);
                }

                if (GUILayout.Button("X", GUILayout.Width(24f)))
                {
                    SerializedProperty listProperty = serializedDatabase.FindProperty(propertyName);
                    if (listProperty != null)
                    {
                        listProperty.DeleteArrayElementAtIndex(index);
                        serializedDatabase.ApplyModifiedProperties();
                        EditorUtility.SetDirty(database);
                    }

                    GUIUtility.ExitGUI();
                }
            }

            if (maxVolume != null)
            {
                maxVolume.floatValue = EditorGUILayout.Slider("Max Volume", maxVolume.floatValue, 0f, 1f);
            }

            if (clips != null)
            {
                EditorGUILayout.PropertyField(clips, new GUIContent("Clips"), true);
                DrawPreviewClipSelector(element, clips, propertyName, index);
            }

            if (audioName != null && string.IsNullOrWhiteSpace(audioName.stringValue))
            {
                EditorGUILayout.HelpBox("这个条目还没有 lookup key。", MessageType.Info);
            }
        }
    }

    private static bool IsEffectivelyEmpty(SerializedProperty element)
    {
        if (element == null)
        {
            return true;
        }

        SerializedProperty audioName = element.FindPropertyRelative("audioName");
        SerializedProperty clips = element.FindPropertyRelative("clips");

        bool hasName = audioName != null && string.IsNullOrWhiteSpace(audioName.stringValue) == false;
        bool hasClips = clips != null && clips.arraySize > 0;

        return hasName == false && hasClips == false;
    }

    private void DrawPreviewClipSelector(SerializedProperty element, SerializedProperty clips, string propertyName, int index)
    {
        if (clips == null || clips.arraySize == 0)
        {
            return;
        }

        string entryKey = GetEntryKey(element, propertyName, index);

        List<string> options = new();
        List<int> clipIndexes = new();
        for (int i = 0; i < clips.arraySize; i++)
        {
            SerializedProperty clipProperty = clips.GetArrayElementAtIndex(i);
            AudioClip clip = clipProperty != null ? clipProperty.objectReferenceValue as AudioClip : null;
            if (clip == null)
            {
                continue;
            }

            options.Add($"{i}: {clip.name}");
            clipIndexes.Add(i);
        }

        if (options.Count == 0)
        {
            return;
        }

        int selectedClipIndex = GetPreviewClipIndex(entryKey, clipIndexes[0]);
        int visibleIndex = 0;
        for (int i = 0; i < clipIndexes.Count; i++)
        {
            if (clipIndexes[i] == selectedClipIndex)
            {
                visibleIndex = i;
                break;
            }
        }

        int nextVisibleIndex = EditorGUILayout.Popup("Preview Clip", visibleIndex, options.ToArray());
        if (nextVisibleIndex >= 0 && nextVisibleIndex < clipIndexes.Count)
        {
            SetPreviewClipIndex(entryKey, clipIndexes[nextVisibleIndex]);
        }
    }

    private bool ShouldDisplayEntry(SerializedProperty element, string groupLabel, bool searchActive)
    {
        if (searchActive)
        {
            return EntryMatchesSearch(element, groupLabel);
        }

        if (showEmptyEntries)
        {
            return true;
        }

        return IsEffectivelyEmpty(element) == false;
    }

    private void PlayEntry(int groupIndex, SerializedProperty element)
    {
        SerializedProperty clips = element.FindPropertyRelative("clips");

        if (clips == null || clips.arraySize == 0)
        {
            return;
        }

        AudioClip clip = GetSelectedPreviewClip(element, clips, groupIndex);
        if (clip == null)
        {
            for (int i = 0; i < clips.arraySize; i++)
            {
                SerializedProperty clipProperty = clips.GetArrayElementAtIndex(i);
                if (clipProperty != null && clipProperty.objectReferenceValue is AudioClip fallbackClip)
                {
                    clip = fallbackClip;
                    break;
                }
            }
        }

        if (clip == null)
        {
            return;
        }

        previewGroupIndex = groupIndex;

        PreviewClip(clip);
    }

    private static void PingFirstClip(SerializedProperty clips)
    {
        if (clips == null || clips.arraySize == 0)
        {
            return;
        }

        SerializedProperty first = clips.GetArrayElementAtIndex(0);
        if (first?.objectReferenceValue != null)
        {
            EditorGUIUtility.PingObject(first.objectReferenceValue);
            Selection.activeObject = first.objectReferenceValue;
        }
    }

    private static void PreviewClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        StopPreview();
        EnsurePreviewMethods();

        if (playPreviewClipMethod != null)
        {
            object[] args = { clip, 0, false };
            playPreviewClipMethod.Invoke(null, args);
        }
    }

    private static void StopPreview()
    {
        EnsurePreviewMethods();
        stopAllPreviewClipsMethod?.Invoke(null, null);
    }

    private AudioClip GetSelectedPreviewClip(SerializedProperty element, SerializedProperty clips, int groupIndex)
    {
        if (clips == null || clips.arraySize == 0 || element == null)
        {
            return null;
        }

        string entryKey = GetEntryKey(element, GroupDefinitions[groupIndex].PropertyName, GetElementIndex(element));
        int selectedIndex = GetPreviewClipIndex(entryKey, 0);

        if (selectedIndex < 0 || selectedIndex >= clips.arraySize)
        {
            return null;
        }

        SerializedProperty clipProperty = clips.GetArrayElementAtIndex(selectedIndex);
        return clipProperty != null ? clipProperty.objectReferenceValue as AudioClip : null;
    }

    private int GetPreviewClipIndex(string entryKey, int fallbackIndex)
    {
        if (!string.IsNullOrWhiteSpace(entryKey) && previewClipIndexByEntry.TryGetValue(entryKey, out int index))
        {
            return Mathf.Max(0, index);
        }

        return Mathf.Max(0, fallbackIndex);
    }

    private void SetPreviewClipIndex(string entryKey, int clipIndex)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return;
        }

        previewClipIndexByEntry[entryKey] = Mathf.Max(0, clipIndex);
    }

    private static string GetEntryKey(SerializedProperty element, string propertyName, int index)
    {
        string audioName = element != null ? element.FindPropertyRelative("audioName")?.stringValue : string.Empty;
        return $"{propertyName}:{index}:{audioName}";
    }

    private static int GetElementIndex(SerializedProperty element)
    {
        string path = element != null ? element.propertyPath : string.Empty;
        int start = path.LastIndexOf('[');
        int end = path.LastIndexOf(']');
        if (start < 0 || end <= start)
        {
            return 0;
        }

        string indexText = path.Substring(start + 1, end - start - 1);
        return int.TryParse(indexText, out int index) ? index : 0;
    }

    private static void EnsurePreviewMethods()
    {
        if (playPreviewClipMethod != null && stopAllPreviewClipsMethod != null)
        {
            return;
        }

        Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtilType == null)
        {
            return;
        }

        playPreviewClipMethod ??= audioUtilType.GetMethod("PlayPreviewClip", BindingFlagsAll, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null)
            ?? audioUtilType.GetMethod("PlayClip", BindingFlagsAll, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        stopAllPreviewClipsMethod ??= audioUtilType.GetMethod("StopAllPreviewClips", BindingFlagsAll);
    }

    private void EnsureDefaults(SerializedProperty listProperty, int groupIndex)
    {
        if (listProperty == null)
        {
            return;
        }

        switch (groupIndex)
        {
            case 0:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerAttackHit));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerAttackMiss));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerBlock));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerCounterSuccess));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerDash));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerPotionUse));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerPotionComplete));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerJump));
                break;
            case 1:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerHurt));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlayerDeath));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.EnemyHurt));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.EnemyDeath));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.AbyssMageFireballExplosion));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.SpellWindLoop));
                break;
            case 2:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.ButtonClick));
                break;
            case 3:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.BonfireIgnite));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.BonfireRest));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.BonfireMenuOpen));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.BonfireMenuClose));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.BonfireTravel));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.BonfireFlameLoop));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.AbyssFireAppear));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.AbyssFireDisappear));
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.AbyssFireFlameLoop));
                break;
            case 4:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlaylistMainMenu));
                break;
            case 5:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlaylistLevels));
                break;
            case 6:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlaylistDoorBattle));
                break;
            case 7:
                EnsureEntry(listProperty, AudioKeyMap.GetAudioName(AudioKey.PlaylistBossBattle));
                break;
        }
    }

    private static void EnsureEntry(SerializedProperty listProperty, string audioName)
    {
        if (listProperty == null || string.IsNullOrWhiteSpace(audioName) || HasEntry(listProperty, audioName))
        {
            return;
        }

        listProperty.arraySize++;
        SerializedProperty element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
        if (element == null)
        {
            return;
        }

        SerializedProperty nameProperty = element.FindPropertyRelative("audioName");
        SerializedProperty clipsProperty = element.FindPropertyRelative("clips");
        SerializedProperty maxVolumeProperty = element.FindPropertyRelative("maxVolume");

        if (nameProperty != null)
        {
            nameProperty.stringValue = audioName;
        }

        if (clipsProperty != null)
        {
            clipsProperty.arraySize = 0;
        }

        if (maxVolumeProperty != null)
        {
            maxVolumeProperty.floatValue = 1f;
        }
    }

    private static void AddEntry(SerializedProperty listProperty, string label)
    {
        if (listProperty == null)
        {
            return;
        }

        listProperty.arraySize++;
        SerializedProperty element = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
        if (element == null)
        {
            return;
        }

        SerializedProperty nameProperty = element.FindPropertyRelative("audioName");
        SerializedProperty clipsProperty = element.FindPropertyRelative("clips");
        SerializedProperty maxVolumeProperty = element.FindPropertyRelative("maxVolume");

        if (nameProperty != null)
        {
            nameProperty.stringValue = $"{label.Replace(" ", "_").ToLowerInvariant()}_new";
        }

        if (clipsProperty != null)
        {
            clipsProperty.arraySize = 0;
        }

        if (maxVolumeProperty != null)
        {
            maxVolumeProperty.floatValue = 1f;
        }
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

            SerializedProperty nameProperty = element.FindPropertyRelative("audioName");
            if (nameProperty != null && nameProperty.stringValue == audioKey)
            {
                return true;
            }
        }

        return false;
    }

    private bool EntryMatchesSearch(SerializedProperty element, string groupLabel)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        string keyword = searchText.Trim();
        if (groupLabel != null && groupLabel.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        SerializedProperty audioName = element.FindPropertyRelative("audioName");
        if (audioName != null && audioName.stringValue.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        SerializedProperty clips = element.FindPropertyRelative("clips");
        if (clips != null)
        {
            for (int i = 0; i < clips.arraySize; i++)
            {
                SerializedProperty clipProperty = clips.GetArrayElementAtIndex(i);
                AudioClip clip = clipProperty != null ? clipProperty.objectReferenceValue as AudioClip : null;
                if (clip != null && clip.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void LoadDefaultDatabase()
    {
        database = Resources.Load<AudioDatabaseSO>("Audio/AUDIO DATABASE");
        RebuildSerializedObject();
    }

    private void LoadDefaultDatabaseIfNeeded()
    {
        if (database == null)
        {
            LoadDefaultDatabase();
        }
    }

    private void RebuildSerializedObject()
    {
        serializedDatabase = database != null ? new SerializedObject(database) : null;
    }

    private void PingDatabase()
    {
        if (database == null)
        {
            return;
        }

        Selection.activeObject = database;
        EditorGUIUtility.PingObject(database);
    }
}
