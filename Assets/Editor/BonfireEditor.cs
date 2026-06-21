using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(Bonfire))]
public class BonfireEditor : Editor
{
    private SerializedProperty bonfireIdProperty;
    private SerializedProperty bonfireDisplayNameProperty;
    private SerializedProperty bonfireActivatedProperty;
    private SerializedProperty restorePlayerOnRestProperty;
    private SerializedProperty reloadSceneOnRestProperty;
    private SerializedProperty restDelayProperty;
    private SerializedProperty startLitProperty;
    private SerializedProperty flameFramesProperty;
    private SerializedProperty framesPerSecondProperty;
    private SerializedProperty loopAnimationProperty;
    private SerializedProperty autoLoadDefaultFramesProperty;
    private SerializedProperty unlitTintProperty;
    private SerializedProperty litTintProperty;
    private SerializedProperty ignitePulseScaleProperty;
    private SerializedProperty ignitePulseDurationProperty;
    private SerializedProperty promptFontProperty;
    private SerializedProperty restPromptProperty;
    private SerializedProperty travelPromptProperty;
    private SerializedProperty ignitePromptProperty;
    private SerializedProperty promptCursorSymbolProperty;
    private SerializedProperty restPromptIconProperty;
    private SerializedProperty travelPromptIconProperty;
    private SerializedProperty ignitePromptIconProperty;
    private SerializedProperty promptIconSizeProperty;
    private SerializedProperty promptRowHeightProperty;
    private SerializedProperty promptRowSpacingProperty;
    private SerializedProperty promptRowPaddingXProperty;
    private SerializedProperty promptRowPaddingYProperty;
    private SerializedProperty promptRowBackgroundProperty;
    private SerializedProperty promptRowSelectedBackgroundProperty;
    private SerializedProperty playerSortingOrderOffsetProperty;
    private SerializedProperty promptLocalOffsetProperty;
    private SerializedProperty promptFontSizeProperty;
    private SerializedProperty promptColorProperty;
    private SerializedProperty selectedPromptColorProperty;
    private SerializedProperty unselectedPromptColorProperty;
    private SerializedProperty travelMenuFontProperty;
    private SerializedProperty travelMenuTitleProperty;
    private SerializedProperty travelMenuHintProperty;
    private SerializedProperty travelMenuEmptyTextProperty;
    private SerializedProperty travelMenuCloseLabelProperty;
    private SerializedProperty travelMenuCursorSymbolProperty;
    private SerializedProperty travelMenuCloseIconProperty;
    private SerializedProperty travelMenuCloseButtonColorProperty;
    private SerializedProperty travelMenuBackdropColorProperty;
    private SerializedProperty travelMenuPanelColorProperty;
    private SerializedProperty travelMenuListColorProperty;
    private SerializedProperty travelMenuRowColorProperty;
    private SerializedProperty travelMenuRowSelectedColorProperty;
    private SerializedProperty travelMenuTitleColorProperty;
    private SerializedProperty travelMenuHintColorProperty;
    private SerializedProperty travelMenuEmptyColorProperty;
    private SerializedProperty travelMenuCursorColorProperty;
    private SerializedProperty travelMenuLabelSelectedColorProperty;
    private SerializedProperty travelMenuLabelUnselectedColorProperty;
    private SerializedProperty travelMenuRootSizeProperty;
    private SerializedProperty travelMenuListSizeProperty;
    private SerializedProperty travelMenuCloseButtonSizeProperty;
    private SerializedProperty travelMenuCloseButtonOffsetProperty;
    private SerializedProperty travelMenuItemHeightProperty;
    private SerializedProperty travelMenuItemSpacingProperty;
    private SerializedProperty travelMenuTitleFontSizeProperty;
    private SerializedProperty travelMenuHintFontSizeProperty;
    private SerializedProperty travelMenuItemFontSizeProperty;
    private SerializedProperty travelMenuCloseFontSizeProperty;
    private SerializedProperty flameLoopSfxKeyProperty;
    private SerializedProperty flameLoopVolumeProperty;
    private SerializedProperty flameLoopMinDistanceProperty;
    private SerializedProperty flameLoopMaxDistanceProperty;
    private SerializedProperty flameRendererProperty;
    private SerializedProperty interactionColliderProperty;

    private void OnEnable()
    {
        bonfireIdProperty = serializedObject.FindProperty("bonfireId");
        bonfireDisplayNameProperty = serializedObject.FindProperty("bonfireDisplayName");
        bonfireActivatedProperty = serializedObject.FindProperty("bonfireActivated");
        restorePlayerOnRestProperty = serializedObject.FindProperty("restorePlayerOnRest");
        reloadSceneOnRestProperty = serializedObject.FindProperty("reloadSceneOnRest");
        restDelayProperty = serializedObject.FindProperty("restDelay");
        startLitProperty = serializedObject.FindProperty("startLit");
        flameFramesProperty = serializedObject.FindProperty("flameFrames");
        framesPerSecondProperty = serializedObject.FindProperty("framesPerSecond");
        loopAnimationProperty = serializedObject.FindProperty("loopAnimation");
        autoLoadDefaultFramesProperty = serializedObject.FindProperty("autoLoadDefaultFrames");
        unlitTintProperty = serializedObject.FindProperty("unlitTint");
        litTintProperty = serializedObject.FindProperty("litTint");
        ignitePulseScaleProperty = serializedObject.FindProperty("ignitePulseScale");
        ignitePulseDurationProperty = serializedObject.FindProperty("ignitePulseDuration");
        promptFontProperty = serializedObject.FindProperty("promptFont");
        restPromptProperty = serializedObject.FindProperty("restPrompt");
        travelPromptProperty = serializedObject.FindProperty("travelPrompt");
        ignitePromptProperty = serializedObject.FindProperty("ignitePrompt");
        promptCursorSymbolProperty = serializedObject.FindProperty("promptCursorSymbol");
        restPromptIconProperty = serializedObject.FindProperty("restPromptIcon");
        travelPromptIconProperty = serializedObject.FindProperty("travelPromptIcon");
        ignitePromptIconProperty = serializedObject.FindProperty("ignitePromptIcon");
        promptIconSizeProperty = serializedObject.FindProperty("promptIconSize");
        promptRowHeightProperty = serializedObject.FindProperty("promptRowHeight");
        promptRowSpacingProperty = serializedObject.FindProperty("promptRowSpacing");
        promptRowPaddingXProperty = serializedObject.FindProperty("promptRowPaddingX");
        promptRowPaddingYProperty = serializedObject.FindProperty("promptRowPaddingY");
        promptRowBackgroundProperty = serializedObject.FindProperty("promptRowBackground");
        promptRowSelectedBackgroundProperty = serializedObject.FindProperty("promptRowSelectedBackground");
        playerSortingOrderOffsetProperty = serializedObject.FindProperty("playerSortingOrderOffset");
        promptLocalOffsetProperty = serializedObject.FindProperty("promptLocalOffset");
        promptFontSizeProperty = serializedObject.FindProperty("promptFontSize");
        promptColorProperty = serializedObject.FindProperty("promptColor");
        selectedPromptColorProperty = serializedObject.FindProperty("selectedPromptColor");
        unselectedPromptColorProperty = serializedObject.FindProperty("unselectedPromptColor");
        travelMenuFontProperty = serializedObject.FindProperty("travelMenuFont");
        travelMenuTitleProperty = serializedObject.FindProperty("travelMenuTitle");
        travelMenuHintProperty = serializedObject.FindProperty("travelMenuHint");
        travelMenuEmptyTextProperty = serializedObject.FindProperty("travelMenuEmptyText");
        travelMenuCloseLabelProperty = serializedObject.FindProperty("travelMenuCloseLabel");
        travelMenuCursorSymbolProperty = serializedObject.FindProperty("travelMenuCursorSymbol");
        travelMenuCloseIconProperty = serializedObject.FindProperty("travelMenuCloseIcon");
        travelMenuCloseButtonColorProperty = serializedObject.FindProperty("travelMenuCloseButtonColor");
        travelMenuBackdropColorProperty = serializedObject.FindProperty("travelMenuBackdropColor");
        travelMenuPanelColorProperty = serializedObject.FindProperty("travelMenuPanelColor");
        travelMenuListColorProperty = serializedObject.FindProperty("travelMenuListColor");
        travelMenuRowColorProperty = serializedObject.FindProperty("travelMenuRowColor");
        travelMenuRowSelectedColorProperty = serializedObject.FindProperty("travelMenuRowSelectedColor");
        travelMenuTitleColorProperty = serializedObject.FindProperty("travelMenuTitleColor");
        travelMenuHintColorProperty = serializedObject.FindProperty("travelMenuHintColor");
        travelMenuEmptyColorProperty = serializedObject.FindProperty("travelMenuEmptyColor");
        travelMenuCursorColorProperty = serializedObject.FindProperty("travelMenuCursorColor");
        travelMenuLabelSelectedColorProperty = serializedObject.FindProperty("travelMenuLabelSelectedColor");
        travelMenuLabelUnselectedColorProperty = serializedObject.FindProperty("travelMenuLabelUnselectedColor");
        travelMenuRootSizeProperty = serializedObject.FindProperty("travelMenuRootSize");
        travelMenuListSizeProperty = serializedObject.FindProperty("travelMenuListSize");
        travelMenuCloseButtonSizeProperty = serializedObject.FindProperty("travelMenuCloseButtonSize");
        travelMenuCloseButtonOffsetProperty = serializedObject.FindProperty("travelMenuCloseButtonOffset");
        travelMenuItemHeightProperty = serializedObject.FindProperty("travelMenuItemHeight");
        travelMenuItemSpacingProperty = serializedObject.FindProperty("travelMenuItemSpacing");
        travelMenuTitleFontSizeProperty = serializedObject.FindProperty("travelMenuTitleFontSize");
        travelMenuHintFontSizeProperty = serializedObject.FindProperty("travelMenuHintFontSize");
        travelMenuItemFontSizeProperty = serializedObject.FindProperty("travelMenuItemFontSize");
        travelMenuCloseFontSizeProperty = serializedObject.FindProperty("travelMenuCloseFontSize");
        flameLoopSfxKeyProperty = serializedObject.FindProperty("flameLoopSfxKey");
        flameLoopVolumeProperty = serializedObject.FindProperty("flameLoopVolume");
        flameLoopMinDistanceProperty = serializedObject.FindProperty("flameLoopMinDistance");
        flameLoopMaxDistanceProperty = serializedObject.FindProperty("flameLoopMaxDistance");
        flameRendererProperty = serializedObject.FindProperty("flameRenderer");
        interactionColliderProperty = serializedObject.FindProperty("interactionCollider");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox("The travel list uses the bonfire display name first. If that is empty, it falls back to the object name.", MessageType.Info);

        DrawScriptField();
        DrawActivationSection();
        DrawCheckpointSection();
        DrawFlameAudioSection();
        DrawAnimationSection();
        DrawPromptSection();
        DrawTravelMenuSection();
        DrawReferenceSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        MonoScript script = MonoScript.FromMonoBehaviour((Bonfire)target);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", script, typeof(MonoScript), false);
        }
    }

    private void DrawCheckpointSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Checkpoint", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bonfireIdProperty, new GUIContent("Bonfire ID"));
        EditorGUILayout.PropertyField(bonfireDisplayNameProperty, new GUIContent("Travel Display Name"));
        EditorGUILayout.PropertyField(restorePlayerOnRestProperty, new GUIContent("Restore Player On Rest"));
        EditorGUILayout.PropertyField(reloadSceneOnRestProperty, new GUIContent("Reload Scene On Rest"));
        EditorGUILayout.PropertyField(restDelayProperty, new GUIContent("Rest Delay"));
        EditorGUILayout.PropertyField(startLitProperty, new GUIContent("Start Lit"));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Object Name As Display Name"))
            {
                ApplyObjectNameToDisplayName();
            }

            if (GUILayout.Button("Clear Display Name"))
            {
                SetDisplayName(string.Empty);
            }
        }
    }

    private void DrawActivationSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Activation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bonfireActivatedProperty, new GUIContent("Bonfire Activated"));
    }

    private void DrawAnimationSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameFramesProperty, new GUIContent("Flame Frames"), true);
        EditorGUILayout.PropertyField(framesPerSecondProperty, new GUIContent("Frames Per Second"));
        EditorGUILayout.PropertyField(loopAnimationProperty, new GUIContent("Loop Animation"));
        EditorGUILayout.PropertyField(autoLoadDefaultFramesProperty, new GUIContent("Auto Load Default Frames"));
        EditorGUILayout.PropertyField(unlitTintProperty, new GUIContent("Unlit Tint"));
        EditorGUILayout.PropertyField(litTintProperty, new GUIContent("Lit Tint"));
        EditorGUILayout.PropertyField(ignitePulseScaleProperty, new GUIContent("Ignite Pulse Scale"));
        EditorGUILayout.PropertyField(ignitePulseDurationProperty, new GUIContent("Ignite Pulse Duration"));
    }

    private void DrawFlameAudioSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Flame Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameLoopSfxKeyProperty, new GUIContent("Flame Loop Sfx Key"));
        EditorGUILayout.PropertyField(flameLoopVolumeProperty, new GUIContent("Flame Loop Volume"));
        EditorGUILayout.PropertyField(flameLoopMinDistanceProperty, new GUIContent("Flame Loop Min Distance"));
        EditorGUILayout.PropertyField(flameLoopMaxDistanceProperty, new GUIContent("Flame Loop Max Distance"));

        EditorGUILayout.Space(4f);
        using (new EditorGUI.DisabledScope(targets == null || targets.Length == 0))
        {
            if (GUILayout.Button("Sync Flame Audio To All Bonfires"))
            {
                serializedObject.ApplyModifiedProperties();
                SyncFlameAudioToAllBonfires();
                serializedObject.Update();
            }
        }
    }

    private void DrawPromptSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Interaction Prompt", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(promptFontProperty, new GUIContent("Prompt Font"));
        EditorGUILayout.PropertyField(restPromptProperty, new GUIContent("Rest Prompt"));
        EditorGUILayout.PropertyField(travelPromptProperty, new GUIContent("Travel Prompt"));
        EditorGUILayout.PropertyField(ignitePromptProperty, new GUIContent("Ignite Prompt"));
        EditorGUILayout.PropertyField(promptCursorSymbolProperty, new GUIContent("Cursor Symbol"));
        EditorGUILayout.PropertyField(restPromptIconProperty, new GUIContent("Rest Icon"));
        EditorGUILayout.PropertyField(travelPromptIconProperty, new GUIContent("Travel Icon"));
        EditorGUILayout.PropertyField(ignitePromptIconProperty, new GUIContent("Ignite Icon"));
        EditorGUILayout.PropertyField(promptIconSizeProperty, new GUIContent("Icon Size"));
        EditorGUILayout.PropertyField(promptRowHeightProperty, new GUIContent("Row Height"));
        EditorGUILayout.PropertyField(promptRowSpacingProperty, new GUIContent("Row Spacing"));
        EditorGUILayout.PropertyField(promptRowPaddingXProperty, new GUIContent("Row Padding X"));
        EditorGUILayout.PropertyField(promptRowPaddingYProperty, new GUIContent("Row Padding Y"));
        EditorGUILayout.PropertyField(promptRowBackgroundProperty, new GUIContent("Row Background"));
        EditorGUILayout.PropertyField(promptRowSelectedBackgroundProperty, new GUIContent("Selected Row Background"));
        EditorGUILayout.PropertyField(playerSortingOrderOffsetProperty, new GUIContent("Player Sorting Order Offset"));
        EditorGUILayout.PropertyField(promptLocalOffsetProperty, new GUIContent("Prompt Local Offset"));
        EditorGUILayout.PropertyField(promptFontSizeProperty, new GUIContent("Prompt Font Size"));
        EditorGUILayout.PropertyField(promptColorProperty, new GUIContent("Prompt Color"));
        EditorGUILayout.PropertyField(selectedPromptColorProperty, new GUIContent("Selected Prompt Color"));
        EditorGUILayout.PropertyField(unselectedPromptColorProperty, new GUIContent("Unselected Prompt Color"));
    }

    private void DrawTravelMenuSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Travel Menu", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(travelMenuFontProperty, new GUIContent("Menu Font"));
        EditorGUILayout.PropertyField(travelMenuTitleProperty, new GUIContent("Title"));
        EditorGUILayout.PropertyField(travelMenuHintProperty, new GUIContent("Hint"));
        EditorGUILayout.PropertyField(travelMenuEmptyTextProperty, new GUIContent("Empty Text"));
        EditorGUILayout.PropertyField(travelMenuCloseLabelProperty, new GUIContent("Close Label"));
        EditorGUILayout.PropertyField(travelMenuCursorSymbolProperty, new GUIContent("Cursor Symbol"));
        EditorGUILayout.PropertyField(travelMenuCloseIconProperty, new GUIContent("Close Icon"));
        EditorGUILayout.PropertyField(travelMenuCloseButtonColorProperty, new GUIContent("Close Button Color"));
        EditorGUILayout.PropertyField(travelMenuBackdropColorProperty, new GUIContent("Backdrop Color"));
        EditorGUILayout.PropertyField(travelMenuPanelColorProperty, new GUIContent("Panel Color"));
        EditorGUILayout.PropertyField(travelMenuListColorProperty, new GUIContent("List Color"));
        EditorGUILayout.PropertyField(travelMenuRowColorProperty, new GUIContent("Row Color"));
        EditorGUILayout.PropertyField(travelMenuRowSelectedColorProperty, new GUIContent("Selected Row Color"));
        EditorGUILayout.PropertyField(travelMenuTitleColorProperty, new GUIContent("Title Color"));
        EditorGUILayout.PropertyField(travelMenuHintColorProperty, new GUIContent("Hint Color"));
        EditorGUILayout.PropertyField(travelMenuEmptyColorProperty, new GUIContent("Empty Color"));
        EditorGUILayout.PropertyField(travelMenuCursorColorProperty, new GUIContent("Cursor Color"));
        EditorGUILayout.PropertyField(travelMenuLabelSelectedColorProperty, new GUIContent("Selected Label Color"));
        EditorGUILayout.PropertyField(travelMenuLabelUnselectedColorProperty, new GUIContent("Unselected Label Color"));
        EditorGUILayout.PropertyField(travelMenuRootSizeProperty, new GUIContent("Panel Size"));
        EditorGUILayout.PropertyField(travelMenuListSizeProperty, new GUIContent("List Size"));
        EditorGUILayout.PropertyField(travelMenuCloseButtonSizeProperty, new GUIContent("Close Button Size"));
        EditorGUILayout.PropertyField(travelMenuCloseButtonOffsetProperty, new GUIContent("Close Button Offset"));
        EditorGUILayout.PropertyField(travelMenuItemHeightProperty, new GUIContent("Item Height"));
        EditorGUILayout.PropertyField(travelMenuItemSpacingProperty, new GUIContent("Item Spacing"));
        EditorGUILayout.PropertyField(travelMenuTitleFontSizeProperty, new GUIContent("Title Font Size"));
        EditorGUILayout.PropertyField(travelMenuHintFontSizeProperty, new GUIContent("Hint Font Size"));
        EditorGUILayout.PropertyField(travelMenuItemFontSizeProperty, new GUIContent("Item Font Size"));
        EditorGUILayout.PropertyField(travelMenuCloseFontSizeProperty, new GUIContent("Close Font Size"));
    }

    private void DrawReferenceSection()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(flameRendererProperty, new GUIContent("Flame Renderer"));
        EditorGUILayout.PropertyField(interactionColliderProperty, new GUIContent("Interaction Collider"));
    }

    private void ApplyObjectNameToDisplayName()
    {
        foreach (Object obj in targets)
        {
            Bonfire bonfire = obj as Bonfire;
            if (bonfire == null)
            {
                continue;
            }

            SerializedObject serializedBonfire = new SerializedObject(bonfire);
            SerializedProperty displayNameProperty = serializedBonfire.FindProperty("bonfireDisplayName");
            if (displayNameProperty != null)
            {
                displayNameProperty.stringValue = bonfire.gameObject.name;
                serializedBonfire.ApplyModifiedProperties();
                EditorUtility.SetDirty(bonfire);
            }
        }
    }

    private void SetDisplayName(string value)
    {
        foreach (Object obj in targets)
        {
            Bonfire bonfire = obj as Bonfire;
            if (bonfire == null)
            {
                continue;
            }

            SerializedObject serializedBonfire = new SerializedObject(bonfire);
            SerializedProperty displayNameProperty = serializedBonfire.FindProperty("bonfireDisplayName");
            if (displayNameProperty != null)
            {
                displayNameProperty.stringValue = value;
                serializedBonfire.ApplyModifiedProperties();
                EditorUtility.SetDirty(bonfire);
            }
        }
    }

    private void SyncFlameAudioToAllBonfires()
    {
        Bonfire source = target as Bonfire;
        if (source == null)
        {
            return;
        }

        Bonfire[] bonfires = Object.FindObjectsByType<Bonfire>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Collections.Generic.List<Bonfire> changedBonfires = new System.Collections.Generic.List<Bonfire>();

        for (int i = 0; i < bonfires.Length; i++)
        {
            Bonfire bonfire = bonfires[i];
            if (bonfire == null || bonfire == source)
            {
                continue;
            }

            changedBonfires.Add(bonfire);
        }

        if (changedBonfires.Count == 0)
        {
            return;
        }

        Undo.RecordObjects(changedBonfires.ToArray(), "Sync Bonfire Flame Audio");

        for (int i = 0; i < changedBonfires.Count; i++)
        {
            Bonfire bonfire = changedBonfires[i];
            bonfire.CopyFlameAudioSettingsFrom(source);
            EditorUtility.SetDirty(bonfire);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bonfire);

            if (bonfire.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(bonfire.gameObject.scene);
            }
        }
    }
}
