using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_Reaper))]
public class EnemyReaperEditor : Editor
{
    private const string SearchPrefsKey = "EnemyReaperEditor.Search";

    private string searchQuery;
    private bool showOnlyMatches;

    private static readonly SearchableSection[] Sections =
    {
        new SearchableSection(
            "Core",
            "Enemy base settings. Use this for health, ground checks, wall checks, and hazard avoidance.",
            new SearchableField("maxHealth", "Max Health", "health hit points hp"),
            new SearchableField("canTakeDamage", "Can Take Damage", "damage invulnerable"),
            new SearchableField("groundCheckDistance", "Ground Check Distance", "ground floor check"),
            new SearchableField("whatIsGround", "What Is Ground", "ground layer mask"),
            new SearchableField("wallCheckDistance", "Wall Check Distance", "wall obstacle"),
            new SearchableField("wallCheckVerticalSpan", "Wall Check Vertical Span", "wall span"),
            new SearchableField("primaryWallCheck", "Primary Wall Check", "wall check transform"),
            new SearchableField("secondaryWallCheck", "Secondary Wall Check", "wall check transform"),
            new SearchableField("avoidPitAndSpikeHazards", "Avoid Pit And Spike Hazards", "hazard pit spike"),
            new SearchableField("hazardLookAheadOffset", "Hazard Look Ahead Offset", "hazard look ahead"),
            new SearchableField("hazardProbeRadius", "Hazard Probe Radius", "hazard probe"),
            new SearchableField("hazardProbeVerticalOffset", "Hazard Probe Vertical Offset", "hazard probe")
        ),
        new SearchableSection(
            "Battle",
            "Attack, chase, retreat, and general battle movement settings.",
            new SearchableField("battleMoveSpeed", "Battle Move Speed", "battle move speed"),
            new SearchableField("attackDistance", "Attack Distance", "melee attack range"),
            new SearchableField("attackRangeAnchor", "Attack Range Anchor", "scene handle melee range"),
            new SearchableField("spellCastDistance", "Spell Cast Distance", "spell range"),
            new SearchableField("spellCastRangeAnchor", "Spell Cast Range Anchor", "scene handle spell range"),
            new SearchableField("attackCooldown", "Attack Cooldown", "attack cooldown"),
            new SearchableField("canChasePlayer", "Can Chase Player", "chase follow"),
            new SearchableField("battleTimeDuration", "Battle Time Duration", "battle timer"),
            new SearchableField("minRetreatDistance", "Min Retreat Distance", "retreat"),
            new SearchableField("battleStopDistance", "Battle Stop Distance", "stop distance"),
            new SearchableField("retreatVelocity", "Retreat Velocity", "retreat speed vector"),
            new SearchableField("moveSpeed", "Move Speed", "walk patrol speed"),
            new SearchableField("moveAnimSpeedMultiplier", "Move Anim Speed Multiplier", "animation speed"),
            new SearchableField("chanceToTeleport", "Chance To Teleport", "teleport chance"),
            new SearchableField("spellCastStateCooldown", "Spell Cast State Cooldown", "spell cooldown")
        ),
        new SearchableSection(
            "Detection",
            "Player perception, vision anchor, and scene gizmo toggle.",
            new SearchableField("showDetectionGizmos", "Show Detection Gizmos", "gizmos"),
            new SearchableField("whatIsPlayer", "What Is Player", "player mask"),
            new SearchableField("playerCheck", "Player Check", "target check transform"),
            new SearchableField("visionAreaAnchor", "Vision Area Anchor", "vision anchor"),
            new SearchableField("playerCheckDistance", "Player Check Distance", "detection range sight"),
            new SearchableField("frontSightDistance", "Front Sight Distance", "front sight"),
            new SearchableField("backSightDistance", "Back Sight Distance", "back sight"),
            new SearchableField("chaseVerticalDistance", "Chase Vertical Distance", "vertical tolerance"),
            new SearchableField("loseSightDuration", "Lose Sight Duration", "lose sight")
        ),
        new SearchableSection(
            "Spell",
            "Reaper spell prefab, spawn cadence, and damage scaling.",
            new SearchableField("spellDamageScale", "Spell Damage Scale", "damage scale"),
            new SearchableField("spellCastPrefab", "Spell Cast Prefab", "spell prefab projectile"),
            new SearchableField("amountToCast", "Amount To Cast", "spell count"),
            new SearchableField("spellCastRate", "Spell Cast Rate", "spawn interval"),
            new SearchableField("playerOffsetPrediction", "Player Offset Prediction", "aim lead offset")
        ),
        new SearchableSection(
            "Stunned",
            "Counter and stun tuning.",
            new SearchableField("stunnedDuration", "Stunned Duration", "stun"),
            new SearchableField("stunnedVelocity", "Stunned Velocity", "stun knockback"),
            new SearchableField("canBeStunned", "Can Be Stunned", "stun counter"),
            new SearchableField("canBeKnockedBack", "Can Be Knocked Back", "knockback")
        ),
        new SearchableSection(
            "Teleport",
            "Teleport anchor and teleport placement tuning.",
            new SearchableField("teleportAreaAnchor", "Teleport Area Anchor", "teleport anchor"),
            new SearchableField("teleportAreaSize", "Teleport Area Size", "teleport size"),
            new SearchableField("teleportGroundSearchHeight", "Teleport Ground Search Height", "ground search"),
            new SearchableField("teleportGroundSearchDistance", "Teleport Ground Search Distance", "ground search"),
            new SearchableField("teleportMaxPlacementAttempts", "Teleport Max Placement Attempts", "placement attempts"),
            new SearchableField("teleportImageEchoCount", "Teleport Image Echo Count", "afterimage"),
            new SearchableField("teleportImageEchoLifetime", "Teleport Image Echo Lifetime", "afterimage"),
            new SearchableField("teleportPostDelay", "Teleport Post Delay", "teleport delay")
        ),
        new SearchableSection(
            "Attack Data",
            "Melee attack data used by the Reaper attack state.",
            new SearchableField("syncAttackHitboxWithTargetCheck", "Sync Attack Hitbox With TargetCheck", "sync targetcheck hitbox", true),
            new SearchableField("reaperAttackData", "Reaper Attack Data", "attack data hitbox"),
            new SearchableField("attackDamageWindowStartNormalized", "Attack Damage Window Start Normalized", "damage window start"),
            new SearchableField("attackDamageWindowEndNormalized", "Attack Damage Window End Normalized", "damage window end"),
            new SearchableField("attackDistance", "Attack Distance", "melee"),
            new SearchableField("attackRangeAnchor", "Attack Range Anchor", "melee anchor")
        ),
        new SearchableSection(
            "Death",
            "Death motion and disappearance settings.",
            new SearchableField("deadFallSpeed", "Dead Fall Speed", "death"),
            new SearchableField("deadSlideSpeed", "Dead Slide Speed", "death"),
            new SearchableField("deadSlideAcceleration", "Dead Slide Acceleration", "death"),
            new SearchableField("deadDropThroughDelay", "Dead Drop Through Delay", "death"),
            new SearchableField("deadDisappearDelay", "Dead Disappear Delay", "death"),
            new SearchableField("deadFallAngle", "Dead Fall Angle", "death")
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
        SearchableInspectorDrawer.DrawSearchBar(
            ref searchQuery,
            ref showOnlyMatches,
            SearchPrefsKey,
            "Enemy Reaper Inspector Search",
            new[] { "Core", "Battle", "Detection", "Spell", "Stunned", "Teleport", "Attack", "Death" }
        );
        SearchableInspectorDrawer.DrawQuickFindButtons(
            SetSearch,
            "core",
            "battle",
            "detection",
            "spell",
            "stunned",
            "teleport",
            "attack",
            "death"
        );

        EditorGUILayout.HelpBox(
            "Reaper attack damage window is defined by two normalized values: Start and End. Damage is only checked while the attack animation is inside this interval.",
            MessageType.Info
        );
        EditorGUILayout.HelpBox(
            "When Sync Attack Hitbox With TargetCheck is enabled, the Scene handle edits TargetCheck position and radius instead of reaperAttackData.",
            MessageType.None
        );

        EditorGUILayout.Space();
        SearchableInspectorDrawer.DrawSections(serializedObject, Sections, searchQuery, showOnlyMatches);

        serializedObject.ApplyModifiedProperties();
    }

    private void OnSceneGUI()
    {
        Enemy_Reaper reaper = (Enemy_Reaper)target;
        if (reaper == null)
        {
            return;
        }

        serializedObject.Update();
        reaper.EnsureBattleRangeAnchors();

        Transform attackAnchor = reaper.AttackRangeAnchor;
        Transform spellAnchor = reaper.SpellCastRangeAnchor;
        if (attackAnchor == null || spellAnchor == null)
        {
            return;
        }

        Vector3 bodyCenter = GetBodyCenter(reaper);
        float boxHeight = Mathf.Max(.1f, reaper.ChaseVerticalDistance * 2f);
        DrawRangeBox(bodyCenter, reaper.AttackDistance, boxHeight, new Color(1f, .45f, .15f, .12f), new Color(1f, .45f, .15f, 1f), $"Attack Range / {reaper.AttackDistance:0.##}");
        DrawRangeBox(bodyCenter, reaper.SpellCastDistance, boxHeight, new Color(.35f, .8f, 1f, .10f), new Color(.35f, .8f, 1f, 1f), $"Spell Trigger Range / {reaper.SpellCastDistance:0.##}");
        DrawReaperAttackHitbox(reaper);

        DrawRangeHandle(reaper, attackAnchor, spellAnchor, "Attack Range", new Color(1f, .45f, .15f, 1f));
        DrawRangeHandle(reaper, spellAnchor, attackAnchor, "Spell Cast Range", new Color(.35f, .8f, 1f, 1f));
    }

    private static Vector3 GetBodyCenter(Enemy_Reaper reaper)
    {
        CapsuleCollider2D collider2D = reaper.GetComponent<CapsuleCollider2D>();
        return collider2D != null ? collider2D.bounds.center : reaper.transform.position;
    }

    private static void DrawRangeBox(Vector3 center, float radius, float height, Color fillColor, Color wireColor, string label)
    {
        float width = Mathf.Max(.1f, radius * 2f);
        float clampedHeight = Mathf.Max(.1f, height);
        Vector3 half = new Vector3(width * .5f, clampedHeight * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (half.y + .12f), label, labelStyle);
    }

    private static void DrawRangeHandle(Enemy_Reaper reaper, Transform anchor, Transform otherAnchor, string undoName, Color color)
    {
        Vector3 handleWorld = anchor.position;
        Handles.color = color;
        Handles.DrawLine(reaper.transform.position, handleWorld);

        EditorGUI.BeginChangeCheck();
        Vector3 moved = Handles.FreeMoveHandle(handleWorld, 0.08f, Vector3.zero, Handles.SphereHandleCap);
        if (!EditorGUI.EndChangeCheck())
        {
            return;
        }

        Undo.RecordObject(anchor, $"Move {undoName}");
        Vector3 local = reaper.transform.InverseTransformPoint(moved);
        local.x = Mathf.Max(.1f, Mathf.Abs(local.x));
        local.y = 0f;
        local.z = 0f;
        anchor.localPosition = local;

        if (otherAnchor != null)
        {
            Vector3 otherLocal = otherAnchor.localPosition;
            float otherDistance = Mathf.Abs(otherLocal.x);
            if (string.Equals(undoName, "Attack Range", System.StringComparison.OrdinalIgnoreCase) && otherDistance < local.x)
            {
                Undo.RecordObject(otherAnchor, "Clamp Spell Cast Range");
                otherLocal.x = local.x;
                otherLocal.y = 0f;
                otherLocal.z = 0f;
                otherAnchor.localPosition = otherLocal;
            }
            else if (string.Equals(undoName, "Spell Cast Range", System.StringComparison.OrdinalIgnoreCase) && otherDistance > local.x)
            {
                Undo.RecordObject(otherAnchor, "Clamp Attack Range");
                otherLocal.x = local.x;
                otherLocal.y = 0f;
                otherLocal.z = 0f;
                otherAnchor.localPosition = otherLocal;
            }
        }

        EditorUtility.SetDirty(anchor);
        EditorUtility.SetDirty(reaper);
    }

    private void DrawReaperAttackHitbox(Enemy_Reaper reaper)
    {
        Entity_Combat combat = reaper.GetComponent<Entity_Combat>();
        if (combat == null)
        {
            return;
        }

        SerializedProperty syncProperty = serializedObject.FindProperty("syncAttackHitboxWithTargetCheck");
        bool syncWithTargetCheck = syncProperty != null && syncProperty.boolValue;

        Color fillColor = new Color(.35f, .8f, 1f, .16f);
        Color wireColor = new Color(.35f, .8f, 1f, 1f);

        if (syncWithTargetCheck)
        {
            if (DrawSyncedReaperAttackHitbox(reaper, combat, fillColor, wireColor))
            {
                return;
            }
        }

        SerializedProperty attackDataProperty = serializedObject.FindProperty("reaperAttackData");
        if (attackDataProperty == null)
        {
            return;
        }

        SerializedProperty offsetProperty = attackDataProperty.FindPropertyRelative("targetCheckOffset");
        SerializedProperty radiusProperty = attackDataProperty.FindPropertyRelative("targetCheckRadius");
        if (offsetProperty == null || radiusProperty == null)
        {
            return;
        }

        Vector2 offset = offsetProperty.vector2Value;
        float radius = Mathf.Max(.01f, radiusProperty.floatValue);
        int facing = reaper.FacingDirection == 0 ? 1 : reaper.FacingDirection;
        Vector3 center = reaper.transform.position + new Vector3(offset.x * facing, offset.y, 0f);

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
        Handles.Label(center + Vector3.up * (radius + .12f), $"Reaper Attack / Radius {radius:0.##}", labelStyle);

        Handles.color = wireColor;
        Handles.DrawLine(reaper.transform.position, center);

        EditorGUI.BeginChangeCheck();
        Vector3 movedCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(reaper, "Move Reaper Attack Offset");
            offset.x = (movedCenter.x - reaper.transform.position.x) * facing;
            offset.y = movedCenter.y - reaper.transform.position.y;
            offsetProperty.vector2Value = offset;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(reaper);
        }

        Vector3 radiusHandleStart = center + Vector3.right * radius;
        EditorGUI.BeginChangeCheck();
        Vector3 radiusHandleMoved = Handles.FreeMoveHandle(radiusHandleStart, 0.08f, Vector3.zero, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(reaper, "Resize Reaper Attack Radius");
            float newRadius = Mathf.Max(.01f, Vector2.Distance(center, radiusHandleMoved));
            radiusProperty.floatValue = newRadius;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(reaper);
        }
    }

    private bool DrawSyncedReaperAttackHitbox(Enemy_Reaper reaper, Entity_Combat combat, Color fillColor, Color wireColor)
    {
        Transform targetCheck = combat.TargetCheck;
        if (targetCheck == null)
        {
            return false;
        }

        SerializedObject combatSerialized = new SerializedObject(combat);
        combatSerialized.Update();
        SerializedProperty targetCheckRadiusProperty = combatSerialized.FindProperty("targetCheckRadius");
        if (targetCheckRadiusProperty == null)
        {
            return false;
        }

        float radius = Mathf.Max(.01f, targetCheckRadiusProperty.floatValue);
        Vector3 center = targetCheck.position;

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
        Handles.Label(center + Vector3.up * (radius + .12f), $"TargetCheck / Radius {radius:0.##}", labelStyle);

        Handles.color = wireColor;
        Handles.DrawLine(reaper.transform.position, center);

        EditorGUI.BeginChangeCheck();
        Vector3 movedCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(targetCheck, "Move TargetCheck");
            Vector3 localPosition = reaper.transform.InverseTransformPoint(movedCenter);
            localPosition.z = 0f;
            targetCheck.localPosition = localPosition;
            EditorUtility.SetDirty(targetCheck);
            EditorUtility.SetDirty(combat);
        }

        Vector3 radiusHandleStart = center + Vector3.right * radius;
        EditorGUI.BeginChangeCheck();
        Vector3 radiusHandleMoved = Handles.FreeMoveHandle(radiusHandleStart, 0.08f, Vector3.zero, Handles.SphereHandleCap);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(combat, "Resize TargetCheck Radius");
            float newRadius = Mathf.Max(.01f, Vector2.Distance(center, radiusHandleMoved));
            targetCheckRadiusProperty.floatValue = newRadius;
            combatSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(combat);
        }

        return true;
    }

    private void SetSearch(string value)
    {
        searchQuery = value;
        EditorPrefs.SetString(SearchPrefsKey, searchQuery ?? string.Empty);
        GUI.FocusControl(null);
    }
}
