using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(ArenaBossEncounterController))]
[CanEditMultipleObjects]
public class ArenaBossEncounterControllerEditor : Editor
{
    private const string GroundLayerName = "Ground";
    private const string AbyssPowerRootName = "AbyssPower";
    private const double PreviewCleanupFallbackSeconds = 4.0;

    private static int previewBackgroundCount;
    private static int previewAbyssPowerCount;
    private static readonly List<PreviewCleanupEntry> previewCleanupEntries = new List<PreviewCleanupEntry>();
    private static readonly List<GiantFireballPreviewEntry> giantFireballPreviewEntries = new List<GiantFireballPreviewEntry>();

    private struct PreviewCleanupEntry
    {
        public Object ObjectReference;
        public double ExpireAt;
    }

    private struct GiantFireballPreviewEntry
    {
        public Enemy_AbyssMageFireball Projectile;
        public double LastUpdateTime;
    }

    static ArenaBossEncounterControllerEditor()
    {
        EditorApplication.update += UpdatePreviewCleanup;
        EditorApplication.update += UpdateGiantFireballPreviews;
    }
    private SerializedProperty bossEnemyProperty;
    private SerializedProperty bossPrefabProperty;
    private SerializedProperty bossPhaseCountProperty;
    private SerializedProperty phase2HealthThresholdProperty;
    private SerializedProperty phase3HealthThresholdProperty;
    private SerializedProperty phase4HealthThresholdProperty;
    private SerializedProperty bossPhaseStatesProperty;
    private SerializedProperty bossFloatingPlatformProperty;
    private SerializedProperty giantFireballExplosionRadiusProperty;
    private SerializedProperty abyssFiresProperty;
    private SerializedProperty abyssFireRevealDelayProperty;
    private SerializedProperty abyssPowersProperty;
    private SerializedProperty abyssMageSkillPointPacePresetProperty;
    private SerializedProperty abyssMageMeleeSkillPointChanceProperty;
    private SerializedProperty abyssMageFireballSummonSkillPointChanceProperty;
    private SerializedProperty abyssMageFireballSummonSkillPointCooldownProperty;
    private SerializedProperty abyssMageFireballPlayerInteractionSkillPointCooldownProperty;
    private SerializedProperty abyssMagePassiveSkillPointInitialChanceProperty;
    private SerializedProperty abyssMagePassiveSkillPointChanceIncrementProperty;
    private SerializedProperty abyssMagePassiveSkillPointCheckIntervalProperty;
    private SerializedProperty giantHorizontalWallClearanceProperty;
    private SerializedProperty giantFireballColliderRadiusMultiplierProperty;
    private SerializedProperty bossEnhancedSkill3CastPointProperty;
    private SerializedProperty bossSkill5CastPointProperty;
    private SerializedProperty normalSkill3CastsBeforeEnhancedProperty;
    private SerializedProperty enhancedSkill3GiantFireballCountProperty;
    private SerializedProperty enhancedSkill3FireballIntervalProperty;
    private SerializedProperty enhancedSkill3PlatformRiseStartDelayProperty;
    private SerializedProperty enhancedSkill3PlatformRiseCountProperty;
    private SerializedProperty enhancedSkill3PlatformRiseIntervalProperty;
    private SerializedProperty skillPointBackgroundsProperty;
    private SerializedProperty bossHealthBarProperty;
    private SerializedProperty bossTargetProperty;
    private SerializedProperty bossCameraCenterXOffsetProperty;
    private SerializedProperty bossIntroMoveDistanceProperty;
    private SerializedProperty bossIntroMoveSpeedMultiplierProperty;
    private SerializedProperty cameraMoveToBossDurationProperty;
    private SerializedProperty cameraMoveBackToPlayerDurationProperty;
    private SerializedProperty cameraReturnToOriginalDurationProperty;
    private SerializedProperty cameraReturnToOriginalSmoothDurationProperty;
    private SerializedProperty cameraReturnToOriginalCurveProperty;
    private SerializedProperty doorsProperty;
    private SerializedProperty lockDoorsWhenEncounterStartsProperty;
    private SerializedProperty playerProperty;
    private SerializedProperty startOnlyOnceProperty;
    private SerializedProperty playerTagProperty;
    private SerializedProperty clearConfirmDelayProperty;

    private void OnEnable()
    {
        bossEnemyProperty = serializedObject.FindProperty("bossEnemy");
        bossPrefabProperty = serializedObject.FindProperty("bossPrefab");
        bossPhaseCountProperty = serializedObject.FindProperty("bossPhaseCount");
        phase2HealthThresholdProperty = serializedObject.FindProperty("phase2HealthThreshold");
        phase3HealthThresholdProperty = serializedObject.FindProperty("phase3HealthThreshold");
        phase4HealthThresholdProperty = serializedObject.FindProperty("phase4HealthThreshold");
        bossPhaseStatesProperty = serializedObject.FindProperty("bossPhaseStates");
        bossFloatingPlatformProperty = serializedObject.FindProperty("bossFloatingPlatform");
        giantFireballExplosionRadiusProperty = serializedObject.FindProperty("giantFireballExplosionRadius");
        abyssFiresProperty = serializedObject.FindProperty("abyssFires");
        abyssFireRevealDelayProperty = serializedObject.FindProperty("abyssFireRevealDelay");
        abyssPowersProperty = serializedObject.FindProperty("abyssPowers");
        abyssMageSkillPointPacePresetProperty = serializedObject.FindProperty("abyssMageSkillPointPacePreset");
        abyssMageMeleeSkillPointChanceProperty = serializedObject.FindProperty("abyssMageMeleeSkillPointChance");
        abyssMageFireballSummonSkillPointChanceProperty = serializedObject.FindProperty("abyssMageFireballSummonSkillPointChance");
        abyssMageFireballSummonSkillPointCooldownProperty = serializedObject.FindProperty("abyssMageFireballSummonSkillPointCooldown");
        abyssMageFireballPlayerInteractionSkillPointCooldownProperty = serializedObject.FindProperty("abyssMageFireballPlayerInteractionSkillPointCooldown");
        abyssMagePassiveSkillPointInitialChanceProperty = serializedObject.FindProperty("abyssMagePassiveSkillPointInitialChance");
        abyssMagePassiveSkillPointChanceIncrementProperty = serializedObject.FindProperty("abyssMagePassiveSkillPointChanceIncrement");
        abyssMagePassiveSkillPointCheckIntervalProperty = serializedObject.FindProperty("abyssMagePassiveSkillPointCheckInterval");
        giantHorizontalWallClearanceProperty = serializedObject.FindProperty("giantHorizontalWallClearance");
        giantFireballColliderRadiusMultiplierProperty = serializedObject.FindProperty("giantFireballColliderRadiusMultiplier");
        bossEnhancedSkill3CastPointProperty = serializedObject.FindProperty("bossEnhancedSkill3CastPoint");
        bossSkill5CastPointProperty = serializedObject.FindProperty("bossSkill5CastPoint");
        normalSkill3CastsBeforeEnhancedProperty = serializedObject.FindProperty("normalSkill3CastsBeforeEnhanced");
        enhancedSkill3GiantFireballCountProperty = serializedObject.FindProperty("enhancedSkill3GiantFireballCount");
        enhancedSkill3FireballIntervalProperty = serializedObject.FindProperty("enhancedSkill3FireballInterval");
        enhancedSkill3PlatformRiseStartDelayProperty = serializedObject.FindProperty("enhancedSkill3PlatformRiseStartDelay");
        enhancedSkill3PlatformRiseCountProperty = serializedObject.FindProperty("enhancedSkill3PlatformRiseCount");
        enhancedSkill3PlatformRiseIntervalProperty = serializedObject.FindProperty("enhancedSkill3PlatformRiseInterval");
        skillPointBackgroundsProperty = serializedObject.FindProperty("skillPointBackgrounds");
        bossHealthBarProperty = serializedObject.FindProperty("bossHealthBar");
        bossTargetProperty = serializedObject.FindProperty("bossTarget");
        bossCameraCenterXOffsetProperty = serializedObject.FindProperty("bossCameraCenterXOffset");
        bossIntroMoveDistanceProperty = serializedObject.FindProperty("bossIntroMoveDistance");
        bossIntroMoveSpeedMultiplierProperty = serializedObject.FindProperty("bossIntroMoveSpeedMultiplier");
        cameraMoveToBossDurationProperty = serializedObject.FindProperty("cameraMoveToBossDuration");
        cameraMoveBackToPlayerDurationProperty = serializedObject.FindProperty("cameraMoveBackToPlayerDuration");
        cameraReturnToOriginalDurationProperty = serializedObject.FindProperty("cameraReturnToOriginalDuration");
        cameraReturnToOriginalSmoothDurationProperty = serializedObject.FindProperty("cameraReturnToOriginalSmoothDuration");
        cameraReturnToOriginalCurveProperty = serializedObject.FindProperty("cameraReturnToOriginalCurve");
        doorsProperty = serializedObject.FindProperty("doors");
        lockDoorsWhenEncounterStartsProperty = serializedObject.FindProperty("lockDoorsWhenEncounterStarts");
        playerProperty = serializedObject.FindProperty("player");
        startOnlyOnceProperty = serializedObject.FindProperty("startOnlyOnce");
        playerTagProperty = serializedObject.FindProperty("playerTag");
        clearConfirmDelayProperty = serializedObject.FindProperty("clearConfirmDelay");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawScriptField();
        DrawRuntimeSummary();
        EditorGUILayout.Space(6f);

        DrawToolsSection();
        EditorGUILayout.Space(8f);

        DrawBossSection();
        EditorGUILayout.Space(8f);

        DrawFloatingPlatformSection();
        EditorGUILayout.Space(8f);

        DrawAbyssFireSection();
        EditorGUILayout.Space(8f);

        DrawAbyssPowerSection();
        EditorGUILayout.Space(8f);

        DrawAbyssMageSkillPointSection();
        EditorGUILayout.Space(8f);

        DrawSkillPointBackgroundSection();
        EditorGUILayout.Space(8f);

        DrawUiSection();
        EditorGUILayout.Space(8f);

        DrawCameraSection();
        EditorGUILayout.Space(8f);

        DrawDoorsSection();
        EditorGUILayout.Space(8f);

        DrawTriggerSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawScriptField()
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }
    }

    private void DrawRuntimeSummary()
    {
        ArenaBossEncounterController controller = (ArenaBossEncounterController)target;
        Enemy bossEnemy = bossEnemyProperty != null ? bossEnemyProperty.objectReferenceValue as Enemy : null;
        int doorCount = doorsProperty != null ? doorsProperty.arraySize : 0;
        int abyssFireCount = abyssFiresProperty != null ? abyssFiresProperty.arraySize : 0;
        int backgroundCount = skillPointBackgroundsProperty != null ? skillPointBackgroundsProperty.arraySize : 0;

        StringBuilder message = new StringBuilder();
        message.AppendLine(bossEnemy != null ? $"Boss: {bossEnemy.name}" : "Boss: Missing");
        message.AppendLine($"Boss Phases: {(bossPhaseCountProperty != null ? Mathf.Clamp(bossPhaseCountProperty.intValue, 1, 4) : 1)}");
        message.AppendLine($"Doors: {doorCount}");
        message.AppendLine($"Abyss Fires: {abyssFireCount}/6");
        message.AppendLine($"Skill Backgrounds: {backgroundCount}/6");
        message.AppendLine($"Boss Floating Platform: {(bossFloatingPlatformProperty != null && bossFloatingPlatformProperty.objectReferenceValue != null ? "Assigned" : "Missing")}");
        message.AppendLine($"Abyss Mage Pace: {(controller != null ? controller.AbyssMageSkillPointPace.ToString() : "N/A")}");
        message.AppendLine(controller != null ? $"Current Boss Phase: {controller.CurrentBossPhase}" : "Current Boss Phase: N/A");
        message.AppendLine(controller != null ? $"Current Skill Points: {controller.CurrentSkillPoints}" : "Current Skill Points: N/A");
        message.AppendLine(controller != null ? $"Active Enemy: {controller.ActiveEnemySummary}" : "Active Enemy: N/A");
        message.AppendLine($"Trigger Status: {(Application.isPlaying ? "Playing" : "Edit Mode")}");

        MessageType messageType = bossEnemy != null ? MessageType.Info : MessageType.Warning;
        EditorGUILayout.HelpBox(message.ToString(), messageType);

    }

    private void DrawToolsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Utility actions for repairing and resetting the arena setup. These do not change the scripted tuning values.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset Boss Encounter"))
            {
                ArenaBossEncounterController controller = target as ArenaBossEncounterController;
                if (controller != null)
                {
                    Undo.RecordObject(controller, "Reset Boss Encounter");
                    controller.ResetEncounter();
                    EditorUtility.SetDirty(controller);
                }
            }

            if (GUILayout.Button("Create / Repair AbyssPower Group"))
            {
                CreateOrRepairAbyssPowerGroup();
            }

            if (GUILayout.Button("Create / Repair Boss References"))
            {
                CreateOrRepairBossReferences();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBossSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Boss Encounter", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(giantFireballExplosionRadiusProperty, new GUIContent("Giant Fireball Explosion Radius"));
        EditorGUILayout.PropertyField(bossEnemyProperty);
        EditorGUILayout.PropertyField(bossPrefabProperty);
        EditorGUILayout.PropertyField(bossPhaseCountProperty);
        EditorGUILayout.PropertyField(phase2HealthThresholdProperty);
        EditorGUILayout.PropertyField(phase3HealthThresholdProperty);
        EditorGUILayout.PropertyField(phase4HealthThresholdProperty);
        EditorGUILayout.PropertyField(bossPhaseStatesProperty, true);
        EditorGUILayout.HelpBox(
            "Boss phases are limited to 4. Each phase can activate/deactivate objects or call platform states such as Hidden / Platform1..Platform6 / AllVisible.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Phase 1"))
            {
                PreviewBossPhaseState(1);
            }

            if (GUILayout.Button("Preview Phase 2"))
            {
                PreviewBossPhaseState(2);
            }

            if (GUILayout.Button("Preview Phase 3"))
            {
                PreviewBossPhaseState(3);
            }

            if (GUILayout.Button("Preview Phase 4"))
            {
                PreviewBossPhaseState(4);
            }
        }

        EditorGUILayout.HelpBox(
            "Preview buttons apply the configured phase actions directly in the current scene. Use Undo to revert the preview changes.",
            MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawFloatingPlatformSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Boss Floating Platform", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign the boss-only floating platform here. This is separate from the wave arena floating platform and should live under ArenaBossEncounter.",
            MessageType.Info);

        if (bossFloatingPlatformProperty != null)
        {
            EditorGUILayout.PropertyField(bossFloatingPlatformProperty);
        }
        else
        {
            EditorGUILayout.HelpBox("bossFloatingPlatform property is missing.", MessageType.Warning);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto Find In Encounter"))
            {
                FillBossFloatingPlatformFromEncounter();
            }

            if (GUILayout.Button("Select Platform"))
            {
                SelectBossFloatingPlatform();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAbyssFireSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Abyss Fire", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign exactly 6 AbyssFire objects in left-to-right order. They will stay hidden until the boss encounter starts, then appear one by one.",
            MessageType.Info);

        if (abyssFiresProperty != null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill From Selection (Sort X)"))
                {
                    FillAbyssFiresFromSelection();
                }

                if (GUILayout.Button("Auto Find In Scene"))
                {
                    FillAbyssFiresFromScene();
                }

                if (GUILayout.Button("Auto Find In Encounter"))
                {
                    FillAbyssFiresFromEncounter();
                }

                if (GUILayout.Button("Clear"))
                {
                    ClearArray(abyssFiresProperty);
                }
            }

            while (abyssFiresProperty.arraySize < 6)
            {
                abyssFiresProperty.InsertArrayElementAtIndex(abyssFiresProperty.arraySize);
            }

            while (abyssFiresProperty.arraySize > 6)
            {
                abyssFiresProperty.DeleteArrayElementAtIndex(abyssFiresProperty.arraySize - 1);
            }

            for (int i = 0; i < abyssFiresProperty.arraySize; i++)
            {
                SerializedProperty element = abyssFiresProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element, new GUIContent($"Abyss Fire {i + 1}"));
            }
        }

        EditorGUILayout.PropertyField(abyssFireRevealDelayProperty, new GUIContent("Reveal Delay"));
        EditorGUILayout.EndVertical();
    }

    private void DrawSkillPointBackgroundSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Skill Point Backgrounds", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign exactly 6 background GameObjects in left-to-right order. The recommended structure is BossBackgrounds -> Background1..Background6, matching the arena platform style.",
            MessageType.Info);

        if (skillPointBackgroundsProperty != null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill From Selection (Sort X)"))
                {
                    FillSkillPointBackgroundsFromSelection();
                }

                if (GUILayout.Button("Auto Find In Scene"))
                {
                    FillSkillPointBackgroundsFromScene();
                }

                if (GUILayout.Button("Auto Find In Encounter"))
                {
                    FillSkillPointBackgroundsFromEncounter();
                }

                if (GUILayout.Button("Clear"))
                {
                    ClearArray(skillPointBackgroundsProperty);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate 6 From Background_2"))
                {
                    GenerateSkillPointBackgroundVariants();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Show All"))
                {
                    previewBackgroundCount = 6;
                    SetSkillPointBackgroundPreviewCount(6);
                }

                if (GUILayout.Button("Preview Hide All"))
                {
                    previewBackgroundCount = 0;
                    SetSkillPointBackgroundPreviewCount(0);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                previewBackgroundCount = EditorGUILayout.IntSlider("Preview Count", previewBackgroundCount, 0, 6);
                if (GUILayout.Button("Apply", GUILayout.Width(64f)))
                {
                    SetSkillPointBackgroundPreviewCount(previewBackgroundCount);
                }
            }

            while (skillPointBackgroundsProperty.arraySize < 6)
            {
                int index = skillPointBackgroundsProperty.arraySize;
                skillPointBackgroundsProperty.InsertArrayElementAtIndex(index);
                skillPointBackgroundsProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            }

            while (skillPointBackgroundsProperty.arraySize > 6)
            {
                skillPointBackgroundsProperty.DeleteArrayElementAtIndex(skillPointBackgroundsProperty.arraySize - 1);
            }

            for (int i = 0; i < skillPointBackgroundsProperty.arraySize; i++)
            {
                SerializedProperty element = skillPointBackgroundsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element, new GUIContent($"Background {i + 1}"));
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAbyssPowerSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Abyss Power", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign exactly 6 AbyssPower scene objects under ArenaBossEncounter -> AbyssPower. Each skill point reveal will randomly activate one inactive object.",
            MessageType.Info);

        if (abyssPowersProperty != null)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Fill From Selection (Sort X)"))
                {
                    FillAbyssPowersFromSelection();
                }

                if (GUILayout.Button("Auto Find In Scene"))
                {
                    FillAbyssPowersFromScene();
                }

                if (GUILayout.Button("Auto Find In Encounter"))
                {
                    FillAbyssPowersFromEncounter();
                }

                if (GUILayout.Button("Clear"))
                {
                    ClearArray(abyssPowersProperty);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preview Show All"))
                {
                    previewAbyssPowerCount = 6;
                    SetAbyssPowerPreviewCount(6);
                }

                if (GUILayout.Button("Preview Hide All"))
                {
                    previewAbyssPowerCount = 0;
                    SetAbyssPowerPreviewCount(0);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                previewAbyssPowerCount = EditorGUILayout.IntSlider("Preview Count", previewAbyssPowerCount, 0, 6);
                if (GUILayout.Button("Apply", GUILayout.Width(64f)))
                {
                    SetAbyssPowerPreviewCount(previewAbyssPowerCount);
                }
            }

            while (abyssPowersProperty.arraySize < 6)
            {
                int index = abyssPowersProperty.arraySize;
                abyssPowersProperty.InsertArrayElementAtIndex(index);
                abyssPowersProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            }

            while (abyssPowersProperty.arraySize > 6)
            {
                abyssPowersProperty.DeleteArrayElementAtIndex(abyssPowersProperty.arraySize - 1);
            }

            for (int i = 0; i < abyssPowersProperty.arraySize; i++)
            {
                SerializedProperty element = abyssPowersProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(element, new GUIContent($"Abyss Power {i + 1}"));
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawAbyssMageSkillPointSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Abyss Mage Skill Point", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "These values tune the Abyss Mage skill point economy. Melee, fireball summon, fireball hit/block, and passive ramp-up are all controlled here.",
            MessageType.Info);

        EditorGUILayout.PropertyField(abyssMageSkillPointPacePresetProperty, new GUIContent("Battle Pace Preset"));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Apply Conservative"))
            {
                ApplyAbyssMageSkillPointPreset(ArenaBossEncounterController.AbyssMageSkillPointPacePreset.Conservative);
            }

            if (GUILayout.Button("Apply Standard"))
            {
                ApplyAbyssMageSkillPointPreset(ArenaBossEncounterController.AbyssMageSkillPointPacePreset.Standard);
            }

            if (GUILayout.Button("Apply Aggressive"))
            {
                ApplyAbyssMageSkillPointPreset(ArenaBossEncounterController.AbyssMageSkillPointPacePreset.Aggressive);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Skill3 Giant Fireball"))
            {
                if (Application.isPlaying)
                {
                    ArenaBossEncounterController controller = target as ArenaBossEncounterController;
                    if (controller != null)
                    {
                        controller.PreviewAbyssMageGiantSpellCast(GetGiantFireballExplosionRadius());
                    }
                }
                else
                {
                    PreviewAbyssMageGiantSpellCastInEditMode();
                }
            }
        }

        EditorGUILayout.HelpBox(
            "Preview Skill3 Giant Fireball works in Play Mode and Edit Mode. In Edit Mode it auto-deletes after a short delay.",
            MessageType.Info);

        EditorGUILayout.PropertyField(abyssMageMeleeSkillPointChanceProperty, new GUIContent("Melee Skill Point Chance"));
        EditorGUILayout.PropertyField(abyssMageFireballSummonSkillPointChanceProperty, new GUIContent("Fireball Summon Skill Point Chance"));
        EditorGUILayout.PropertyField(abyssMageFireballSummonSkillPointCooldownProperty, new GUIContent("Fireball Summon Skill Point Cooldown"));
        EditorGUILayout.PropertyField(abyssMageFireballPlayerInteractionSkillPointCooldownProperty, new GUIContent("Fireball Player Interaction Skill Point Cooldown"));
        EditorGUILayout.PropertyField(abyssMagePassiveSkillPointInitialChanceProperty, new GUIContent("Passive Skill Point Initial Chance"));
        EditorGUILayout.PropertyField(abyssMagePassiveSkillPointChanceIncrementProperty, new GUIContent("Passive Skill Point Chance Increment"));
        EditorGUILayout.PropertyField(abyssMagePassiveSkillPointCheckIntervalProperty, new GUIContent("Passive Skill Point Check Interval"));
        EditorGUILayout.PropertyField(giantHorizontalWallClearanceProperty, new GUIContent("Giant Horizontal Wall Clearance"));
        EditorGUILayout.PropertyField(giantFireballColliderRadiusMultiplierProperty, new GUIContent("Giant Fireball Collider Radius Multiplier"));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Enhanced Skill 3", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bossEnhancedSkill3CastPointProperty, new GUIContent("Boss Enhanced Skill3 Cast Point"));
        EditorGUILayout.PropertyField(bossSkill5CastPointProperty, new GUIContent("Boss Skill5 Cast Point"));
        EditorGUILayout.PropertyField(normalSkill3CastsBeforeEnhancedProperty, new GUIContent("Normal Skill3 Casts Before Enhanced"));
        EditorGUILayout.PropertyField(enhancedSkill3GiantFireballCountProperty, new GUIContent("Enhanced Giant Fireball Count"));
        EditorGUILayout.PropertyField(enhancedSkill3FireballIntervalProperty, new GUIContent("Enhanced Fireball Interval"));
        EditorGUILayout.PropertyField(enhancedSkill3PlatformRiseStartDelayProperty, new GUIContent("Platform Rise Start Delay"));
        EditorGUILayout.PropertyField(enhancedSkill3PlatformRiseCountProperty, new GUIContent("Platform Rise Count"));
        EditorGUILayout.PropertyField(enhancedSkill3PlatformRiseIntervalProperty, new GUIContent("Platform Rise Interval"));

        EditorGUILayout.EndVertical();
    }

    private void PreviewAbyssMageGiantSpellCastInEditMode()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        bool spawnedTemporaryMage;
        Enemy_AbyssMage mage = controller != null
            ? controller.ResolvePreviewAbyssMageForSpellPreview(false, out spawnedTemporaryMage)
            : null;

        if (mage == null)
        {
            Debug.LogWarning("[AbyssMageBoss] Edit-mode preview failed because no Enemy_AbyssMage was found.", controller);
            return;
        }

        Enemy_AbyssMageFireball previewProjectile = mage.SpawnPreviewGiantAbyssFireball(null, GetGiantFireballExplosionRadius());
        if (previewProjectile == null)
        {
            Debug.LogWarning("[AbyssMageBoss] Edit-mode preview failed because the giant fireball could not be spawned.", mage);
            return;
        }

        GameObject previewObject = previewProjectile.gameObject;
        Undo.RegisterCreatedObjectUndo(previewObject, "Preview Skill3 Giant Fireball");
        RegisterPreviewCleanup(previewObject, Mathf.Max(2f, mage.GiantFireballHoverDuration + 2f));
        RegisterGiantFireballPreview(previewProjectile);
        SceneView.RepaintAll();
    }

    private float GetGiantFireballExplosionRadius()
    {
        return giantFireballExplosionRadiusProperty != null
            ? giantFireballExplosionRadiusProperty.floatValue
            : -1f;
    }

    private static void RegisterGiantFireballPreview(Enemy_AbyssMageFireball projectile)
    {
        if (projectile == null)
        {
            return;
        }

        giantFireballPreviewEntries.Add(new GiantFireballPreviewEntry
        {
            Projectile = projectile,
            LastUpdateTime = EditorApplication.timeSinceStartup
        });
    }

    private static void RegisterPreviewCleanup(Object previewObject, float lifetime)
    {
        if (previewObject == null)
        {
            return;
        }

        previewCleanupEntries.Add(new PreviewCleanupEntry
        {
            ObjectReference = previewObject,
            ExpireAt = EditorApplication.timeSinceStartup + Mathf.Max(0.1f, lifetime)
        });
    }

    private static void UpdatePreviewCleanup()
    {
        if (previewCleanupEntries.Count == 0)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        for (int i = previewCleanupEntries.Count - 1; i >= 0; i--)
        {
            PreviewCleanupEntry entry = previewCleanupEntries[i];
            if (entry.ObjectReference == null || now >= entry.ExpireAt)
            {
                if (entry.ObjectReference != null)
                {
                    Object.DestroyImmediate(entry.ObjectReference);
                    SceneView.RepaintAll();
                }

                previewCleanupEntries.RemoveAt(i);
            }
        }
    }

    private static void UpdateGiantFireballPreviews()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            giantFireballPreviewEntries.Clear();
            return;
        }

        if (giantFireballPreviewEntries.Count == 0)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        bool repainted = false;

        for (int i = giantFireballPreviewEntries.Count - 1; i >= 0; i--)
        {
            GiantFireballPreviewEntry entry = giantFireballPreviewEntries[i];
            if (entry.Projectile == null)
            {
                giantFireballPreviewEntries.RemoveAt(i);
                continue;
            }

            float deltaTime = (float)System.Math.Max(0.0, now - entry.LastUpdateTime);
            entry.LastUpdateTime = now;
            giantFireballPreviewEntries[i] = entry;

            entry.Projectile.EditorPreviewTick(deltaTime);
            repainted = true;
        }

        if (repainted)
        {
            SceneView.RepaintAll();
        }
    }

    private void ApplyAbyssMageSkillPointPreset(ArenaBossEncounterController.AbyssMageSkillPointPacePreset preset)
    {
        if (serializedObject == null)
        {
            return;
        }

        Undo.RecordObjects(targets, $"Apply Abyss Mage Skill Point Preset ({preset})");

        if (abyssMageSkillPointPacePresetProperty != null)
        {
            abyssMageSkillPointPacePresetProperty.enumValueIndex = (int)preset;
        }

        switch (preset)
        {
            case ArenaBossEncounterController.AbyssMageSkillPointPacePreset.Conservative:
                SetSkillPointPresetValues(25f, 12f, 5f, 8f, 1f, 1f, 1.25f);
                break;
            case ArenaBossEncounterController.AbyssMageSkillPointPacePreset.Aggressive:
                SetSkillPointPresetValues(50f, 30f, 3f, 4f, 2f, 3f, 1f);
                break;
            case ArenaBossEncounterController.AbyssMageSkillPointPacePreset.Standard:
            default:
                SetSkillPointPresetValues(35f, 20f, 4f, 6f, 1f, 2f, 1f);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void SetSkillPointPresetValues(float meleeChance, float summonChance, float summonCooldown, float playerInteractionCooldown, float passiveInitialChance, float passiveIncrement, float passiveInterval)
    {
        if (abyssMageMeleeSkillPointChanceProperty != null)
        {
            abyssMageMeleeSkillPointChanceProperty.floatValue = meleeChance;
        }

        if (abyssMageFireballSummonSkillPointChanceProperty != null)
        {
            abyssMageFireballSummonSkillPointChanceProperty.floatValue = summonChance;
        }

        if (abyssMageFireballSummonSkillPointCooldownProperty != null)
        {
            abyssMageFireballSummonSkillPointCooldownProperty.floatValue = summonCooldown;
        }

        if (abyssMageFireballPlayerInteractionSkillPointCooldownProperty != null)
        {
            abyssMageFireballPlayerInteractionSkillPointCooldownProperty.floatValue = playerInteractionCooldown;
        }

        if (abyssMagePassiveSkillPointInitialChanceProperty != null)
        {
            abyssMagePassiveSkillPointInitialChanceProperty.floatValue = passiveInitialChance;
        }

        if (abyssMagePassiveSkillPointChanceIncrementProperty != null)
        {
            abyssMagePassiveSkillPointChanceIncrementProperty.floatValue = passiveIncrement;
        }

        if (abyssMagePassiveSkillPointCheckIntervalProperty != null)
        {
            abyssMagePassiveSkillPointCheckIntervalProperty.floatValue = passiveInterval;
        }
    }

    private void DrawUiSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("UI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign the boss UI component here if you want to control or inspect it manually. The controller will also auto-find it at runtime.",
            MessageType.Info);
        if (bossHealthBarProperty != null)
        {
            EditorGUILayout.PropertyField(bossHealthBarProperty);
        }
        else
        {
            EditorGUILayout.HelpBox("bossHealthBar property is missing.", MessageType.Warning);
        }
        EditorGUILayout.EndVertical();
    }

    private void FillBossFloatingPlatformFromEncounter()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        Transform root = controller != null ? controller.transform.root : null;
        ArenaBossFloatingPlatformController platform = root != null ? root.GetComponentInChildren<ArenaBossFloatingPlatformController>(true) : null;
        ApplyObjectToTargets(bossFloatingPlatformProperty != null ? bossFloatingPlatformProperty.name : "bossFloatingPlatform", platform);
    }

    private void SelectBossFloatingPlatform()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        Transform root = controller != null ? controller.transform.root : null;
        ArenaBossFloatingPlatformController platform = root != null ? root.GetComponentInChildren<ArenaBossFloatingPlatformController>(true) : null;
        if (platform != null)
        {
            Selection.activeGameObject = platform.gameObject;
            EditorGUIUtility.PingObject(platform.gameObject);
        }
    }

    private void DrawCameraSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign the BossTarget child here. It is a normal child object in the hierarchy, so you can move it manually to tune the boss camera anchor and the boss spawn position.",
            MessageType.Info);
        EditorGUILayout.PropertyField(bossTargetProperty);
        EditorGUILayout.PropertyField(bossCameraCenterXOffsetProperty);
        EditorGUILayout.PropertyField(bossIntroMoveDistanceProperty);
        EditorGUILayout.PropertyField(bossIntroMoveSpeedMultiplierProperty);
        EditorGUILayout.PropertyField(cameraMoveToBossDurationProperty);
        EditorGUILayout.PropertyField(cameraMoveBackToPlayerDurationProperty);
        EditorGUILayout.PropertyField(cameraReturnToOriginalDurationProperty);
        EditorGUILayout.PropertyField(cameraReturnToOriginalSmoothDurationProperty, new GUIContent("Return To Original Duration (Smooth)"));
        EditorGUILayout.PropertyField(cameraReturnToOriginalCurveProperty, new GUIContent("Return To Original Curve"));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Create / Repair Boss Target"))
            {
                CreateOrRepairBossCameraTarget();
            }

            if (GUILayout.Button("Select Target"))
            {
                SelectBossCameraTarget();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void CreateOrRepairBossCameraTarget()
    {
        bool changed = false;
        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null || controller.transform == null)
            {
                continue;
            }

            Transform root = controller.transform.root;
            if (root == null)
            {
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Create Or Repair Boss Camera Target");

            Transform target = FindDeepChild(root, "BossTarget");
            Transform legacyTarget = FindDeepChild(root, "BossCameraTarget");
            if (target == null && legacyTarget != null)
            {
                Undo.RecordObject(legacyTarget.gameObject, "Rename BossCameraTarget To BossTarget");
                legacyTarget.name = "BossTarget";
                target = legacyTarget;
            }
            if (target == null)
            {
                GameObject targetObject = new GameObject("BossTarget");
                Undo.RegisterCreatedObjectUndo(targetObject, "Create BossTarget");
                Undo.SetTransformParent(targetObject.transform, controller.transform, "Parent BossTarget");
                target = targetObject.transform;
            }
            else if (target.parent != controller.transform)
            {
                Undo.SetTransformParent(target, controller.transform, "Parent BossTarget");
            }

            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            SerializedObject controllerSo = new SerializedObject(controller);
            if (bossTargetProperty != null)
            {
                SerializedProperty targetProperty = controllerSo.FindProperty(bossTargetProperty.name);
                if (targetProperty != null)
                {
                    targetProperty.objectReferenceValue = target;
                    controllerSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(target);
            Selection.activeTransform = target;
            EditorGUIUtility.PingObject(target.gameObject);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.SaveScene(scene);
            }
        }
    }

    private void SelectBossCameraTarget()
    {
        ArenaBossEncounterController controller = this.target as ArenaBossEncounterController;
        if (controller == null || controller.transform == null)
        {
            return;
        }

        Transform root = controller.transform.root;
        if (root == null)
        {
            return;
        }

        Transform cameraTarget = FindDeepChild(root, "BossTarget") ?? FindDeepChild(root, "BossCameraTarget");
        if (cameraTarget != null)
        {
            Selection.activeTransform = cameraTarget;
            EditorGUIUtility.PingObject(cameraTarget.gameObject);
        }
    }

    private void FillAbyssFiresFromSelection()
    {
        AbyssFire[] selectedFires = GetSelectedSceneObjectsSortedByX<AbyssFire>();
        ApplyObjectArrayToTargets(abyssFiresProperty != null ? abyssFiresProperty.name : "abyssFires", selectedFires, 6);
    }

    private void FillAbyssFiresFromScene()
    {
        AbyssFire[] fires = GetSceneObjectsSortedByX<AbyssFire>();
        ApplyObjectArrayToTargets(abyssFiresProperty != null ? abyssFiresProperty.name : "abyssFires", fires, 6);
    }

    private void FillAbyssFiresFromEncounter()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        Transform root = controller != null ? controller.transform.root : null;
        AbyssFire[] fires = GetRootObjectsSortedByX<AbyssFire>(root);
        ApplyObjectArrayToTargets(abyssFiresProperty != null ? abyssFiresProperty.name : "abyssFires", fires, 6);
    }

    private void FillAbyssPowersFromSelection()
    {
        AbyssPower[] selectedPowers = GetSelectedSceneObjectsSortedByX<AbyssPower>();
        ApplyObjectArrayToTargets(abyssPowersProperty != null ? abyssPowersProperty.name : "abyssPowers", selectedPowers, 6);
    }

    private void FillAbyssPowersFromScene()
    {
        AbyssPower[] powers = GetSceneObjectsSortedByX<AbyssPower>();
        ApplyObjectArrayToTargets(abyssPowersProperty != null ? abyssPowersProperty.name : "abyssPowers", powers, 6);
    }

    private void FillAbyssPowersFromEncounter()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        Transform root = controller != null ? controller.transform.root : null;
        AbyssPower[] powers = GetRootObjectsSortedByX<AbyssPower>(root);
        ApplyObjectArrayToTargets(abyssPowersProperty != null ? abyssPowersProperty.name : "abyssPowers", powers, 6);
    }

    private void FillSkillPointBackgroundsFromSelection()
    {
        GameObject[] selectedBackgrounds = GetSelectedSceneGameObjectsSortedByX();
        ApplyObjectArrayToTargets(skillPointBackgroundsProperty != null ? skillPointBackgroundsProperty.name : "skillPointBackgrounds", selectedBackgrounds, 6);
    }

    private void FillSkillPointBackgroundsFromScene()
    {
        GameObject[] backgrounds = GetSceneBackgroundObjectsSortedByX();
        ApplyObjectArrayToTargets(skillPointBackgroundsProperty != null ? skillPointBackgroundsProperty.name : "skillPointBackgrounds", backgrounds, 6);
    }

    private void FillSkillPointBackgroundsFromEncounter()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        Transform root = controller != null ? controller.transform.root : null;
        GameObject[] backgrounds = GetEncounterBackgroundObjectsSortedByX(root);
        ApplyObjectArrayToTargets(skillPointBackgroundsProperty != null ? skillPointBackgroundsProperty.name : "skillPointBackgrounds", backgrounds, 6);
    }

    private void GenerateSkillPointBackgroundVariants()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        if (controller == null)
        {
            return;
        }

        Transform root = controller.transform.root;
        if (root == null)
        {
            return;
        }

        Transform source = FindDeepChild(root, "Background_2");
        if (source == null)
        {
            Debug.LogWarning("Could not find Background_2 in the boss encounter hierarchy.");
            return;
        }

        Transform container = FindDeepChild(root, "BossBackgrounds");
        if (container == null)
        {
            container = FindDeepChild(root, "BossSkillBackgrounds");
        }
        if (container == null)
        {
            GameObject containerObject = new GameObject("BossBackgrounds");
            Undo.RegisterCreatedObjectUndo(containerObject, "Create BossBackgrounds");
            Undo.SetTransformParent(containerObject.transform, root, "Parent BossBackgrounds");
            container = containerObject.transform;
            container.localPosition = Vector3.zero;
            container.localRotation = Quaternion.identity;
            container.localScale = Vector3.one;
        }

        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Generate Skill Point Background Variants");

        for (int i = 0; i < 6; i++)
        {
            string variantName = $"Background{i + 1}";
            Transform existing = container.Find(variantName);
            GameObject variantObject;

            if (existing != null)
            {
                variantObject = existing.gameObject;
            }
            else
            {
                variantObject = Object.Instantiate(source.gameObject);
                Undo.RegisterCreatedObjectUndo(variantObject, $"Create {variantName}");
                Undo.SetTransformParent(variantObject.transform, container, $"Parent {variantName}");
            }

            variantObject.name = variantName;
            variantObject.transform.position = source.position;
            variantObject.transform.rotation = source.rotation;
            variantObject.transform.localScale = source.localScale;
            variantObject.SetActive(false);
        }

        FillSkillPointBackgroundsFromEncounter();
        EditorUtility.SetDirty(root.gameObject);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = container.gameObject;
        EditorGUIUtility.PingObject(container.gameObject);
    }

    private void PreviewBossPhaseState(int phase)
    {
        if (phase < 1 || phase > 4)
        {
            return;
        }

        bool changed = false;
        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null)
            {
                continue;
            }

            Transform root = controller.transform != null ? controller.transform.root : null;
            if (root != null)
            {
                Undo.RegisterFullObjectHierarchyUndo(root.gameObject, $"Preview Boss Phase {phase}");
            }

            controller.PreviewBossPhaseState(phase);
            EditorUtility.SetDirty(controller);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private void SetSkillPointBackgroundPreviewCount(int visibleCount)
    {
        bool changed = false;
        int clampedVisibleCount = Mathf.Clamp(visibleCount, 0, 6);

        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null)
            {
                continue;
            }

            Undo.RecordObject(controller, "Set Skill Point Background Preview");
            controller.PreviewSkillPointBackgroundVisibleCount(clampedVisibleCount);
            EditorUtility.SetDirty(controller);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private void SetAbyssPowerPreviewCount(int visibleCount)
    {
        bool changed = false;
        int clampedVisibleCount = Mathf.Clamp(visibleCount, 0, 6);

        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null)
            {
                continue;
            }

            Undo.RecordObject(controller, "Set Abyss Power Preview");
            controller.PreviewAbyssPowerVisibleCount(clampedVisibleCount);
            EditorUtility.SetDirty(controller);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    [MenuItem("Tools/Arena Encounter/Reset Boss Encounter")]
    private static void MenuResetBossEncounter()
    {
        if (!TryGetSelectedArenaBossEncounterController(out ArenaBossEncounterController controller))
        {
            Debug.LogWarning("[ArenaBossEncounter] No ArenaBossEncounterController found for Reset Boss Encounter.");
            return;
        }

        Undo.RecordObject(controller, "Reset Boss Encounter");
        controller.ResetEncounter();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        if (controller.gameObject.scene.IsValid())
        {
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }
    }

    [MenuItem("Tools/Arena Encounter/Create / Repair AbyssPower Group")]
    private static void MenuCreateOrRepairAbyssPowerGroup()
    {
        if (!TryGetSelectedArenaBossEncounterController(out ArenaBossEncounterController controller))
        {
            Debug.LogWarning("[ArenaBossEncounter] No ArenaBossEncounterController found for AbyssPower repair.");
            return;
        }

        CreateOrRepairAbyssPowerGroup(controller);
    }

    [MenuItem("Tools/Arena Encounter/Create / Repair Boss References")]
    private static void MenuCreateOrRepairBossReferences()
    {
        if (!TryGetSelectedArenaBossEncounterController(out ArenaBossEncounterController controller))
        {
            Debug.LogWarning("[ArenaBossEncounter] No ArenaBossEncounterController found for Boss reference repair.");
            return;
        }

        CreateOrRepairBossReferences(controller);
    }

    private static bool TryGetSelectedArenaBossEncounterController(out ArenaBossEncounterController controller)
    {
        controller = null;

        if (Selection.activeGameObject != null)
        {
            controller = Selection.activeGameObject.GetComponentInParent<ArenaBossEncounterController>(true);
        }

        if (controller != null)
        {
            return true;
        }

        controller = Object.FindObjectOfType<ArenaBossEncounterController>(true);
        return controller != null;
    }

    private void CreateOrRepairAbyssPowerGroup()
    {
        bool changed = false;
        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null || controller.transform == null)
            {
                continue;
            }

            CreateOrRepairAbyssPowerGroup(controller);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private void CreateOrRepairBossReferences()
    {
        bool changed = false;
        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null || controller.transform == null)
            {
                continue;
            }

            CreateOrRepairBossReferences(controller);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private static void CreateOrRepairBossReferences(ArenaBossEncounterController controller)
    {
        if (controller == null || controller.transform == null)
        {
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(controller.gameObject, "Create Or Repair Boss References");

        SerializedObject controllerSo = new SerializedObject(controller);
        Transform controllerRoot = controller.transform.root != null ? controller.transform.root : controller.transform;

        ArenaBossFloatingPlatformController floatingPlatform = controllerSo.FindProperty("bossFloatingPlatform")?.objectReferenceValue as ArenaBossFloatingPlatformController;
        Transform castPointParent = floatingPlatform != null ? floatingPlatform.transform : controller.transform;

        Enemy_AbyssMage bossMage = FindSceneBossMage(controller);
        if (bossMage == null)
        {
            GameObject bossPrefab = controllerSo.FindProperty("bossPrefab")?.objectReferenceValue as GameObject;

            if (bossPrefab != null)
            {
                GameObject bossObject = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, controller.gameObject.scene);
                Undo.RegisterCreatedObjectUndo(bossObject, "Create Boss Enemy");
                Undo.SetTransformParent(bossObject.transform, controller.transform, "Parent Boss Enemy");
                bossObject.name = bossPrefab.name;
                if (!Application.isPlaying)
                {
                    bossObject.SetActive(false);
                }

                bossMage = bossObject.GetComponentInChildren<Enemy_AbyssMage>(true);
                if (bossMage == null)
                {
                    bossMage = bossObject.GetComponent<Enemy_AbyssMage>();
                }
            }
        }
        else if (bossMage.transform.parent != controller.transform)
        {
            Undo.SetTransformParent(bossMage.transform, controller.transform, "Parent Boss Enemy");
        }

        if (bossMage != null)
        {
            if (!Application.isPlaying)
            {
                bossMage.gameObject.SetActive(false);
            }

            SetObjectReference(controllerSo, "bossEnemy", bossMage);
            EditorUtility.SetDirty(bossMage);
        }

        Transform enhancedCastPoint = FindDeepChild(controllerRoot, "BossEnhancedSkill3CastPoint");
        if (enhancedCastPoint == null)
        {
            enhancedCastPoint = EnsureEmptyChild(castPointParent, "BossEnhancedSkill3CastPoint");
        }
        else if (enhancedCastPoint.parent != castPointParent)
        {
            Undo.SetTransformParent(enhancedCastPoint, castPointParent, "Parent BossEnhancedSkill3CastPoint");
        }

        if (enhancedCastPoint != null)
        {
            enhancedCastPoint.localPosition = Vector3.zero;
            enhancedCastPoint.localRotation = Quaternion.identity;
            enhancedCastPoint.localScale = Vector3.one;
            SetObjectReference(controllerSo, "bossEnhancedSkill3CastPoint", enhancedCastPoint);
            EditorUtility.SetDirty(enhancedCastPoint);
        }

        Transform skill5CastPoint = FindDeepChild(controllerRoot, "BossSkill5CastPoint");
        if (skill5CastPoint == null)
        {
            skill5CastPoint = EnsureEmptyChild(castPointParent, "BossSkill5CastPoint");
        }
        else if (skill5CastPoint.parent != castPointParent)
        {
            Undo.SetTransformParent(skill5CastPoint, castPointParent, "Parent BossSkill5CastPoint");
        }

        if (skill5CastPoint != null)
        {
            skill5CastPoint.localPosition = Vector3.zero;
            skill5CastPoint.localRotation = Quaternion.identity;
            skill5CastPoint.localScale = Vector3.one;
            SetObjectReference(controllerSo, "bossSkill5CastPoint", skill5CastPoint);
            EditorUtility.SetDirty(skill5CastPoint);
        }

        controllerSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);

        if (bossMage != null)
        {
            Selection.activeGameObject = bossMage.gameObject;
            EditorGUIUtility.PingObject(bossMage.gameObject);
        }
    }

    private static void CreateOrRepairAbyssPowerGroup(ArenaBossEncounterController controller)
    {
        if (controller == null || controller.transform == null)
        {
            return;
        }

        Transform root = controller.transform;
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Create Or Repair AbyssPower Group");

        GameObject group = FindDeepChild(root, AbyssPowerRootName)?.gameObject;
        bool groupCreated = false;
        if (group == null)
        {
            group = new GameObject(AbyssPowerRootName);
            Undo.RegisterCreatedObjectUndo(group, "Create AbyssPower Group");
            Undo.SetTransformParent(group.transform, root, "Parent AbyssPower Group");
            groupCreated = true;
        }

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
        {
            group.layer = ignoreRaycastLayer;
        }

        if (groupCreated)
        {
            group.transform.localPosition = Vector3.zero;
            group.transform.localRotation = Quaternion.identity;
            group.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < 6; i++)
        {
            string childName = $"{AbyssPowerRootName}_{i + 1}";
            Transform child = group.transform.Find(childName);
            GameObject childObject;
            bool childCreated = false;
            if (child != null)
            {
                childObject = child.gameObject;
            }
            else
            {
                childObject = new GameObject(childName);
                Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
                Undo.SetTransformParent(childObject.transform, group.transform, $"Parent {childName}");
                childCreated = true;
            }

            childObject.layer = group.layer;
            if (childCreated)
            {
                childObject.transform.localPosition = new Vector3(-3f + i * 1.2f, 1.8f, 0f);
                childObject.transform.localRotation = Quaternion.identity;
                childObject.transform.localScale = Vector3.one;
            }

            if (childObject.GetComponent<AbyssPower>() == null)
            {
                Undo.AddComponent<AbyssPower>(childObject);
            }

            Collider2D[] colliders = childObject.GetComponentsInChildren<Collider2D>(true);
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider2D collider = colliders[colliderIndex];
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }

            Rigidbody2D[] bodies = childObject.GetComponentsInChildren<Rigidbody2D>(true);
            for (int bodyIndex = 0; bodyIndex < bodies.Length; bodyIndex++)
            {
                Rigidbody2D body = bodies[bodyIndex];
                if (body != null)
                {
                    Undo.DestroyObjectImmediate(body);
                }
            }

            if (childObject.GetComponent<Tilemap>() == null)
            {
                Undo.AddComponent<Tilemap>(childObject);
            }

            TilemapRenderer renderer = childObject.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<TilemapRenderer>(childObject);
            }

            renderer.sortingLayerName = "Background";
            renderer.sortingOrder = 12;
            childObject.SetActive(true);
        }

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
    }

    private void ApplyObjectArrayToTargets(string propertyName, Object[] objects, int desiredSize)
    {
        if (objects == null)
        {
            objects = new Object[0];
        }

        int count = Mathf.Min(desiredSize, objects.Length);
        foreach (Object targetObject in targets)
        {
            if (targetObject == null)
            {
                continue;
            }

            SerializedObject so = new SerializedObject(targetObject);
            SerializedProperty arrayProperty = so.FindProperty(propertyName);
            if (arrayProperty == null)
            {
                continue;
            }

            while (arrayProperty.arraySize < desiredSize)
            {
                int index = arrayProperty.arraySize;
                arrayProperty.InsertArrayElementAtIndex(index);
                arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = null;
            }

            while (arrayProperty.arraySize > desiredSize)
            {
                arrayProperty.DeleteArrayElementAtIndex(arrayProperty.arraySize - 1);
            }

            for (int i = 0; i < desiredSize; i++)
            {
                SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
                element.objectReferenceValue = i < count ? objects[i] : null;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(targetObject);
        }

        serializedObject.Update();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private void ApplyObjectToTargets(string propertyName, Object value)
    {
        foreach (Object targetObject in targets)
        {
            if (targetObject == null)
            {
                continue;
            }

            SerializedObject so = new SerializedObject(targetObject);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetObject);
            }
        }

        serializedObject.Update();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static T[] GetSelectedSceneObjectsSortedByX<T>() where T : Component
    {
        Object[] selection = Selection.gameObjects;
        List<T> result = new List<T>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        for (int i = 0; i < selection.Length; i++)
        {
            GameObject go = selection[i] as GameObject;
            if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
            {
                continue;
            }

            T component = go.GetComponentInParent<T>(true);
            if (component == null || component.gameObject == null || !seen.Add(component.gameObject))
            {
                continue;
            }

            result.Add(component);
        }

        result.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return result.ToArray();
    }

    private static T[] GetSceneObjectsSortedByX<T>() where T : Component
    {
        T[] sceneObjects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<T> result = new List<T>();
        for (int i = 0; i < sceneObjects.Length; i++)
        {
            T sceneObject = sceneObjects[i];
            if (sceneObject == null || sceneObject.gameObject == null)
            {
                continue;
            }

            if (!sceneObject.gameObject.scene.IsValid() || !sceneObject.gameObject.scene.isLoaded)
            {
                continue;
            }

            result.Add(sceneObject);
        }

        result.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return result.ToArray();
    }

    private static T[] GetRootObjectsSortedByX<T>(Transform root) where T : Component
    {
        if (root == null)
        {
            return new T[0];
        }

        T[] objects = root.GetComponentsInChildren<T>(true);
        List<T> result = new List<T>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        for (int i = 0; i < objects.Length; i++)
        {
            T component = objects[i];
            if (component == null || component.gameObject == null || !seen.Add(component.gameObject))
            {
                continue;
            }

            result.Add(component);
        }

        result.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return result.ToArray();
    }

    private static GameObject[] GetSelectedSceneGameObjectsSortedByX()
    {
        Object[] selection = Selection.gameObjects;
        List<GameObject> result = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        for (int i = 0; i < selection.Length; i++)
        {
            GameObject go = selection[i] as GameObject;
            if (go == null || !go.scene.IsValid() || !go.scene.isLoaded || !seen.Add(go))
            {
                continue;
            }

            result.Add(go);
        }

        result.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return result.ToArray();
    }

    private static GameObject[] GetSceneBackgroundObjectsSortedByX()
    {
        TilemapRenderer[] renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        List<GameObject> result = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        for (int i = 0; i < renderers.Length; i++)
        {
            TilemapRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            GameObject go = renderer.gameObject;
            if (go == null || !go.scene.IsValid() || !go.scene.isLoaded)
            {
                continue;
            }

            string name = go.name;
            if (!name.StartsWith("Background", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (!seen.Add(go))
            {
                continue;
            }

            result.Add(go);
        }

        result.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return result.ToArray();
    }

    private static GameObject[] GetEncounterBackgroundObjectsSortedByX(Transform root)
    {
        if (root == null)
        {
            return new GameObject[0];
        }

        Transform container = FindDeepChild(root, "BossBackgrounds");
        if (container == null)
        {
            container = FindDeepChild(root, "BossSkillBackgrounds");
        }
        if (container != null)
        {
            TilemapRenderer[] containerRenderers = container.GetComponentsInChildren<TilemapRenderer>(true);
            GameObject[] containerBackgrounds = FilterBackgroundObjectsByName(containerRenderers);
            if (containerBackgrounds.Length > 0)
            {
                return containerBackgrounds;
            }
        }

        TilemapRenderer[] renderers = root.GetComponentsInChildren<TilemapRenderer>(true);
        return FilterBackgroundObjectsByName(renderers);
    }

    private static GameObject[] FilterBackgroundObjectsByName(TilemapRenderer[] renderers)
    {
        List<GameObject> result = new List<GameObject>();
        HashSet<GameObject> seen = new HashSet<GameObject>();

        for (int i = 0; i < renderers.Length; i++)
        {
            TilemapRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            GameObject go = renderer.gameObject;
            if (go == null || !seen.Add(go))
            {
                continue;
            }

            string name = go.name;
            if (!name.StartsWith("Background", System.StringComparison.Ordinal))
            {
                continue;
            }

            result.Add(go);
        }

        result.Sort((left, right) => left.transform.position.x.CompareTo(right.transform.position.x));
        return result.ToArray();
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static Enemy_AbyssMage FindSceneBossMage(ArenaBossEncounterController controller)
    {
        if (controller == null || controller.transform == null)
        {
            return null;
        }

        Transform controllerTransform = controller.transform;
        Enemy_AbyssMage[] mages = Object.FindObjectsOfType<Enemy_AbyssMage>(true);

        for (int i = 0; i < mages.Length; i++)
        {
            Enemy_AbyssMage mage = mages[i];
            if (mage != null && mage.transform != null && mage.transform.IsChildOf(controllerTransform))
            {
                return mage;
            }
        }

        for (int i = 0; i < mages.Length; i++)
        {
            Enemy_AbyssMage mage = mages[i];
            if (mage != null && mage.gameObject != null && mage.gameObject.scene == controller.gameObject.scene)
            {
                return mage;
            }
        }

        return null;
    }

    private static Transform EnsureEmptyChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform existing = parent.Find(childName);
        GameObject childObject = existing != null ? existing.gameObject : new GameObject(childName);
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {childName}");
        }
        else if (existing.parent != parent)
        {
            Undo.SetTransformParent(existing, parent, $"Parent {childName}");
        }

        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject.transform;
    }

    private static ArenaDoorController FindOrCreateDoor(Transform parent, string doorName, Vector3 localPosition)
    {
        Transform existingDoor = parent != null ? parent.Find(doorName) : null;
        if (existingDoor == null)
        {
            return CreateDoor(parent, doorName, localPosition);
        }

        ArenaDoorController doorController = existingDoor.GetComponent<ArenaDoorController>();
        if (doorController == null)
        {
            doorController = Undo.AddComponent<ArenaDoorController>(existingDoor.gameObject);
        }

        EnsureDoorChildren(existingDoor, doorController);
        existingDoor.localPosition = localPosition;
        return doorController;
    }

    private static ArenaDoorController CreateDoor(Transform parent, string doorName, Vector3 localPosition)
    {
        GameObject doorRoot = new GameObject(doorName);
        Undo.RegisterCreatedObjectUndo(doorRoot, $"Create {doorName}");
        if (parent != null)
        {
            Undo.SetTransformParent(doorRoot.transform, parent, $"Parent {doorName}");
        }

        doorRoot.transform.localPosition = localPosition;
        doorRoot.transform.localRotation = Quaternion.identity;
        doorRoot.transform.localScale = Vector3.one;

        ArenaDoorController doorController = Undo.AddComponent<ArenaDoorController>(doorRoot);
        GameObject visualRoot = FindOrCreateChild(doorRoot.transform, "Visual");
        GameObject blockingRoot = FindOrCreateChild(doorRoot.transform, "Blocking");

        SetupDoorVisual(visualRoot);
        SetupDoorBlocking(blockingRoot);

        SerializedObject doorSo = new SerializedObject(doorController);
        SetObject(doorSo, "visualRoot", visualRoot);
        SetObject(doorSo, "blockingRoot", blockingRoot);
        SetBool(doorSo, "openOnStart", true);
        SetString(doorSo, "openBoolParameter", "open");
        doorSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(doorController);
        return doorController;
    }

    private static void EnsureDoorChildren(Transform doorRoot, ArenaDoorController doorController)
    {
        if (doorRoot == null || doorController == null)
        {
            return;
        }

        GameObject visualRoot = FindOrCreateChild(doorRoot, "Visual");
        GameObject blockingRoot = FindOrCreateChild(doorRoot, "Blocking");

        SetupDoorVisual(visualRoot);
        SetupDoorBlocking(blockingRoot);

        SerializedObject doorSo = new SerializedObject(doorController);
        SetObject(doorSo, "visualRoot", visualRoot);
        SetObject(doorSo, "blockingRoot", blockingRoot);
        SetBool(doorSo, "openOnStart", true);
        SetString(doorSo, "openBoolParameter", "open");
        doorSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupDoorVisual(GameObject visualRoot)
    {
        if (visualRoot == null)
        {
            return;
        }

        if (visualRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(visualRoot);
        }

        TilemapRenderer renderer = visualRoot.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(visualRoot);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 100;
    }

    private static void SetupDoorBlocking(GameObject blockingRoot)
    {
        if (blockingRoot == null)
        {
            return;
        }

        int groundLayer = LayerMask.NameToLayer(GroundLayerName);
        if (groundLayer >= 0)
        {
            blockingRoot.layer = groundLayer;
        }

        if (blockingRoot.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(blockingRoot);
        }

        TilemapRenderer renderer = blockingRoot.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(blockingRoot);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = 101;

        if (blockingRoot.GetComponent<TilemapCollider2D>() == null)
        {
            TilemapCollider2D collider = Undo.AddComponent<TilemapCollider2D>(blockingRoot);
            collider.usedByComposite = true;
        }

        if (blockingRoot.GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D body = Undo.AddComponent<Rigidbody2D>(blockingRoot);
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            body.useAutoMass = false;
        }

        if (blockingRoot.GetComponent<CompositeCollider2D>() == null)
        {
            CompositeCollider2D composite = Undo.AddComponent<CompositeCollider2D>(blockingRoot);
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }
    }

    private static GameObject FindOrCreateChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject childObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
        Undo.SetTransformParent(childObject.transform, parent, $"Parent {childName}");
        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        return childObject;
    }

    private static void RemoveChildrenExcept(Transform parent, string childNameToKeep)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != null && child.name != childNameToKeep)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        if (serializedObject == null)
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SetObject(serializedObject, propertyName, value);
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        if (serializedObject == null)
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        if (serializedObject == null)
        {
            return;
        }

        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }

    private static void ClearArray(SerializedProperty arrayProperty)
    {
        if (arrayProperty == null || !arrayProperty.isArray)
        {
            return;
        }

        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty element = arrayProperty.GetArrayElementAtIndex(i);
            element.objectReferenceValue = null;
        }
    }

    private void DrawDoorsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Boss rooms should use their own ArenaDoors group under the boss encounter root. This setup should contain Door_Left only.",
            MessageType.Info);
        EditorGUILayout.PropertyField(doorsProperty, true);
        EditorGUILayout.PropertyField(lockDoorsWhenEncounterStartsProperty);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto Find Local"))
            {
                FillBossDoorsFromEncounter();
            }

            if (GUILayout.Button("Create / Repair Boss Doors"))
            {
                CreateOrRepairBossDoors();
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void FillBossDoorsFromEncounter()
    {
        ArenaBossEncounterController controller = target as ArenaBossEncounterController;
        Transform root = controller != null ? controller.transform.root : null;
        Transform doorGroup = FindDeepChild(root, "ArenaDoors");
        if (doorGroup == null)
        {
            return;
        }

        List<ArenaDoorController> doors = new List<ArenaDoorController>();
        for (int i = 0; i < doorGroup.childCount; i++)
        {
            ArenaDoorController door = doorGroup.GetChild(i).GetComponent<ArenaDoorController>();
            if (door != null)
            {
                doors.Add(door);
            }
        }

        ApplyObjectArrayToTargets(doorsProperty != null ? doorsProperty.name : "doors", doors.ToArray(), 1);
    }

    private void CreateOrRepairBossDoors()
    {
        bool changed = false;
        foreach (Object inspectedTarget in targets)
        {
            ArenaBossEncounterController controller = inspectedTarget as ArenaBossEncounterController;
            if (controller == null)
            {
                continue;
            }

            Transform root = controller.transform != null ? controller.transform.root : null;
            if (root == null)
            {
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Create Or Repair Boss Doors");

            Transform doorGroup = FindDeepChild(root, "ArenaDoors");
            if (doorGroup == null)
            {
                GameObject doorGroupObject = new GameObject("ArenaDoors");
                Undo.RegisterCreatedObjectUndo(doorGroupObject, "Create ArenaDoors");
                Undo.SetTransformParent(doorGroupObject.transform, root, "Parent ArenaDoors");
                doorGroup = doorGroupObject.transform;
                doorGroup.localPosition = Vector3.zero;
                doorGroup.localRotation = Quaternion.identity;
                doorGroup.localScale = Vector3.one;
            }
            else if (doorGroup.parent != root)
            {
                Undo.SetTransformParent(doorGroup, root, "Parent ArenaDoors");
            }

            RemoveChildrenExcept(doorGroup, "Door_Left");
            ArenaDoorController leftDoor = FindOrCreateDoor(doorGroup, "Door_Left", new Vector3(0.5f, 0.5f, 0f));

            SerializedObject controllerSo = new SerializedObject(controller);
            SerializedProperty doorArray = controllerSo.FindProperty(doorsProperty != null ? doorsProperty.name : "doors");
            if (doorArray != null)
            {
                while (doorArray.arraySize < 1)
                {
                    doorArray.InsertArrayElementAtIndex(doorArray.arraySize);
                }

                while (doorArray.arraySize > 1)
                {
                    doorArray.DeleteArrayElementAtIndex(doorArray.arraySize - 1);
                }

                doorArray.GetArrayElementAtIndex(0).objectReferenceValue = leftDoor;
                controllerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(controller);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }

    private void DrawTriggerSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Trigger", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startOnlyOnceProperty);
        EditorGUILayout.PropertyField(playerTagProperty);
        EditorGUILayout.PropertyField(playerProperty);
        EditorGUILayout.PropertyField(clearConfirmDelayProperty);
        EditorGUILayout.EndVertical();
    }
}

[CustomEditor(typeof(ArenaBossFloatingPlatformController))]
public class ArenaBossFloatingPlatformControllerEditor : Editor
{
    private const string GroundLayerName = "Ground";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ArenaBossFloatingPlatformController platform = (ArenaBossFloatingPlatformController)target;

        EditorGUILayout.Space(2f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Rise Sequence", GUILayout.Height(26f)))
            {
                ArenaFloatingPlatformPreviewDriver.Play(platform, appearance: true);
            }
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.HelpBox(
            "Boss-only floating platform. This should live under ArenaBossEncounter and use GroundReferencePoint + Platform_1..Platform_6.",
            MessageType.Info);

        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Quick Select", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Hidden"))
            {
                platform.SetHidden();
                MarkDirty(platform);
            }

            if (GUILayout.Button("Preview Platform 1"))
            {
                platform.SetPlatform1();
                MarkDirty(platform);
            }

            if (GUILayout.Button("Preview Platform 2"))
            {
                platform.SetPlatform2();
                MarkDirty(platform);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview Platform 3"))
            {
                platform.SetPlatform3();
                MarkDirty(platform);
            }

            if (GUILayout.Button("Preview Platform 4"))
            {
                platform.SetPlatform4();
                MarkDirty(platform);
            }

            if (GUILayout.Button("Preview Platform 5"))
            {
                platform.SetPlatform5();
                MarkDirty(platform);
            }

            if (GUILayout.Button("Preview Platform 6"))
            {
                platform.SetPlatform6();
                MarkDirty(platform);
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Preview All Visible"))
            {
                platform.SetAllVisible();
                MarkDirty(platform);
            }

            if (GUILayout.Button("Preview Disappearance Seq"))
            {
                ArenaFloatingPlatformPreviewDriver.Play(platform, appearance: false);
            }
        }

        if (GUILayout.Button("Generate / Repair Template"))
        {
            GenerateOrRepairPlatformTemplate(platform);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("Select Children", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSelectButton(platform.transform, "GroundReferencePoint");
            DrawSelectButton(platform.transform, "Platform_1");
            DrawSelectButton(platform.transform, "Platform_2");
            DrawSelectButton(platform.transform, "Platform_3");
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawSelectButton(platform.transform, "Platform_4");
            DrawSelectButton(platform.transform, "Platform_5");
            DrawSelectButton(platform.transform, "Platform_6");
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void MarkDirty(ArenaBossFloatingPlatformController platform)
    {
        if (platform == null)
        {
            return;
        }

        EditorUtility.SetDirty(platform);
        EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
    }

    private static void DrawSelectButton(Transform root, string childName)
    {
        if (GUILayout.Button($"Select {childName}"))
        {
            Transform child = root != null ? root.Find(childName) : null;
            if (child != null)
            {
                Selection.activeGameObject = child.gameObject;
                EditorGUIUtility.PingObject(child.gameObject);
            }
        }
    }

    private static void GenerateOrRepairPlatformTemplate(ArenaBossFloatingPlatformController platform)
    {
        if (platform == null)
        {
            return;
        }

        Transform root = platform.transform;
        Undo.RegisterFullObjectHierarchyUndo(platform.gameObject, "Generate Or Repair Arena Boss Floating Platform");

        GameObject groundReferencePoint = EnsureGroundReferencePoint(root);
        GameObject platform1 = EnsurePlatformLayer(root, "Platform_1", true, 5);
        GameObject platform2 = EnsurePlatformLayer(root, "Platform_2", true, 6);
        GameObject platform3 = EnsurePlatformLayer(root, "Platform_3", true, 7);
        GameObject platform4 = EnsurePlatformLayer(root, "Platform_4", true, 8);
        GameObject platform5 = EnsurePlatformLayer(root, "Platform_5", true, 9);
        GameObject platform6 = EnsurePlatformLayer(root, "Platform_6", true, 10);

        SerializedObject so = new SerializedObject(platform);
        SetObjectReference(so, "groundReferencePoint", groundReferencePoint != null ? groundReferencePoint.transform : null);
        SetObjectReference(so, "platform1Tilemap", platform1 != null ? platform1.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "platform2Tilemap", platform2 != null ? platform2.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "platform3Tilemap", platform3 != null ? platform3.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "platform4Tilemap", platform4 != null ? platform4.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "platform5Tilemap", platform5 != null ? platform5.GetComponent<Tilemap>() : null);
        SetObjectReference(so, "platform6Tilemap", platform6 != null ? platform6.GetComponent<Tilemap>() : null);
        SetBool(so, "startHidden", true);
        SetFloat(so, "platformRiseInterval", 0.7f);
        SetFloat(so, "platformRiseDuration", 0.85f);
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(platform);
        EditorSceneManager.MarkSceneDirty(platform.gameObject.scene);
        Selection.activeGameObject = platform.gameObject;
        EditorGUIUtility.PingObject(platform.gameObject);
    }

    private static GameObject EnsureGroundReferencePoint(Transform parent)
    {
        if (parent == null)
        {
            return null;
        }

        Transform existing = parent.Find("GroundReferencePoint");
        GameObject childObject = existing != null ? existing.gameObject : new GameObject("GroundReferencePoint");
        if (existing == null)
        {
            Undo.RegisterCreatedObjectUndo(childObject, "Create GroundReferencePoint");
            Undo.SetTransformParent(childObject.transform, parent, "Parent GroundReferencePoint");
        }

        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        childObject.layer = LayerMask.NameToLayer("Default");
        return childObject;
    }

    private static GameObject EnsurePlatformLayer(Transform parent, string childName, bool includeCollider, int sortingOrder)
    {
        if (parent == null)
        {
            return null;
        }

        Transform child = parent.Find(childName);
        GameObject childObject = child != null ? child.gameObject : new GameObject(childName);
        if (child == null)
        {
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            Undo.SetTransformParent(childObject.transform, parent, $"Parent {childName}");
        }

        childObject.transform.localPosition = Vector3.zero;
        childObject.transform.localRotation = Quaternion.identity;
        childObject.transform.localScale = Vector3.one;
        childObject.layer = includeCollider ? LayerMask.NameToLayer(GroundLayerName) : LayerMask.NameToLayer("Default");

        if (childObject.GetComponent<Tilemap>() == null)
        {
            Undo.AddComponent<Tilemap>(childObject);
        }

        TilemapRenderer renderer = childObject.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(childObject);
        }

        renderer.sortingLayerName = "Background";
        renderer.sortingOrder = sortingOrder;

        if (includeCollider)
        {
            TilemapCollider2D tilemapCollider = childObject.GetComponent<TilemapCollider2D>();
            if (tilemapCollider == null)
            {
                tilemapCollider = Undo.AddComponent<TilemapCollider2D>(childObject);
            }

            tilemapCollider.usedByComposite = true;

            Rigidbody2D body = childObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = Undo.AddComponent<Rigidbody2D>(childObject);
            }

            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            body.useAutoMass = false;

            CompositeCollider2D composite = childObject.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = Undo.AddComponent<CompositeCollider2D>(childObject);
            }

            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        }

        return childObject;
    }

    private static void SetObjectReference(SerializedObject so, string propertyName, Object value)
    {
        if (so == null)
        {
            return;
        }

        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        if (so == null)
        {
            return;
        }

        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        if (so == null)
        {
            return;
        }

        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Float)
        {
            property.floatValue = value;
        }
    }
}
