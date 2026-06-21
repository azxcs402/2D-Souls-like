using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(Enemy_Mage))]
public class EnemyMageEditor : Editor
{
    private const string SearchPrefsKey = "EnemyMageEditor.Search";
    private const string HoverAreaAnchorName = "ProjectileHoverArea";
    private const string TeleportAreaAnchorName = "TeleportArea";
    private static readonly string[] TeleportTriggerAnchorNames =
    {
        "TeleportTriggerRange_A",
        "TeleportTriggerRange_B",
        "TeleportTriggerRange_C"
    };

    private string searchQuery;
    private bool showOnlyMatches;

    private readonly List<Section> sections = new List<Section>
    {
        new Section(
            "Core",
            "Enemy base settings. Use this for health, audio range, ground checks, and wall checks.",
            new[]
            {
                new FieldInfo("maxHealth", "Max Health", "health hit points hp"),
                new FieldInfo("canTakeDamage", "Can Take Damage", "damage invulnerable"),
                new FieldInfo("combatSoundDistance", "Combat Sound Distance", "audio range sound distance"),
                new FieldInfo("groundCheckDistance", "Ground Check Distance", "ground floor check"),
                new FieldInfo("whatIsGround", "What Is Ground", "ground layer mask"),
                new FieldInfo("wallCheckDistance", "Wall Check Distance", "wall obstacle"),
                new FieldInfo("wallCheckVerticalSpan", "Wall Check Vertical Span", "wall span"),
                new FieldInfo("primaryWallCheck", "Primary Wall Check", "wall check transform"),
                new FieldInfo("secondaryWallCheck", "Secondary Wall Check", "wall check transform"),
                new FieldInfo("avoidPitAndSpikeHazards", "Avoid Pit And Spike Hazards", "hazard pit spike"),
                new FieldInfo("hazardLookAheadOffset", "Hazard Look Ahead Offset", "hazard look ahead"),
                new FieldInfo("hazardProbeRadius", "Hazard Probe Radius", "hazard probe"),
                new FieldInfo("hazardProbeVerticalOffset", "Hazard Probe Vertical Offset", "hazard probe")
            }
        ),
        new Section(
            "Battle Details",
            "Combat behavior settings. Spell Cast Distance is here. Spell attack cooldown starts when casting begins.",
            new[]
            {
                new FieldInfo("battleMoveSpeed", "Battle Move Speed", "combat chase move"),
                new FieldInfo("attackDistance", "Attack Distance", "melee close range"),
                new FieldInfo("spellCastDistance", "Spell Cast Distance", "spell cast ranged attack remote range"),
                new FieldInfo("attackCooldown", "Attack Cooldown", "attack delay cooldown"),
                new FieldInfo("spellAttackCooldown", "Spell Attack Cooldown (starts when casting begins)", "spell cooldown"),
                new FieldInfo("canChasePlayer", "Can Chase Player", "chase follow"),
                new FieldInfo("battleTimeDuration", "Battle Time Duration", "battle timer"),
                new FieldInfo("minRetreatDistance", "Min Retreat Distance", "retreat close distance"),
                new FieldInfo("battleStopDistance", "Battle Stop Distance", "stop distance"),
                new FieldInfo("retreatVelocity", "Retreat Velocity", "retreat speed vector"),
                new FieldInfo("retreatCooldown", "Retreat Cooldown", "retreat delay"),
                new FieldInfo("retreatMaxDistance", "Retreat Max Distance", "retreat limit"),
                new FieldInfo("retreatSpeed", "Retreat Speed", "retreat movement speed"),
                new FieldInfo("teleportAreaAnchor", "Teleport Area Anchor", "drag this child transform to move teleport area"),
                new FieldInfo("teleportMaxPlacementAttempts", "Teleport Max Placement Attempts", "teleport attempts"),
                new FieldInfo("teleportGroundSearchHeight", "Teleport Ground Search Height", "teleport ray start"),
                new FieldInfo("teleportGroundSearchDistance", "Teleport Ground Search Distance", "teleport ray distance"),
                new FieldInfo("teleportImageEchoCount", "Teleport Image Echo Count", "teleport afterimage count"),
                new FieldInfo("teleportImageEchoLifetime", "Teleport Image Echo Lifetime", "teleport afterimage duration"),
                new FieldInfo("teleportPostDelay", "Teleport Post Delay", "teleport pause"),
                new FieldInfo("onHitTeleportInitialChance", "On Hit Teleport Initial Chance", "teleport hit chance"),
                new FieldInfo("onHitTeleportChanceIncrement", "On Hit Teleport Chance Increment", "teleport hit chance increment"),
                new FieldInfo("rangeTeleportInitialChance", "Range Teleport Initial Chance", "teleport range chance"),
                new FieldInfo("rangeTeleportChanceIncrement", "Range Teleport Chance Increment", "teleport range chance increment"),
                new FieldInfo("rangeTeleportCheckInterval", "Range Teleport Check Interval", "teleport range interval")
            }
        ),
        new Section(
            "Stunned",
            "Settings used when the mage is countered or stunned.",
            new[]
            {
                new FieldInfo("stunnedDuration", "Stunned Duration", "stun time"),
                new FieldInfo("stunnedVelocity", "Stunned Velocity", "stun knockback"),
                new FieldInfo("canBeStunned", "Can Be Stunned", "counter stun"),
                new FieldInfo("stunAttackRecoveryDelay", "Stun Attack Recovery Delay", "stun recovery")
            }
        ),
        new Section(
            "Patrol",
            "Idle / move timings and turn chance outside combat.",
            new[]
            {
                new FieldInfo("idleDurationMin", "Idle Duration Min", "idle wait"),
                new FieldInfo("idleDurationMax", "Idle Duration Max", "idle wait"),
                new FieldInfo("moveDurationMin", "Move Duration Min", "patrol move"),
                new FieldInfo("moveDurationMax", "Move Duration Max", "patrol move"),
                new FieldInfo("patrolTurnChance", "Patrol Turn Chance", "turn flip random"),
                new FieldInfo("patrolTurnDelay", "Patrol Turn Delay", "turn delay")
            }
        ),
        new Section(
            "Movement",
            "Base movement tuning and animation speed.",
            new[]
            {
                new FieldInfo("moveSpeed", "Move Speed", "walk patrol speed"),
                new FieldInfo("moveAnimSpeedMultiplier", "Move Anim Speed Multiplier", "anim speed")
            }
        ),
        new Section(
            "Player Detection",
            "Detection range, chase height and sight loss timing.",
            new[]
            {
                new FieldInfo("whatIsPlayer", "What Is Player", "player layer mask"),
                new FieldInfo("playerCheck", "Player Check", "target check transform"),
                new FieldInfo("playerCheckDistance", "Player Check Distance", "detection range sight"),
                new FieldInfo("chaseVerticalDistance", "Chase Vertical Distance", "vertical tolerance"),
                new FieldInfo("loseSightDuration", "Lose Sight Duration", "forget player delay")
            }
        ),
        new Section(
            "Mage Info",
            "Projectile setup and mage-specific helper points.",
            new[]
            {
                new FieldInfo("spellPrefab", "Spell Prefab", "projectile fireball"),
                new FieldInfo("spellStartPosition", "Spell Start Position", "spawn point"),
                new FieldInfo("amountToCast", "Amount To Cast", "projectile count"),
                new FieldInfo("spellCastCooldown", "Projectile Spawn Interval", "gap between projectiles"),
                new FieldInfo("projectileHoverArrivalDuration", "Projectile Hover Arrival Duration", "fly from hand to hover point"),
                new FieldInfo("showBattleRangesInScene", "Show Battle Ranges In Scene", "scene gizmo range toggle"),
                new FieldInfo("projectileHoverAreaAnchor", "Projectile Hover Area Anchor", "drag this child transform to move hover area"),
                new FieldInfo("teleportAreaAnchor", "Teleport Area Anchor", "drag this child transform to move teleport area"),
                new FieldInfo("projectileHoverMinSeparation", "Projectile Hover Min Separation", "min distance between hover points"),
                new FieldInfo("projectileHoverMaxPlacementAttempts", "Projectile Hover Max Placement Attempts", "random placement attempts"),
                new FieldInfo("behindCollisionCheck", "Behind Collision Check", "retreat wall check"),
                new FieldInfo("hasRecoveryAnimation", "Has Recovery Animation", "stun recovery"),
                new FieldInfo("canBeKnockedBack", "Can Be Knocked Back", "knockback")
            }
        ),
        new Section(
            "Attack Info",
            "Attack data and animation state names. 'skeletonAttack' is the current melee attack state name.",
            new[]
            {
                new FieldInfo("mageAttackData", "Mage Attack Data", "melee hitbox"),
                new FieldInfo("attackAnimationState", "Attack Animation State", "skeletonAttack attack"),
                new FieldInfo("idleAnimationState", "Idle Animation State", "mageIdle"),
                new FieldInfo("moveAnimationState", "Move Animation State", "mageMove"),
                new FieldInfo("battleAnimationState", "Battle Animation State", "mageBattle idle move"),
                new FieldInfo("stunnedAnimationState", "Stunned Animation State", "mageStunned"),
                new FieldInfo("stunRecoveryAnimationState", "Stun Recovery Animation State", "mageStunRecovery"),
                new FieldInfo("spellCastAnimationState", "Spell Cast Animation State", "mageSpellCast"),
                new FieldInfo("spellCastPerformedAnimationState", "Spell Cast Performed Animation State", "mageSpellCast_performed")
            }
        ),
        new Section(
            "Death Info",
            "Death motion and drop-through tuning.",
            new[]
            {
                new FieldInfo("deadFallSpeed", "Dead Fall Speed", "death fall"),
                new FieldInfo("deadSlideSpeed", "Dead Slide Speed", "slide"),
                new FieldInfo("deadSlideAcceleration", "Dead Slide Acceleration", "slide acceleration"),
                new FieldInfo("deadDropThroughDelay", "Dead Drop Through Delay", "drop through"),
                new FieldInfo("deadDisappearDelay", "Dead Disappear Delay", "despawn"),
                new FieldInfo("deadFallAngle", "Dead Fall Angle", "death angle")
            }
        )
    };

    private void OnEnable()
    {
        searchQuery = EditorPrefs.GetString(SearchPrefsKey, string.Empty);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SearchableInspectorDrawer.DrawScriptField(serializedObject);
        EnemyInspectorActionDrawer.DrawKillEnemyButton(target as Enemy);
        if (GUILayout.Button("Move Script Component Up"))
        {
            EnemyComponentOrderTools.MoveComponentToTop((Component)target);
        }
        DrawHoverAnchorUtility();
        DrawTeleportAnchorUtility();
        DrawTeleportTriggerRangeUtility();
        DrawSearchBar();
        DrawQuickFind();

        EditorGUILayout.Space();
        DrawFilteredSections();

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Enemy_Mage mage = (Enemy_Mage)target;
        if (mage == null || (!mage.ShowBattleRangesInScene && !IsMageHierarchySelected(mage)))
        {
            return;
        }

        serializedObject.Update();

        SerializedProperty anchorProperty = serializedObject.FindProperty("projectileHoverAreaAnchor");
        SerializedProperty offsetProperty = serializedObject.FindProperty("projectileHoverAreaOffset");
        SerializedProperty sizeProperty = serializedObject.FindProperty("projectileHoverAreaSize");
        SerializedProperty teleportAnchorProperty = serializedObject.FindProperty("teleportAreaAnchor");
        Transform hoverAnchor = anchorProperty != null ? anchorProperty.objectReferenceValue as Transform : null;
        Transform teleportAnchor = teleportAnchorProperty != null ? teleportAnchorProperty.objectReferenceValue as Transform : null;
        Vector2 fallbackOffset = offsetProperty != null ? offsetProperty.vector2Value : Vector2.zero;
        if (hoverAnchor != null)
        {
            DrawHoverAreaGizmo(mage.ProjectileHoverAreaCenter, mage.ProjectileHoverAreaSize);
            if (teleportAnchor != null)
            {
                DrawTeleportAreaOutline(mage.TeleportAreaCenter, mage.TeleportAreaSize);
            }
            DrawRangeDisc(
                mage.transform.position,
                mage.AttackDistance,
                new Color(1f, .28f, .18f, .16f),
                new Color(1f, .28f, .18f, 1f),
                $"Melee / Radius {mage.AttackDistance:0.##}"
            );

            DrawRangeDisc(
                mage.transform.position,
                mage.SpellCastDistance,
                new Color(.2f, .7f, 1f, .14f),
                new Color(.2f, .7f, 1f, 1f),
                $"Spell / Radius {mage.SpellCastDistance:0.##}"
            );
            return;
        }

        if (teleportAnchor != null)
        {
            DrawTeleportAreaOutline(mage.TeleportAreaCenter, mage.TeleportAreaSize);
        }

        if (sizeProperty == null)
        {
            return;
        }

        Vector2 size = sizeProperty.vector2Value;
        Vector3 center = mage.transform.position + new Vector3(fallbackOffset.x, fallbackOffset.y, 0f);
        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3 oppositeCorner = center - half;
        Vector3 sizeCorner = center + half;

        EditorGUI.BeginChangeCheck();
        Vector3 movedCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mage, "Move Mage Projectile Hover Area");
            offsetProperty.vector2Value = new Vector2(
                movedCenter.x - mage.transform.position.x,
                movedCenter.y - mage.transform.position.y
            );
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mage);
            return;
        }

        EditorGUI.BeginChangeCheck();
        Vector3 movedCorner = Handles.FreeMoveHandle(
            sizeCorner,
            0.09f,
            Vector3.zero,
            Handles.CubeHandleCap
        );
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(mage, "Resize Mage Projectile Hover Area");
            Vector2 newCenter = new Vector2(
                (movedCorner.x + oppositeCorner.x) * .5f,
                (movedCorner.y + oppositeCorner.y) * .5f
            );
            Vector2 newSize = new Vector2(
                Mathf.Max(.1f, Mathf.Abs(movedCorner.x - oppositeCorner.x)),
                Mathf.Max(.1f, Mathf.Abs(movedCorner.y - oppositeCorner.y))
            );

            offsetProperty.vector2Value = newCenter - (Vector2)mage.transform.position;
            sizeProperty.vector2Value = newSize;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mage);
        }
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Mage Inspector Search", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        searchQuery = EditorGUILayout.TextField("Search", searchQuery);
        if (EditorGUI.EndChangeCheck())
        {
            EditorPrefs.SetString(SearchPrefsKey, searchQuery ?? string.Empty);
        }

        showOnlyMatches = EditorGUILayout.ToggleLeft("Show only matching fields", showOnlyMatches);

        int matchCount = CountMatches(searchQuery);
        EditorGUILayout.LabelField($"{matchCount} matching field(s) found.");
        EditorGUILayout.EndVertical();
    }

    private void DrawHoverAnchorUtility()
    {
        Enemy_Mage mage = (Enemy_Mage)target;
        if (mage == null)
        {
            return;
        }

        SerializedProperty anchorProperty = serializedObject.FindProperty("projectileHoverAreaAnchor");
        Transform anchor = anchorProperty != null ? anchorProperty.objectReferenceValue as Transform : null;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(anchor == null ? "Create Hover Anchor" : "Select Hover Anchor"))
        {
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject(HoverAreaAnchorName);
                Undo.RegisterCreatedObjectUndo(anchorObject, "Create Hover Anchor");
                anchorObject.transform.SetParent(mage.transform, false);
                anchorObject.transform.localPosition = mage.ProjectileHoverAreaOffset;
                anchorObject.transform.localRotation = Quaternion.identity;
                anchorObject.transform.localScale = Vector3.one;
                Enemy_MageProjectileHoverAreaAnchor anchorComponent = anchorObject.AddComponent<Enemy_MageProjectileHoverAreaAnchor>();
                SerializedObject anchorSo = new SerializedObject(anchorComponent);
                SerializedProperty hoverSizeProperty = anchorSo.FindProperty("hoverAreaSize");
                if (hoverSizeProperty != null)
                {
                    hoverSizeProperty.vector2Value = mage.ProjectileHoverAreaSize;
                    anchorSo.ApplyModifiedPropertiesWithoutUndo();
                }

                anchorProperty.objectReferenceValue = anchorObject.transform;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(mage);
                Selection.activeGameObject = anchorObject;
            }
            else
            {
                Selection.activeTransform = anchor;
                EditorGUIUtility.PingObject(anchor);
            }
        }

        if (anchor != null && GUILayout.Button("Clear Hover Anchor"))
        {
            anchorProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mage);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTeleportAnchorUtility()
    {
        Enemy_Mage mage = (Enemy_Mage)target;
        if (mage == null)
        {
            return;
        }

        SerializedProperty anchorProperty = serializedObject.FindProperty("teleportAreaAnchor");
        Transform anchor = anchorProperty != null ? anchorProperty.objectReferenceValue as Transform : null;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(anchor == null ? "Create Teleport Area" : "Select Teleport Area"))
        {
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject(TeleportAreaAnchorName);
                Undo.RegisterCreatedObjectUndo(anchorObject, "Create Teleport Area");
                anchorObject.transform.SetParent(mage.transform, false);
                anchorObject.transform.localPosition = mage.TeleportAreaOffset;
                anchorObject.transform.localRotation = Quaternion.identity;
                anchorObject.transform.localScale = Vector3.one;

                Enemy_MageTeleportAreaAnchor anchorComponent = anchorObject.AddComponent<Enemy_MageTeleportAreaAnchor>();
                SerializedObject anchorSo = new SerializedObject(anchorComponent);
                SerializedProperty areaSizeProperty = anchorSo.FindProperty("areaSize");
                if (areaSizeProperty != null)
                {
                    areaSizeProperty.vector2Value = mage.TeleportAreaSize;
                    anchorSo.ApplyModifiedPropertiesWithoutUndo();
                }

                anchorProperty.objectReferenceValue = anchorObject.transform;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(mage);
                Selection.activeGameObject = anchorObject;
            }
            else
            {
                Selection.activeTransform = anchor;
                EditorGUIUtility.PingObject(anchor);
            }
        }

        if (anchor != null && GUILayout.Button("Clear Teleport Area"))
        {
            anchorProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mage);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTeleportTriggerRangeUtility()
    {
        Enemy_Mage mage = (Enemy_Mage)target;
        if (mage == null)
        {
            return;
        }

        Enemy_MageTeleportTriggerAnchor[] anchors = mage.GetComponentsInChildren<Enemy_MageTeleportTriggerAnchor>(true);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(anchors.Length == 0 ? "Create Teleport Trigger Ranges" : "Select Teleport Trigger Range A"))
        {
            if (anchors.Length == 0)
            {
                CreateTeleportTriggerRanges(mage);
            }
            else
            {
                Selection.activeTransform = anchors[0].transform;
                EditorGUIUtility.PingObject(anchors[0]);
            }
        }

        if (anchors.Length > 1 && GUILayout.Button("Select B"))
        {
            Selection.activeTransform = anchors[1].transform;
            EditorGUIUtility.PingObject(anchors[1]);
        }

        if (anchors.Length > 2 && GUILayout.Button("Select C"))
        {
            Selection.activeTransform = anchors[2].transform;
            EditorGUIUtility.PingObject(anchors[2]);
        }

        if (anchors.Length > 0 && GUILayout.Button("Recreate Teleport Trigger Ranges"))
        {
            CreateTeleportTriggerRanges(mage);
        }

        EditorGUILayout.EndHorizontal();
    }

    private static void CreateTeleportTriggerRanges(Enemy_Mage mage)
    {
        if (mage == null)
        {
            return;
        }

        Vector3[] defaultPositions =
        {
            new Vector3(-2.2f, 0.2f, 0f),
            new Vector3(0f, 0.2f, 0f),
            new Vector3(2.2f, 0.2f, 0f)
        };

        for (int i = 0; i < TeleportTriggerAnchorNames.Length; i++)
        {
            Transform existing = mage.transform.Find(TeleportTriggerAnchorNames[i]);
            GameObject anchorObject;

            if (existing == null)
            {
                anchorObject = new GameObject(TeleportTriggerAnchorNames[i]);
                Undo.RegisterCreatedObjectUndo(anchorObject, "Create Teleport Trigger Range");
                anchorObject.transform.SetParent(mage.transform, false);
            }
            else
            {
                anchorObject = existing.gameObject;
            }

            anchorObject.transform.localPosition = defaultPositions[i];
            anchorObject.transform.localRotation = Quaternion.identity;
            anchorObject.transform.localScale = Vector3.one;

            Enemy_MageTeleportTriggerAnchor anchor = anchorObject.GetComponent<Enemy_MageTeleportTriggerAnchor>();
            if (anchor == null)
            {
                anchor = anchorObject.AddComponent<Enemy_MageTeleportTriggerAnchor>();
            }

            SerializedObject anchorSo = new SerializedObject(anchor);
            SerializedProperty radiusProperty = anchorSo.FindProperty("radius");
            if (radiusProperty != null)
            {
                radiusProperty.floatValue = 2f;
                anchorSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorUtility.SetDirty(mage);
        Selection.activeTransform = mage.transform.Find(TeleportTriggerAnchorNames[0]);
    }

    private void DrawQuickFind()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Spell Cast Distance"))
        {
            SetSearch("spell cast distance");
        }

        if (GUILayout.Button("Spell Attack Cooldown"))
        {
            SetSearch("spell attack cooldown");
        }

        if (GUILayout.Button("Hover Area"))
        {
            SetSearch("hover area");
        }

        if (GUILayout.Button("Teleport Area"))
        {
            SetSearch("teleport area");
        }

        if (GUILayout.Button("Attack Distance"))
        {
            SetSearch("attack distance");
        }

        if (GUILayout.Button("Health"))
        {
            SetSearch("health");
        }

        if (GUILayout.Button("Hazard"))
        {
            SetSearch("hazard");
        }

        if (GUILayout.Button("Player Check"))
        {
            SetSearch("player check");
        }

        if (GUILayout.Button("Clear"))
        {
            SetSearch(string.Empty);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void SetSearch(string value)
    {
        searchQuery = value;
        EditorPrefs.SetString(SearchPrefsKey, searchQuery ?? string.Empty);
        GUI.FocusControl(null);
    }

    private void DrawFilteredSections()
    {
        bool hasSearch = !string.IsNullOrWhiteSpace(searchQuery);
        string normalizedSearch = hasSearch ? searchQuery.Trim().ToLowerInvariant() : string.Empty;

        foreach (Section section in sections)
        {
            List<FieldInfo> visibleFields = section.Fields
                .Where(field => !hasSearch || Matches(field, normalizedSearch))
                .ToList();

            if (visibleFields.Count == 0 && hasSearch && showOnlyMatches)
            {
                continue;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(section.Title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(section.Description, EditorStyles.miniLabel);

            if (hasSearch && visibleFields.Count == 0)
            {
                EditorGUILayout.HelpBox("No matching fields in this section.", MessageType.None);
            }

            foreach (FieldInfo field in section.Fields)
            {
                bool matches = !hasSearch || Matches(field, normalizedSearch);
                if (hasSearch && showOnlyMatches && !matches)
                {
                    continue;
                }

                DrawField(field, matches);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2f);
        }
    }

    private void DrawField(FieldInfo field, bool highlight)
    {
        SerializedProperty property = serializedObject.FindProperty(field.PropertyName);
        if (property == null)
        {
            return;
        }

        Color previousColor = GUI.backgroundColor;
        if (highlight)
        {
            GUI.backgroundColor = new Color(.85f, .95f, 1f, 1f);
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(property, new GUIContent(field.Label));

        if (highlight && !string.IsNullOrWhiteSpace(searchQuery))
        {
            EditorGUILayout.LabelField($"Matched by: {field.Keywords}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
        GUI.backgroundColor = previousColor;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawMageGizmos(Enemy_Mage mage, GizmoType gizmoType)
    {
        if (mage == null || (!mage.ShowBattleRangesInScene && !IsMageHierarchySelected(mage)))
        {
            return;
        }

        DrawHoverAreaGizmo(mage.ProjectileHoverAreaCenter, mage.ProjectileHoverAreaSize);
        DrawTeleportAreaOutline(mage.TeleportAreaCenter, mage.TeleportAreaSize);
        DrawTeleportTriggerRanges(mage);

        DrawRangeDisc(
            mage.transform.position,
            mage.AttackDistance,
            new Color(1f, .28f, .18f, .16f),
            new Color(1f, .28f, .18f, 1f),
            $"Melee / Radius {mage.AttackDistance:0.##}"
        );

        DrawRangeDisc(
            mage.transform.position,
            mage.SpellCastDistance,
            new Color(.2f, .7f, 1f, .14f),
            new Color(.2f, .7f, 1f, 1f),
            $"Spell / Radius {mage.SpellCastDistance:0.##}"
        );
    }

    private static bool IsMageHierarchySelected(Enemy_Mage mage)
    {
        if (mage == null)
        {
            return false;
        }

        Transform activeTransform = Selection.activeTransform;
        return activeTransform != null
            && (activeTransform == mage.transform || activeTransform.IsChildOf(mage.transform));
    }

    private static void DrawRangeDisc(Vector3 center, float radius, Color fillColor, Color wireColor, string label)
    {
        if (radius <= 0f)
        {
            return;
        }

        Handles.color = fillColor;
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        Handles.color = wireColor;
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (radius + .12f), label, labelStyle);
    }

    private static void DrawHoverAreaGizmo(Vector3 center, Vector2 size, bool showLabel = true)
    {
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Color fillColor = new Color(.35f, .8f, 1f, .12f);
        Color wireColor = new Color(.35f, .8f, 1f, 1f);

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);

        if (!showLabel)
        {
            return;
        }

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (size.y * .5f + .12f), $"Spell Hover Area / {size.x:0.##} x {size.y:0.##}", labelStyle);
    }

    private static void DrawTeleportAreaOutline(Vector3 center, Vector2 size)
    {
        Enemy_MageTeleportAreaAnchor.DrawOutline(center, size);
    }

    private static void DrawTeleportTriggerRanges(Enemy_Mage mage)
    {
        if (mage == null)
        {
            return;
        }

        Enemy_MageTeleportTriggerAnchor[] anchors = mage.GetComponentsInChildren<Enemy_MageTeleportTriggerAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            Enemy_MageTeleportTriggerAnchor anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            Enemy_MageTeleportTriggerAnchor.DrawRange(anchor.transform.position, anchor.Radius, anchor.name);
        }
    }

    private int CountMatches(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return sections.Sum(section => section.Fields.Length);
        }

        string normalizedQuery = query.Trim().ToLowerInvariant();
        return sections.Sum(section => section.Fields.Count(field => Matches(field, normalizedQuery)));
    }

    private bool Matches(FieldInfo field, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return field.PropertyName.ToLowerInvariant().Contains(normalizedQuery)
            || field.Label.ToLowerInvariant().Contains(normalizedQuery)
            || field.Keywords.ToLowerInvariant().Contains(normalizedQuery);
    }

    private readonly struct Section
    {
        public Section(string title, string description, FieldInfo[] fields)
        {
            Title = title;
            Description = description;
            Fields = fields;
        }

        public string Title { get; }
        public string Description { get; }
        public FieldInfo[] Fields { get; }
    }

    private readonly struct FieldInfo
    {
        public FieldInfo(string propertyName, string label, string keywords)
        {
            PropertyName = propertyName;
            Label = label;
            Keywords = keywords;
        }

        public string PropertyName { get; }
        public string Label { get; }
        public string Keywords { get; }
    }
}
