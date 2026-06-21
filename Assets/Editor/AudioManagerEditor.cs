using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[CustomEditor(typeof(AudioManager))]
public class AudioManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SearchableInspectorDrawer.DrawScriptField(serializedObject);

        SerializedProperty audioDbProperty = serializedObject.FindProperty("audioDB");
        SerializedProperty audioMixerProperty = serializedObject.FindProperty("audioMixer");
        SerializedProperty bgmMixerGroupProperty = serializedObject.FindProperty("bgmMixerGroup");
        SerializedProperty sfxMixerGroupProperty = serializedObject.FindProperty("sfxMixerGroup");
        SerializedProperty bgmSourceProperty = serializedObject.FindProperty("bgmSource");
        SerializedProperty sfxSourceProperty = serializedObject.FindProperty("sfxSource");

        EditorGUILayout.HelpBox(
            "Runtime can create this object automatically. Use this inspector to verify references, repair mixer routing, and rebuild missing child sources.",
            MessageType.Info
        );

        DrawSection("References", () =>
        {
            DrawPropertyField(audioDbProperty, "Audio Database");
            DrawPropertyField(audioMixerProperty, "Audio Mixer");
            DrawPropertyField(bgmMixerGroupProperty, "BGM Mixer Group");
            DrawPropertyField(sfxMixerGroupProperty, "SFX Mixer Group");
            DrawPropertyField(bgmSourceProperty, "BGM Source");
            DrawPropertyField(sfxSourceProperty, "SFX Source");
        });

        DrawSection("Tools", () =>
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Database"))
                {
                    AudioDatabaseSO db = Resources.Load<AudioDatabaseSO>("Audio/AUDIO DATABASE");
                    if (audioDbProperty != null)
                    {
                        audioDbProperty.objectReferenceValue = db;
                    }
                }

                if (GUILayout.Button("Load Mixer"))
                {
                    AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Settings/AudioMixer.mixer");
                    if (audioMixerProperty != null)
                    {
                        audioMixerProperty.objectReferenceValue = mixer;
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Auto Assign Groups"))
                {
                    AssignMixerGroups(audioMixerProperty, bgmMixerGroupProperty, sfxMixerGroupProperty);
                }

                if (GUILayout.Button("Rebuild Sources"))
                {
                    CreateMissingSource("BGM Source", bgmSourceProperty);
                    CreateMissingSource("SFX Source", sfxSourceProperty);
                }
            }
        });

        AudioManager manager = (AudioManager)target;
        DrawSection("Runtime Status", () =>
        {
            DrawStatusRow("Instance", AudioManager.instance == manager ? "This object" : (AudioManager.instance != null ? "Other instance" : "None"));
            DrawStatusRow("Database", IsAssigned(audioDbProperty));
            DrawStatusRow("Mixer", IsAssigned(audioMixerProperty));
            DrawStatusRow("BGM Group", IsAssigned(bgmMixerGroupProperty));
            DrawStatusRow("SFX Group", IsAssigned(sfxMixerGroupProperty));
            DrawStatusRow("BGM Source", IsAssigned(bgmSourceProperty));
            DrawStatusRow("SFX Source", IsAssigned(sfxSourceProperty));
        });

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawSection(string title, System.Action drawContent)
    {
        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            drawContent?.Invoke();
        }
    }

    private static void DrawPropertyField(SerializedProperty property, string label)
    {
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }

    private static void DrawStatusRow(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, GUILayout.Width(120f));
            EditorGUILayout.LabelField(value);
        }
    }

    private static string IsAssigned(SerializedProperty property)
    {
        return property != null && property.objectReferenceValue != null ? "Assigned" : "Missing";
    }

    private static void AssignMixerGroups(SerializedProperty audioMixerProperty, SerializedProperty bgmMixerGroupProperty, SerializedProperty sfxMixerGroupProperty)
    {
        if (audioMixerProperty == null || audioMixerProperty.objectReferenceValue == null)
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/Settings/AudioMixer.mixer");
            if (mixer == null)
            {
                return;
            }

            audioMixerProperty.objectReferenceValue = mixer;
        }

        AudioMixer mixerAsset = audioMixerProperty.objectReferenceValue as AudioMixer;
        if (mixerAsset == null)
        {
            return;
        }

        AudioMixerGroup bgmGroup = FindGroup(mixerAsset, "Background music");
        AudioMixerGroup sfxGroup = FindGroup(mixerAsset, "Sound effects");

        if (bgmMixerGroupProperty != null && bgmGroup != null)
        {
            bgmMixerGroupProperty.objectReferenceValue = bgmGroup;
        }

        if (sfxMixerGroupProperty != null && sfxGroup != null)
        {
            sfxMixerGroupProperty.objectReferenceValue = sfxGroup;
        }
    }

    private static AudioMixerGroup FindGroup(AudioMixer mixer, string groupName)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(groupName))
        {
            return null;
        }

        AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
        if (groups != null && groups.Length > 0)
        {
            return groups[0];
        }

        return null;
    }

    private void CreateMissingSource(string childName, SerializedProperty sourceProperty)
    {
        AudioManager manager = (AudioManager)target;
        if (manager == null || sourceProperty == null || sourceProperty.objectReferenceValue != null)
        {
            return;
        }

        Transform child = manager.transform.Find(childName);
        GameObject childObject;
        if (child == null)
        {
            childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            childObject.transform.SetParent(manager.transform, false);
        }
        else
        {
            childObject = child.gameObject;
        }

        AudioSource source = childObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = Undo.AddComponent<AudioSource>(childObject);
        }

        source.playOnAwake = false;
        source.loop = false;

        sourceProperty.objectReferenceValue = source;
    }
}
