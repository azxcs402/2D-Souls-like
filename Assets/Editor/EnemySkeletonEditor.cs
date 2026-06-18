using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_Skeleton))]
public class EnemySkeletonEditor : Editor
{
    private const string SearchPrefsKey = "EnemySkeletonEditor.Search";

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
            "Patrol Info",
            "Idle and roaming timing for non-combat behavior.",
            new SearchableField("idleDurationMin", "Idle Duration Min", "idle"),
            new SearchableField("idleDurationMax", "Idle Duration Max", "idle"),
            new SearchableField("moveDurationMin", "Move Duration Min", "move"),
            new SearchableField("moveDurationMax", "Move Duration Max", "move"),
            new SearchableField("patrolTurnChance", "Patrol Turn Chance", "turn")
        ),
        new SearchableSection(
            "Edge Check",
            "Forward ground edge sensing used during patrol.",
            new SearchableField("useEdgeCheck", "Use Edge Check", "edge"),
            new SearchableField("edgeCheckForwardOffset", "Edge Check Forward Offset", "edge"),
            new SearchableField("edgeCheckDistance", "Edge Check Distance", "edge")
        ),
        new SearchableSection(
            "Target Detection",
            "Player detection and line-of-sight tuning.",
            new SearchableField("whatIsPlayer", "What Is Player", "player mask"),
            new SearchableField("frontSightDistance", "Front Sight Distance", "sight"),
            new SearchableField("backSightDistance", "Back Sight Distance", "sight"),
            new SearchableField("chaseVerticalDistance", "Chase Vertical Distance", "vertical"),
            new SearchableField("maxSeeThroughWallDistance", "Max See Through Wall Distance", "wall"),
            new SearchableField("wallThicknessSampleDistance", "Wall Thickness Sample Distance", "wall"),
            new SearchableField("battleStopDistance", "Battle Stop Distance", "stop"),
            new SearchableField("loseSightDuration", "Lose Sight Duration", "lose sight"),
            new SearchableField("attackCooldown", "Attack Cooldown", "attack cooldown"),
            new SearchableField("showDetectionGizmos", "Show Detection Gizmos", "gizmos")
        ),
        new SearchableSection(
            "Movement Info",
            "Movement speed settings for patrol and battle movement.",
            new SearchableField("skeletonMoveSpeed", "Skeleton Move Speed", "move"),
            new SearchableField("battleMoveSpeed", "Battle Move Speed", "battle move"),
            new SearchableField("battleMoveSpeedMultiplier", "Battle Move Speed Multiplier", "battle move multiplier", true)
        ),
        new SearchableSection(
            "Attack Info",
            "Attack range, motion, damage, and knockback tuning.",
            new SearchableField("skeletonAttackRange", "Skeleton Attack Range", "attack range"),
            new SearchableField("skeletonAttackMoveDistance", "Skeleton Attack Move Distance", "attack move"),
            new SearchableField("skeletonAttackKnockbackForce", "Skeleton Attack Knockback Force", "knockback", true),
            new SearchableField("skeletonAttackData", "Skeleton Attack Data", "attack data"),
            new SearchableField("skeletonAttackMoveDuration", "Skeleton Attack Move Duration", "attack move"),
            new SearchableField("skeletonAttackMoveXDelay", "Skeleton Attack Move X Delay", "move delay"),
            new SearchableField("skeletonAttackMoveYDelay", "Skeleton Attack Move Y Delay", "move delay"),
            new SearchableField("skeletonTurnDelay", "Skeleton Turn Delay", "turn delay"),
            new SearchableField("skeletonContactDamage", "Skeleton Contact Damage", "damage"),
            new SearchableField("canSkeletonBeKnockedBack", "Can Skeleton Be Knocked Back", "knockback"),
            new SearchableField("canSkeletonBeKnockedBackDuringAttack", "Can Skeleton Be Knocked Back During Attack", "knockback")
        ),
        new SearchableSection(
            "Stunned Info",
            "Stun displacement and animator parameters.",
            new SearchableField("stunnedMoveDistance", "Stunned Move Distance", "stunned"),
            new SearchableField("stunnedBoolParameter", "Stunned Bool Parameter", "stunned"),
            new SearchableField("stunnedAnimationState", "Stunned Animation State", "stunned"),
            new SearchableField("stunAttackRecoveryDelay", "Stun Attack Recovery Delay", "stun recovery")
        ),
        new SearchableSection(
            "Death Info",
            "Death fall, slide, and disappearance tuning.",
            new SearchableField("deadFallSpeed", "Dead Fall Speed", "death"),
            new SearchableField("deadSlideSpeed", "Dead Slide Speed", "death"),
            new SearchableField("deadSlideAcceleration", "Dead Slide Acceleration", "death"),
            new SearchableField("deadDropThroughDelay", "Dead Drop Through Delay", "death"),
            new SearchableField("deadDisappearDelay", "Dead Disappear Delay", "death"),
            new SearchableField("deadFallAngle", "Dead Fall Angle", "death")
        ),
        new SearchableSection(
            "Animation State Names",
            "Animator state names used by the skeleton controller.",
            new SearchableField("idleAnimationState", "Idle Animation State", "idle"),
            new SearchableField("moveAnimationState", "Move Animation State", "move"),
            new SearchableField("battleAnimationState", "Battle Animation State", "battle")
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
        SearchableInspectorDrawer.DrawSearchBar(
            ref searchQuery,
            ref showOnlyMatches,
            SearchPrefsKey,
            "Enemy Skeleton Inspector Search",
            new[] { "Core", "Patrol", "Edge", "Target", "Move", "Attack", "Stunned", "Death" }
        );
        SearchableInspectorDrawer.DrawQuickFindButtons(
            SetSearch,
            "core",
            "patrol",
            "edge",
            "target",
            "move",
            "attack",
            "stunned",
            "death",
            "hazard"
        );

        EditorGUILayout.Space();
        SearchableInspectorDrawer.DrawSections(serializedObject, Sections, searchQuery, showOnlyMatches);

        serializedObject.ApplyModifiedProperties();
    }

    private void SetSearch(string value)
    {
        searchQuery = value;
        EditorPrefs.SetString(SearchPrefsKey, searchQuery ?? string.Empty);
        GUI.FocusControl(null);
    }
}
