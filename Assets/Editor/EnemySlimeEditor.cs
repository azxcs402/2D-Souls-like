using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy_Slime))]
public class EnemySlimeEditor : Editor
{
    private const string SearchPrefsKey = "EnemySlimeEditor.Search";

    private string searchQuery;
    private bool showOnlyMatches;

    private static readonly SearchableSection[] Sections =
    {
        new SearchableSection(
            "Battle Info",
            "Core battle tuning, chase range, and retreat behavior.",
            new SearchableField("battleMoveSpeed", "Battle Move Speed", "battle speed"),
            new SearchableField("attackDistance", "Attack Distance", "attack range"),
            new SearchableField("attackCooldown", "Attack Cooldown", "attack cooldown"),
            new SearchableField("canChasePlayer", "Can Chase Player", "chase"),
            new SearchableField("battleTimeDuration", "Battle Time Duration", "battle time"),
            new SearchableField("minRetreatDistance", "Min Retreat Distance", "retreat"),
            new SearchableField("battleStopDistance", "Battle Stop Distance", "stop distance"),
            new SearchableField("retreatVelocity", "Retreat Velocity", "retreat velocity")
        ),
        new SearchableSection(
            "Stunned Info",
            "Stun duration, launch velocity, and stun enable toggle.",
            new SearchableField("stunnedDuration", "Stunned Duration", "stun"),
            new SearchableField("stunnedVelocity", "Stunned Velocity", "stun velocity"),
            new SearchableField("canBeStunned", "Can Be Stunned", "stun"),
            new SearchableField("stunAttackRecoveryDelay", "Stun Attack Recovery Delay", "stun recovery")
        ),
        new SearchableSection(
            "Patrol Info",
            "Non-combat idle and movement timing.",
            new SearchableField("idleDurationMin", "Idle Duration Min", "idle"),
            new SearchableField("idleDurationMax", "Idle Duration Max", "idle"),
            new SearchableField("moveDurationMin", "Move Duration Min", "move"),
            new SearchableField("moveDurationMax", "Move Duration Max", "move"),
            new SearchableField("patrolTurnChance", "Patrol Turn Chance", "turn"),
            new SearchableField("patrolTurnDelay", "Patrol Turn Delay", "turn delay")
        ),
        new SearchableSection(
            "Movement Info",
            "Base movement speed and movement animation speed multiplier.",
            new SearchableField("moveSpeed", "Move Speed", "speed"),
            new SearchableField("moveAnimSpeedMultiplier", "Move Anim Speed Multiplier", "animation speed")
        ),
        new SearchableSection(
            "Vision Info",
            "Player detection mask, front/back sight, and sight occlusion.",
            new SearchableField("showDetectionGizmos", "Show Detection Gizmos", "gizmos"),
            new SearchableField("whatIsPlayer", "What Is Player", "player mask"),
            new SearchableField("frontSightDistance", "Front Sight Distance", "front sight"),
            new SearchableField("backSightDistance", "Back Sight Distance", "back sight"),
            new SearchableField("chaseVerticalDistance", "Chase Vertical Distance", "vertical"),
            new SearchableField("maxSeeThroughWallDistance", "Max See Through Wall Distance", "wall occlusion"),
            new SearchableField("wallThicknessSampleDistance", "Wall Thickness Sample Distance", "wall sampling"),
            new SearchableField("loseSightDuration", "Lose Sight Duration", "lose sight")
        ),
        new SearchableSection(
            "Spawn Info",
            "Slime splitting / spawn-on-death tuning.",
            new SearchableField("slimeToCreatePrefab", "Slime To Create Prefab", "spawn prefab"),
            new SearchableField("amountOfSlimesToCreate", "Amount Of Slimes To Create", "spawn count"),
            new SearchableField("newSlimeVelocity", "New Slime Velocity", "spawn velocity"),
            new SearchableField("hasRecoveryAnimation", "Has Recovery Animation", "recovery"),
            new SearchableField("canBeKnockedBack", "Can Be Knocked Back", "knockback"),
            new SearchableField("slimeSpriteFacesLeftByDefault", "Slime Sprite Faces Left By Default", "flip direction")
        ),
        new SearchableSection(
            "Split Info",
            "Recursive split control and child tuning.",
            new SearchableField("splitOnDeath", "Split On Death", "split"),
            new SearchableField("maxSplitGenerations", "Max Split Generations", "generation"),
            new SearchableField("splitChildScaleMultiplier", "Split Child Scale Multiplier", "scale"),
            new SearchableField("splitChildHealthMultiplier", "Split Child Health Multiplier", "health"),
            new SearchableField("splitChildDamageMultiplier", "Split Child Damage Multiplier", "damage"),
            new SearchableField("splitSpawnHorizontalOffset", "Split Spawn Horizontal Offset", "offset"),
            new SearchableField("splitSpawnVerticalOffset", "Split Spawn Vertical Offset", "offset")
        ),
        new SearchableSection(
            "Stunned Collider",
            "Dedicated collider for stun state adjustments.",
            new SearchableField("stunnedCollider", "Stunned Collider", "stunned collider")
        ),
        new SearchableSection(
            "Attack Info",
            "Attack hitbox data and animation state names.",
            new SearchableField("slimeAttackData", "Slime Attack Data", "attack data"),
            new SearchableField("attackAnimationState", "Attack Animation State", "attack"),
            new SearchableField("idleAnimationState", "Idle Animation State", "idle"),
            new SearchableField("moveAnimationState", "Move Animation State", "move"),
            new SearchableField("battleAnimationState", "Battle Animation State", "battle"),
            new SearchableField("stunnedAnimationState", "Stunned Animation State", "stunned"),
            new SearchableField("stunRecoveryAnimationState", "Stun Recovery Animation State", "stun recovery")
        ),
        new SearchableSection(
            "Death Info",
            "Death fall, slide, drop-through, and disappear tuning.",
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
            "Enemy Slime Inspector Search",
            new[] { "Battle", "Stun", "Patrol", "Vision", "Spawn", "Split", "Attack", "Death" }
        );
        SearchableInspectorDrawer.DrawQuickFindButtons(
            SetSearch,
            "battle",
            "stun",
            "patrol",
            "vision",
            "spawn",
            "split",
            "attack",
            "death"
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
