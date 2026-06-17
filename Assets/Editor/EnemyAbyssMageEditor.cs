using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(Enemy_AbyssMage))]
public class EnemyAbyssMageEditor : Editor
{
    private const string SearchPrefsKey = "EnemyAbyssMageEditor.Search";
    private static readonly string[] HoverAreaAnchorNames =
    {
        "ProjectileHoverArea1",
        "ProjectileHoverArea2"
    };
    private static readonly string[] SpellStartPointNames =
    {
        "SpellStartPoint1",
        "SpellStartPoint2"
    };
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
            "Enemy base settings. Use this for health, ground checks, and wall checks.",
            new[]
            {
                new FieldInfo("maxHealth", "Max Health", "health hit points hp"),
                new FieldInfo("canTakeDamage", "Can Take Damage", "damage invulnerable"),
                new FieldInfo("groundCheckDistance", "Ground Check Distance", "ground floor check"),
                new FieldInfo("whatIsGround", "What Is Ground", "ground layer mask"),
                new FieldInfo("wallCheckDistance", "Wall Check Distance", "wall obstacle"),
                new FieldInfo("wallCheckVerticalSpan", "Wall Check Vertical Span", "wall span"),
                new FieldInfo("primaryWallCheck", "Primary Wall Check", "wall check transform"),
                new FieldInfo("secondaryWallCheck", "Secondary Wall Check", "wall check transform")
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
            "AbyssMage Info",
            "Projectile setup and mage-specific helper points. Left and right spell points each map to their own hover area.",
            new[]
            {
                new FieldInfo("spellPrefab", "Spell Prefab", "projectile fireball"),
                new FieldInfo("spellStartPosition1", "Spell Start Point 1", "left spawn point"),
                new FieldInfo("spellStartPosition2", "Spell Start Point 2", "right spawn point"),
                new FieldInfo("amountToCast", "Amount To Cast Per Side", "projectile count per side"),
                new FieldInfo("spellCastCooldown", "Projectile Spawn Interval", "gap between projectiles"),
                new FieldInfo("projectileHoverArrivalDuration", "Projectile Hover Arrival Duration", "fly from hand to hover point"),
                new FieldInfo("showBattleRangesInScene", "Show Battle Ranges In Scene", "scene gizmo range toggle"),
                new FieldInfo("projectileHoverAreaAnchor1", "Projectile Hover Area 1", "left hover area"),
                new FieldInfo("projectileHoverAreaAnchor2", "Projectile Hover Area 2", "right hover area"),
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
            "Attack data and animation state names. 'abyssMageAttack' is the current melee attack state name.",
            new[]
            {
                new FieldInfo("abyssMageAttackData", "AbyssMage Attack Data", "melee hitbox"),
                new FieldInfo("attackAnimationState", "Attack Animation State", "abyssMageAttack attack"),
                new FieldInfo("idleAnimationState", "Idle Animation State", "abyssMageIdle"),
                new FieldInfo("moveAnimationState", "Move Animation State", "abyssMageMove"),
                new FieldInfo("battleAnimationState", "Battle Animation State", "mageBattle idle move"),
                new FieldInfo("stunnedAnimationState", "Stunned Animation State", "abyssMageStunned"),
                new FieldInfo("stunRecoveryAnimationState", "Stun Recovery Animation State", "abyssMageStunRecovery"),
                new FieldInfo("spellCastAnimationState", "Spell Cast Animation State", "abyssMageSpellCast"),
                new FieldInfo("spellCastPerformedAnimationState", "Spell Cast Performed Animation State", "abyssMageSpellCast_performed")
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
        if (GUILayout.Button("Move Script Component Up"))
        {
            EnemyComponentOrderTools.MoveComponentToTop((Component)target);
        }
        if (GUILayout.Button("Sync Scene Instance To Boss Setup"))
        {
            EnemyAbyssMageSceneConfigurator.ConfigureInstance(((Enemy_AbyssMage)target).gameObject);
        }
        DrawHoverAnchorUtility();
        DrawSpellStartPointUtility();
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
        Enemy_AbyssMage mage = (Enemy_AbyssMage)target;
        if (mage == null || (!mage.ShowBattleRangesInScene && !IsMageHierarchySelected(mage)))
        {
            return;
        }

        serializedObject.Update();

        SerializedProperty hoverAnchor1Property = serializedObject.FindProperty("projectileHoverAreaAnchor1");
        SerializedProperty hoverAnchor2Property = serializedObject.FindProperty("projectileHoverAreaAnchor2");
        SerializedProperty sizeProperty = serializedObject.FindProperty("projectileHoverAreaSize");
        SerializedProperty teleportAnchorProperty = serializedObject.FindProperty("teleportAreaAnchor");
        Transform hoverAnchor1 = hoverAnchor1Property != null ? hoverAnchor1Property.objectReferenceValue as Transform : null;
        Transform hoverAnchor2 = hoverAnchor2Property != null ? hoverAnchor2Property.objectReferenceValue as Transform : null;
        Transform teleportAnchor = teleportAnchorProperty != null ? teleportAnchorProperty.objectReferenceValue as Transform : null;
        if (hoverAnchor1 != null || hoverAnchor2 != null)
        {
            if (hoverAnchor1 != null)
            {
                DrawHoverAreaGizmo(mage.ProjectileHoverAreaCenter1, mage.ProjectileHoverAreaSize1, "Projectile Hover Area 1");
            }

            if (hoverAnchor2 != null)
            {
                DrawHoverAreaGizmo(mage.ProjectileHoverAreaCenter2, mage.ProjectileHoverAreaSize2, "Projectile Hover Area 2");
            }

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
        Vector3 center = hoverAnchor1 != null
            ? mage.ProjectileHoverAreaCenter1
            : mage.ProjectileHoverAreaCenter2;
        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3 oppositeCorner = center - half;
        Vector3 sizeCorner = center + half;

        EditorGUI.BeginChangeCheck();
        Vector3 movedCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(hoverAnchor1 != null ? hoverAnchor1 : hoverAnchor2 != null ? hoverAnchor2 : mage.transform, "Move Mage Projectile Hover Area 1");
            if (hoverAnchor1 != null)
            {
                hoverAnchor1.localPosition = new Vector2(
                    movedCenter.x - mage.transform.position.x,
                    movedCenter.y - mage.transform.position.y
                );
                EditorUtility.SetDirty(hoverAnchor1);
            }
            else if (hoverAnchor2 != null)
            {
                hoverAnchor2.localPosition = new Vector2(
                    movedCenter.x - mage.transform.position.x,
                    movedCenter.y - mage.transform.position.y
                );
                EditorUtility.SetDirty(hoverAnchor2);
            }
            else
            {
                DrawHoverAreaGizmo(movedCenter, size, "Projectile Hover Area 1");
            }
        }

        EditorGUI.BeginChangeCheck();
        Vector3 movedCorner = Handles.FreeMoveHandle(sizeCorner, 0.09f, Vector3.zero, Handles.CubeHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(sizeProperty.serializedObject.targetObject, "Resize Mage Projectile Hover Area 1");
            Vector2 newCenter = new Vector2(
                (movedCorner.x + oppositeCorner.x) * .5f,
                (movedCorner.y + oppositeCorner.y) * .5f
            );
            Vector2 newSize = new Vector2(
                Mathf.Max(.1f, Mathf.Abs(movedCorner.x - oppositeCorner.x)),
                Mathf.Max(.1f, Mathf.Abs(movedCorner.y - oppositeCorner.y))
            );

            sizeProperty.vector2Value = newSize;
            if (hoverAnchor1 != null)
            {
                hoverAnchor1.localPosition = newCenter - (Vector2)mage.transform.position;
                EditorUtility.SetDirty(hoverAnchor1);
            }
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
        Enemy_AbyssMage mage = (Enemy_AbyssMage)target;
        if (mage == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Projectile Hover Areas", EditorStyles.boldLabel);
        DrawAnchorRow(
            mage,
            0,
            HoverAreaAnchorNames[0],
            "projectileHoverAreaAnchor1",
            "Projectile Hover Area 1",
            mage.ProjectileHoverAreaAnchor1 != null
                ? new Vector2(mage.ProjectileHoverAreaAnchor1.localPosition.x, mage.ProjectileHoverAreaAnchor1.localPosition.y)
                : new Vector2(-0.85f, 2.33f),
            mage.ProjectileHoverAreaSize1
        );
        DrawAnchorRow(
            mage,
            1,
            HoverAreaAnchorNames[1],
            "projectileHoverAreaAnchor2",
            "Projectile Hover Area 2",
            mage.ProjectileHoverAreaAnchor2 != null
                ? new Vector2(mage.ProjectileHoverAreaAnchor2.localPosition.x, mage.ProjectileHoverAreaAnchor2.localPosition.y)
                : new Vector2(0.85f, 2.33f),
            mage.ProjectileHoverAreaSize2
        );
        EditorGUILayout.EndVertical();
    }

    private void DrawSpellStartPointUtility()
    {
        Enemy_AbyssMage mage = (Enemy_AbyssMage)target;
        if (mage == null)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Spell Start Points", EditorStyles.boldLabel);
        DrawTransformAnchorRow(mage, 0, SpellStartPointNames[0], "spellStartPosition1", "Spell Start Point 1", mage.SpellStartPosition1);
        DrawTransformAnchorRow(mage, 1, SpellStartPointNames[1], "spellStartPosition2", "Spell Start Point 2", mage.SpellStartPosition2);
        EditorGUILayout.EndVertical();
    }

    private void DrawAnchorRow(Enemy_AbyssMage mage, int laneIndex, string defaultName, string propertyName, string label, Vector2 defaultLocalPosition, Vector2 hoverSize)
    {
        SerializedProperty anchorProperty = serializedObject.FindProperty(propertyName);
        Transform anchor = anchorProperty != null ? anchorProperty.objectReferenceValue as Transform : null;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(anchor == null ? $"Create {label}" : $"Select {label}"))
        {
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject(defaultName);
                Undo.RegisterCreatedObjectUndo(anchorObject, $"Create {label}");
                anchorObject.transform.SetParent(mage.transform, false);
                anchorObject.transform.localPosition = defaultLocalPosition;
                anchorObject.transform.localRotation = Quaternion.identity;
                anchorObject.transform.localScale = Vector3.one;
                Enemy_AbyssMageProjectileHoverAreaAnchor anchorComponent = anchorObject.AddComponent<Enemy_AbyssMageProjectileHoverAreaAnchor>();
                SerializedObject anchorSo = new SerializedObject(anchorComponent);
                SerializedProperty hoverSizeProperty = anchorSo.FindProperty("hoverAreaSize");
                if (hoverSizeProperty != null)
                {
                    hoverSizeProperty.vector2Value = hoverSize;
                    anchorSo.ApplyModifiedPropertiesWithoutUndo();
                }

                if (anchorProperty != null)
                {
                    anchorProperty.objectReferenceValue = anchorObject.transform;
                    serializedObject.ApplyModifiedProperties();
                }
                EditorUtility.SetDirty(mage);
                Selection.activeGameObject = anchorObject;
            }
            else
            {
                Selection.activeTransform = anchor;
                EditorGUIUtility.PingObject(anchor);
            }
        }

        if (anchor != null && GUILayout.Button($"Clear {label}"))
        {
            anchorProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mage);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTransformAnchorRow(Enemy_AbyssMage mage, int laneIndex, string defaultName, string propertyName, string label, Transform current)
    {
        SerializedProperty anchorProperty = serializedObject.FindProperty(propertyName);
        Transform anchor = anchorProperty != null ? anchorProperty.objectReferenceValue as Transform : current;

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(anchor == null ? $"Create {label}" : $"Select {label}"))
        {
            if (anchor == null)
            {
                GameObject anchorObject = new GameObject(defaultName);
                Undo.RegisterCreatedObjectUndo(anchorObject, $"Create {label}");
                anchorObject.transform.SetParent(mage.transform, false);
                anchorObject.transform.localPosition = laneIndex == 0 ? new Vector3(-0.38f, 0.46f, 0f) : new Vector3(0.38f, 0.46f, 0f);
                anchorObject.transform.localRotation = Quaternion.identity;
                anchorObject.transform.localScale = Vector3.one;

                if (anchorProperty != null)
                {
                    anchorProperty.objectReferenceValue = anchorObject.transform;
                    serializedObject.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(mage);
                Selection.activeGameObject = anchorObject;
            }
            else
            {
                Selection.activeTransform = anchor;
                EditorGUIUtility.PingObject(anchor);
            }
        }

        if (anchor != null && GUILayout.Button($"Clear {label}"))
        {
            anchorProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(mage);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTeleportAnchorUtility()
    {
        Enemy_AbyssMage mage = (Enemy_AbyssMage)target;
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

                Enemy_AbyssMageTeleportAreaAnchor anchorComponent = anchorObject.AddComponent<Enemy_AbyssMageTeleportAreaAnchor>();
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
        Enemy_AbyssMage mage = (Enemy_AbyssMage)target;
        if (mage == null)
        {
            return;
        }

        Enemy_AbyssMageTeleportTriggerAnchor[] anchors = mage.GetComponentsInChildren<Enemy_AbyssMageTeleportTriggerAnchor>(true);

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

    private static void CreateTeleportTriggerRanges(Enemy_AbyssMage mage)
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

            Enemy_AbyssMageTeleportTriggerAnchor anchor = anchorObject.GetComponent<Enemy_AbyssMageTeleportTriggerAnchor>();
            if (anchor == null)
            {
                anchor = anchorObject.AddComponent<Enemy_AbyssMageTeleportTriggerAnchor>();
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
    private static void DrawMageGizmos(Enemy_AbyssMage mage, GizmoType gizmoType)
    {
        if (mage == null || (!mage.ShowBattleRangesInScene && !IsMageHierarchySelected(mage)))
        {
            return;
        }

        DrawHoverAreaGizmo(mage.ProjectileHoverAreaCenter1, mage.ProjectileHoverAreaSize1, "Projectile Hover Area 1");
        DrawHoverAreaGizmo(mage.ProjectileHoverAreaCenter2, mage.ProjectileHoverAreaSize2, "Projectile Hover Area 2");
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

    private static bool IsMageHierarchySelected(Enemy_AbyssMage mage)
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

    private static void DrawHoverAreaGizmo(Vector3 center, Vector2 size, string label = "Spell Hover Area")
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

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (size.y * .5f + .12f), $"{label} / {size.x:0.##} x {size.y:0.##}", labelStyle);
    }

    private static void DrawTeleportAreaOutline(Vector3 center, Vector2 size)
    {
        Enemy_AbyssMageTeleportAreaAnchor.DrawOutline(center, size);
    }

    private static void DrawTeleportTriggerRanges(Enemy_AbyssMage mage)
    {
        if (mage == null)
        {
            return;
        }

        Enemy_AbyssMageTeleportTriggerAnchor[] anchors = mage.GetComponentsInChildren<Enemy_AbyssMageTeleportTriggerAnchor>(true);
        for (int i = 0; i < anchors.Length; i++)
        {
            Enemy_AbyssMageTeleportTriggerAnchor anchor = anchors[i];
            if (anchor == null)
            {
                continue;
            }

            Enemy_AbyssMageTeleportTriggerAnchor.DrawRange(anchor.transform.position, anchor.Radius, anchor.name);
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

public static class EnemyAbyssMageSceneConfigurator
{
    private const string BossSpritePath = "Assets/Graphics/Characters/Boss/AbyssMage/Enemy_AbyssMage.png";
    private const string BossControllerPath = "Assets/Animations/AnimatorControllers/Characters/Boss/Enemy_AbyssMage.controller";
    private const string BossFireballPrefabPath = "Assets/Prefabs/Enemy/Boss/Enemy_AbyssMage_Fireball.prefab";

    private static readonly string[] SpellStartPointNames =
    {
        "SpellStartPoint1",
        "SpellStartPoint2"
    };

    private static readonly string[] HoverAreaNames =
    {
        "ProjectileHoverArea1",
        "ProjectileHoverArea2"
    };

    private static readonly string[] TeleportTriggerNames =
    {
        "TeleportTriggerRange_A",
        "TeleportTriggerRange_B",
        "TeleportTriggerRange_C"
    };

    [MenuItem("Tools/Enemy/Configure Selected Abyss Mage Instance")]
    public static void ConfigureSelectedAbyssMageInstance()
    {
        GameObject root = FindSelectedAbyssMageRoot();
        if (root == null)
        {
            Debug.LogError("Select the Abyss Mage hierarchy root first.");
            return;
        }

        ConfigureInstance(root);
    }

    public static void ConfigureInstance(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(root))
        {
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Configure Abyss Mage Instance");

        root.name = "Enemy_AbyssMage";
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            root.layer = enemyLayer;
        }

        Enemy_Mage legacyMage = root.GetComponent<Enemy_Mage>();
        if (legacyMage != null)
        {
            Undo.DestroyObjectImmediate(legacyMage);
        }

        Enemy_AbyssMage mage = root.GetComponent<Enemy_AbyssMage>();
        if (mage == null)
        {
            mage = Undo.AddComponent<Enemy_AbyssMage>(root);
        }

        Enemy_Healthy health = root.GetComponent<Enemy_Healthy>();
        if (health == null)
        {
            health = Undo.AddComponent<Enemy_Healthy>(root);
        }

        Animator animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BossControllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }

            SpriteRenderer spriteRenderer = animator.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Sprite bossSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BossSpritePath);
                if (bossSprite != null)
                {
                    spriteRenderer.sprite = bossSprite;
                }

                spriteRenderer.sortingLayerName = "Enemy";
                spriteRenderer.sortingOrder = -1;
            }
        }

        GameObject fireballPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossFireballPrefabPath);

        Transform targetCheck = EnsureChild(root.transform, "TargetCheck", Vector3.zero);
        Transform behindCheck = EnsureChild(root.transform, "BehindCheck", new Vector3(-0.93f, -0.73f, 0f));
        Transform primaryWallCheck = EnsureChild(root.transform, "PrimaryWallCheck", new Vector3(0.62f, -0.929f, 0f));
        Transform secondaryWallCheck = EnsureChild(root.transform, "SecondaryWallCheck", new Vector3(0.383f, -0.385f, 0f));
        Transform teleportArea = EnsureTeleportArea(root.transform);
        Transform spellStartPoint1 = EnsureChild(root.transform, SpellStartPointNames[0], new Vector3(-0.38f, 0.46f, 0f));
        Transform spellStartPoint2 = EnsureChild(root.transform, SpellStartPointNames[1], new Vector3(0.38f, 0.46f, 0f));
        Transform hoverArea1 = EnsureHoverArea(root.transform, HoverAreaNames[0], new Vector3(-0.85f, 2.33f, 0f));
        Transform hoverArea2 = EnsureHoverArea(root.transform, HoverAreaNames[1], new Vector3(0.85f, 2.33f, 0f));

        EnsureTeleportTriggers(root.transform);

        SerializedObject mageSo = new SerializedObject(mage);
        SetObjectReference(mageSo, "playerCheck", targetCheck);
        SetObjectReference(mageSo, "spellPrefab", fireballPrefab);
        SetObjectReference(mageSo, "spellStartPosition1", spellStartPoint1);
        SetObjectReference(mageSo, "spellStartPosition2", spellStartPoint2);
        SetObjectReference(mageSo, "projectileHoverAreaAnchor1", hoverArea1);
        SetObjectReference(mageSo, "projectileHoverAreaAnchor2", hoverArea2);
        SetObjectReference(mageSo, "teleportAreaAnchor", teleportArea);
        SetObjectReference(mageSo, "behindCollisionCheck", behindCheck);
        SetObjectReference(mageSo, "primaryWallCheck", primaryWallCheck);
        SetObjectReference(mageSo, "secondaryWallCheck", secondaryWallCheck);

        SetInteger(mageSo, "maxHealth", 60);
        SetBool(mageSo, "canTakeDamage", true);
        SetString(mageSo, "questTargetId", "enemy_abyss_mage");
        SetFloat(mageSo, "battleMoveSpeed", 4f);
        SetFloat(mageSo, "attackDistance", 1.3f);
        SetFloat(mageSo, "spellCastDistance", 4.5f);
        SetFloat(mageSo, "attackCooldown", 0.5f);
        SetFloat(mageSo, "spellAttackCooldown", 5f);
        SetBool(mageSo, "showBattleRangesInScene", false);
        SetBool(mageSo, "canChasePlayer", true);
        SetFloat(mageSo, "battleTimeDuration", 5f);
        SetFloat(mageSo, "minRetreatDistance", 1f);
        SetFloat(mageSo, "battleStopDistance", 0.08f);
        SetVector2(mageSo, "retreatVelocity", new Vector2(7f, 4f));
        SetFloat(mageSo, "retreatCooldown", 4f);
        SetFloat(mageSo, "retreatMaxDistance", 8f);
        SetFloat(mageSo, "retreatSpeed", 15f);
        SetVector2(mageSo, "teleportAreaSize", new Vector2(7f, 3f));
        SetInteger(mageSo, "teleportMaxPlacementAttempts", 18);
        SetFloat(mageSo, "teleportGroundSearchHeight", 2f);
        SetFloat(mageSo, "teleportGroundSearchDistance", 8f);
        SetInteger(mageSo, "teleportImageEchoCount", 6);
        SetFloat(mageSo, "teleportImageEchoLifetime", 0.3f);
        SetFloat(mageSo, "teleportPostDelay", 0.06f);
        SetFloat(mageSo, "onHitTeleportInitialChance", 5f);
        SetFloat(mageSo, "onHitTeleportChanceIncrement", 5f);
        SetFloat(mageSo, "rangeTeleportInitialChance", 6f);
        SetFloat(mageSo, "rangeTeleportChanceIncrement", 2f);
        SetFloat(mageSo, "rangeTeleportCheckInterval", 1f);
        SetFloat(mageSo, "stunnedDuration", 1f);
        SetVector2(mageSo, "stunnedVelocity", new Vector2(7f, 7f));
        SetBool(mageSo, "canBeStunned", true);
        SetFloat(mageSo, "idleDurationMin", 2f);
        SetFloat(mageSo, "idleDurationMax", 2f);
        SetFloat(mageSo, "moveDurationMin", 2f);
        SetFloat(mageSo, "moveDurationMax", 2f);
        SetFloat(mageSo, "patrolTurnChance", 0.5f);
        SetFloat(mageSo, "patrolTurnDelay", 0.15f);
        SetFloat(mageSo, "moveSpeed", 1.4f);
        SetFloat(mageSo, "moveAnimSpeedMultiplier", 1f);
        SetFloat(mageSo, "playerCheckDistance", 10f);
        SetFloat(mageSo, "chaseVerticalDistance", 3f);
        SetFloat(mageSo, "loseSightDuration", 3f);
        SetInteger(mageSo, "amountToCast", 2);
        SetFloat(mageSo, "spellCastCooldown", 0.3f);
        SetFloat(mageSo, "projectileHoverArrivalDuration", 0.22f);
        SetFloat(mageSo, "projectileHoverMinSeparation", 0.72f);
        SetInteger(mageSo, "projectileHoverMaxPlacementAttempts", 24);
        SetBool(mageSo, "hasRecoveryAnimation", true);
        SetBool(mageSo, "canBeKnockedBack", true);
        SetString(mageSo, "attackAnimationState", "abyssMageAttack");
        SetString(mageSo, "idleAnimationState", "abyssMageIdle");
        SetString(mageSo, "moveAnimationState", "abyssMageMove");
        SetString(mageSo, "battleAnimationState", "abyssMageBattle - idle/move");
        SetString(mageSo, "stunnedAnimationState", "abyssMageStunned");
        SetString(mageSo, "stunRecoveryAnimationState", "abyssMageStunRecovery");
        SetString(mageSo, "spellCastAnimationState", "abyssMageSpellCast");
        SetString(mageSo, "spellCastPerformedAnimationState", "abyssMageSpellCast_performed");
        SetFloat(mageSo, "deadFallSpeed", 2.25f);
        SetFloat(mageSo, "deadSlideSpeed", 0.65f);
        SetFloat(mageSo, "deadSlideAcceleration", 1.2f);
        SetFloat(mageSo, "deadDropThroughDelay", 0f);
        SetFloat(mageSo, "deadDisappearDelay", 4f);
        SetFloat(mageSo, "deadFallAngle", 90f);
        SetLayerMask(mageSo, "whatIsPlayer", LayerMask.GetMask("Player"));
        mageSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject healthSo = new SerializedObject(health);
        SetInteger(healthSo, "maxHealth", 60);
        SetBool(healthSo, "canTakeDamage", true);
        healthSo.ApplyModifiedPropertiesWithoutUndo();

        Entity_Combat combat = root.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInteger(combatSo, "damage", 16);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(combat);
        }

        ConfigureHoverAreaComponent(hoverArea1, new Vector2(4f, 1.8f));
        ConfigureHoverAreaComponent(hoverArea2, new Vector2(4f, 1.8f));
        ConfigureTeleportAreaComponent(teleportArea, new Vector2(7f, 3f));
        ConfigureTeleportTriggerComponent(root.transform, TeleportTriggerNames[0], new Vector3(-2.2f, 0.2f, 0f), 2f);
        ConfigureTeleportTriggerComponent(root.transform, TeleportTriggerNames[1], new Vector3(0f, 0.2f, 0f), 2f);
        ConfigureTeleportTriggerComponent(root.transform, TeleportTriggerNames[2], new Vector3(2.2f, 0.2f, 0f), 2f);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(mage);
        EditorUtility.SetDirty(health);
        EditorSceneManager.MarkSceneDirty(root.scene);
    }

    private static GameObject FindSelectedAbyssMageRoot()
    {
        if (Selection.activeGameObject != null)
        {
            GameObject selected = Selection.activeGameObject;
            Enemy_AbyssMage abyssMage = selected.GetComponentInParent<Enemy_AbyssMage>(true);
            if (abyssMage != null)
            {
                return abyssMage.gameObject;
            }

            Enemy_Mage mage = selected.GetComponentInParent<Enemy_Mage>(true);
            if (mage != null)
            {
                return mage.gameObject;
            }

            Transform root = selected.transform.root;
            if (root != null && root.name == "Enemy_AbyssMage")
            {
                return root.gameObject;
            }

            return root != null ? root.gameObject : selected;
        }

        Enemy_AbyssMage abyssMageInScene = UnityEngine.Object.FindObjectOfType<Enemy_AbyssMage>(true);
        if (abyssMageInScene != null)
        {
            return abyssMageInScene.gameObject;
        }

        Enemy_Mage mageInScene = UnityEngine.Object.FindObjectOfType<Enemy_Mage>(true);
        return mageInScene != null ? mageInScene.gameObject : null;
    }

    private static Transform EnsureTeleportArea(Transform root)
    {
        Transform teleportArea = EnsureChild(root, "TeleportArea", new Vector3(0f, 0.8f, 0f));
        ConfigureTeleportAreaComponent(teleportArea, new Vector2(7f, 3f));
        return teleportArea;
    }

    private static Transform EnsureHoverArea(Transform root, string childName, Vector3 localPosition)
    {
        Transform hoverArea = EnsureChild(root, childName, localPosition);
        ConfigureHoverAreaComponent(hoverArea, new Vector2(4f, 1.8f));
        return hoverArea;
    }

    private static void EnsureTeleportTriggers(Transform root)
    {
        ConfigureTeleportTriggerComponent(root, TeleportTriggerNames[0], new Vector3(-2.2f, 0.2f, 0f), 2f);
        ConfigureTeleportTriggerComponent(root, TeleportTriggerNames[1], new Vector3(0f, 0.2f, 0f), 2f);
        ConfigureTeleportTriggerComponent(root, TeleportTriggerNames[2], new Vector3(2.2f, 0.2f, 0f), 2f);
    }

    private static void ConfigureTeleportTriggerComponent(Transform root, string childName, Vector3 localPosition, float radius)
    {
        Transform trigger = EnsureChild(root, childName, localPosition);
        Enemy_AbyssMageTeleportTriggerAnchor anchor = trigger.GetComponent<Enemy_AbyssMageTeleportTriggerAnchor>();
        if (anchor == null)
        {
            anchor = Undo.AddComponent<Enemy_AbyssMageTeleportTriggerAnchor>(trigger.gameObject);
        }

        SerializedObject so = new SerializedObject(anchor);
        SetFloat(so, "radius", radius);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureHoverAreaComponent(Transform hoverArea, Vector2 size)
    {
        if (hoverArea == null)
        {
            return;
        }

        Enemy_AbyssMageProjectileHoverAreaAnchor anchor = hoverArea.GetComponent<Enemy_AbyssMageProjectileHoverAreaAnchor>();
        if (anchor == null)
        {
            anchor = Undo.AddComponent<Enemy_AbyssMageProjectileHoverAreaAnchor>(hoverArea.gameObject);
        }

        SerializedObject so = new SerializedObject(anchor);
        SetVector2(so, "hoverAreaSize", size);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureTeleportAreaComponent(Transform teleportArea, Vector2 size)
    {
        if (teleportArea == null)
        {
            return;
        }

        Enemy_AbyssMageTeleportAreaAnchor anchor = teleportArea.GetComponent<Enemy_AbyssMageTeleportAreaAnchor>();
        if (anchor == null)
        {
            anchor = Undo.AddComponent<Enemy_AbyssMageTeleportAreaAnchor>(teleportArea.gameObject);
        }

        SerializedObject so = new SerializedObject(anchor);
        SetVector2(so, "areaSize", size);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Transform EnsureChild(Transform root, string childName, Vector3 localPosition)
    {
        Transform child = root.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(childObject, $"Create {childName}");
            child = childObject.transform;
            child.SetParent(root, false);
        }

        child.name = childName;
        child.localPosition = localPosition;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;
        if (child.gameObject.layer != root.gameObject.layer)
        {
            child.gameObject.layer = root.gameObject.layer;
        }

        return child;
    }

    private static void SetObjectReference(SerializedObject so, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetString(SerializedObject so, string propertyName, string value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }

    private static void SetBool(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetInteger(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static void SetVector2(SerializedObject so, string propertyName, Vector2 value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.vector2Value = value;
        }
    }

    private static void SetLayerMask(SerializedObject so, string propertyName, LayerMask value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value.value;
        }
    }
}
