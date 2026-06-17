using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Player))]
public class PlayerEditor : Editor
{
    private const string SearchPrefsKey = "PlayerEditor.Search";

    private string searchQuery;
    private bool showOnlyMatches;

    private static readonly SearchableSection[] Sections =
    {
        new SearchableSection(
            "Move Info",
            "Basic ground and air movement tuning.",
            new SearchableField("moveSpeed", "Move Speed", "movement run walk"),
            new SearchableField("airMoveSpeedMultiplier", "Air Move Speed Multiplier", "air movement")
        ),
        new SearchableSection(
            "Jump Info",
            "Jump impulse and related tuning.",
            new SearchableField("jumpForce", "Jump Force", "jump hop")
        ),
        new SearchableSection(
            "Wall Slide Info",
            "Wall slide and wall cling movement.",
            new SearchableField("wallSlideSpeed", "Wall Slide Speed", "wall slide"),
            new SearchableField("wallSlideVisualOffset", "Wall Slide Visual Offset", "visual offset"),
            new SearchableField("wallSlideNoInputDropTime", "Wall Slide No Input Drop Time", "drop delay")
        ),
        new SearchableSection(
            "Wall Hold Info",
            "Wall hold enable state and hold duration.",
            new SearchableField("wallHoldEnabled", "Wall Hold Enabled", "wall hold"),
            new SearchableField("wallHoldHasDuration", "Wall Hold Has Duration", "duration"),
            new SearchableField("wallHoldDuration", "Wall Hold Duration", "hold time")
        ),
        new SearchableSection(
            "Wall Jump Info",
            "Wall jump force and air attack window.",
            new SearchableField("wallJumpForce", "Wall Jump Force", "wall jump"),
            new SearchableField("wallJumpDuration", "Wall Jump Duration", "wall jump"),
            new SearchableField("wallJumpInputGraceTime", "Wall Jump Input Grace Time", "grace"),
            new SearchableField("wallJumpAirAttackEnabled", "Wall Jump Air Attack Enabled", "air attack")
        ),
        new SearchableSection(
            "Dash Info",
            "Dash movement and enemy collision options.",
            new SearchableField("dashDistance", "Dash Distance", "dash"),
            new SearchableField("dashDuration", "Dash Duration", "dash"),
            new SearchableField("dashCooldown", "Dash Cooldown", "dash"),
            new SearchableField("wallDashAwayFromWallEnabled", "Wall Dash Away From Wall Enabled", "wall dash"),
            new SearchableField("ignoreEnemyCollisionDuringDash", "Ignore Enemy Collision During Dash", "enemy collision")
        ),
        new SearchableSection(
            "Basic Attack Info",
            "Ground combo tuning, animation names and motion data.",
            new SearchableField("basicAttackAnimationNames", "Basic Attack Animation Names", "combo attack"),
            new SearchableField("airAttackAnimationNames", "Air Attack Animation Names", "air combo attack"),
            new SearchableField("basicAttackAnimationSpeeds", "Basic Attack Animation Speeds", "speed"),
            new SearchableField("basicAttackMoveDistances", "Basic Attack Move Distances", "move distance"),
            new SearchableField("basicAttackKnockbackForces", "Basic Attack Knockback Forces", "knockback", true),
            new SearchableField("basicAttackData", "Basic Attack Data", "hitbox radius"),
            new SearchableField("basicAttackComboInputLeftWindows", "Basic Attack Combo Input Left Windows", "combo"),
            new SearchableField("basicAttackComboInputRightWindows", "Basic Attack Combo Input Right Windows", "combo"),
            new SearchableField("basicAttackTurnInputLeftWindows", "Basic Attack Turn Input Left Windows", "turn"),
            new SearchableField("basicAttackTurnInputRightWindows", "Basic Attack Turn Input Right Windows", "turn"),
            new SearchableField("basicAttackDashComboInputWindow", "Basic Attack Dash Combo Input Window", "dash combo"),
            new SearchableField("basicAttackDashTurnInputWindow", "Basic Attack Dash Turn Input Window", "dash turn"),
            new SearchableField("basicAttackLoopCooldown", "Basic Attack Loop Cooldown", "loop"),
            new SearchableField("basicAttackMoveDuration", "Basic Attack Move Duration", "move"),
            new SearchableField("canMoveDuringBasicAttack", "Can Move During Basic Attack", "move attack"),
            new SearchableField("basicAttackMoveSpeedMultiplier", "Basic Attack Move Speed Multiplier", "speed multiplier")
        ),
        new SearchableSection(
            "Air Attack Info",
            "Air combo tuning and motion data.",
            new SearchableField("airAttackAnimationSpeeds", "Air Attack Animation Speeds", "air speed"),
            new SearchableField("airAttackMoveDistances", "Air Attack Move Distances", "air move"),
            new SearchableField("airAttackKnockbackForces", "Air Attack Knockback Forces", "air knockback", true),
            new SearchableField("airAttackData", "Air Attack Data", "air hitbox"),
            new SearchableField("airAttackComboInputLeftWindows", "Air Attack Combo Input Left Windows", "air combo"),
            new SearchableField("airAttackComboInputRightWindows", "Air Attack Combo Input Right Windows", "air combo"),
            new SearchableField("airAttackTurnInputLeftWindows", "Air Attack Turn Input Left Windows", "air turn"),
            new SearchableField("airAttackTurnInputRightWindows", "Air Attack Turn Input Right Windows", "air turn"),
            new SearchableField("airAttackDashComboInputWindow", "Air Attack Dash Combo Input Window", "air dash combo"),
            new SearchableField("airAttackDashTurnInputWindow", "Air Attack Dash Turn Input Window", "air dash turn"),
            new SearchableField("airAttackComboGroundBlockDistance", "Air Attack Combo Ground Block Distance", "ground block"),
            new SearchableField("airAttackMoveDuration", "Air Attack Move Duration", "air move"),
            new SearchableField("airAttackFallSpeed", "Air Attack Fall Speed", "fall speed")
        ),
        new SearchableSection(
            "Fall Attack Info",
            "Downward attack tuning and landing response.",
            new SearchableField("fallAttackStartAnimationName", "Fall Attack Start Animation Name", "fall attack"),
            new SearchableField("fallAttackEndAnimationName", "Fall Attack End Animation Name", "fall attack"),
            new SearchableField("fallAttackAnimationSpeed", "Fall Attack Animation Speed", "speed"),
            new SearchableField("fallAttackWindupDuration", "Fall Attack Windup Duration", "windup"),
            new SearchableField("fallAttackGravityMultiplier", "Fall Attack Gravity Multiplier", "gravity"),
            new SearchableField("fallAttackDiveSpeed", "Fall Attack Dive Speed", "dive"),
            new SearchableField("fallAttackDiveAngle", "Fall Attack Dive Angle", "angle"),
            new SearchableField("fallAttackGroundCheckDistance", "Fall Attack Ground Check Distance", "ground"),
            new SearchableField("fallAttackGroundSearchDistance", "Fall Attack Ground Search Distance", "ground"),
            new SearchableField("fallAttackEndAnimationMinSpeed", "Fall Attack End Animation Min Speed", "end speed"),
            new SearchableField("fallAttackEndAnimationMaxSpeed", "Fall Attack End Animation Max Speed", "end speed"),
            new SearchableField("fallAttackEndAnimationLandingOffset", "Fall Attack End Animation Landing Offset", "landing"),
            new SearchableField("fallAttackData", "Fall Attack Data", "fall hitbox")
        ),
        new SearchableSection(
            "Counter Attack Info",
            "Counter attack animation and timing.",
            new SearchableField("counterDuration", "Counter Duration", "counter"),
            new SearchableField("counterAttackTargetCheckRadiusMultiplier", "Counter Attack Target Check Radius Multiplier", "counter range radius"),
            new SearchableField("counterAttackAnimationState", "Counter Attack Animation State", "counter"),
            new SearchableField("counterAttackPerformedAnimationState", "Counter Attack Performed Animation State", "counter")
        ),
        new SearchableSection(
            "Stamina Info",
            "All stamina values, recovery delays, and attack costs.",
            new SearchableField("maxStamina", "Max Stamina", "stamina"),
            new SearchableField("staminaRecoveryPerSecond", "Stamina Recovery Per Second", "recovery"),
            new SearchableField("staminaEmptyRecoveryDelay", "Stamina Empty Recovery Delay", "empty"),
            new SearchableField("jumpStaminaCost", "Jump Stamina Cost", "jump"),
            new SearchableField("jumpStaminaRecoveryDelay", "Jump Stamina Recovery Delay", "jump"),
            new SearchableField("nonCombatJumpStaminaCost", "Non Combat Jump Stamina Cost", "jump"),
            new SearchableField("nonCombatJumpStaminaRecoveryDelay", "Non Combat Jump Stamina Recovery Delay", "jump"),
            new SearchableField("wallJumpStaminaCost", "Wall Jump Stamina Cost", "wall jump"),
            new SearchableField("wallJumpStaminaRecoveryDelay", "Wall Jump Stamina Recovery Delay", "wall jump"),
            new SearchableField("nonCombatWallJumpStaminaCost", "Non Combat Wall Jump Stamina Cost", "wall jump"),
            new SearchableField("nonCombatWallJumpStaminaRecoveryDelay", "Non Combat Wall Jump Stamina Recovery Delay", "wall jump"),
            new SearchableField("dashStaminaCost", "Dash Stamina Cost", "dash"),
            new SearchableField("dashStaminaRecoveryDelay", "Dash Stamina Recovery Delay", "dash"),
            new SearchableField("nonCombatDashStaminaCost", "Non Combat Dash Stamina Cost", "dash"),
            new SearchableField("nonCombatDashStaminaRecoveryDelay", "Non Combat Dash Stamina Recovery Delay", "dash"),
            new SearchableField("counterAttackStaminaCost", "Counter Attack Stamina Cost", "counter"),
            new SearchableField("counterAttackSuccessStaminaCost", "Counter Attack Success Stamina Cost", "counter"),
            new SearchableField("counterAttackStaminaRecoveryDelay", "Counter Attack Stamina Recovery Delay", "counter"),
            new SearchableField("wallHoldStaminaDrainPerSecond", "Wall Hold Stamina Drain Per Second", "wall hold"),
            new SearchableField("wallSlideStaminaDrainPerSecond", "Wall Slide Stamina Drain Per Second", "wall slide"),
            new SearchableField("wallContactStaminaRecoveryDelay", "Wall Contact Stamina Recovery Delay", "wall contact"),
            new SearchableField("basicAttackStaminaCosts", "Basic Attack Stamina Costs", "basic attack"),
            new SearchableField("airAttackStaminaCosts", "Air Attack Stamina Costs", "air attack"),
            new SearchableField("attackStaminaRecoveryDelay", "Attack Stamina Recovery Delay", "attack"),
            new SearchableField("currentStamina", "Current Stamina", "current stamina", true)
        ),
        new SearchableSection(
            "Healing Potion Info",
            "Healing potion count, heal amount, use duration, and move speed penalty.",
            new SearchableField("maxHealingPotionCount", "Max Healing Potion Count", "healing potion count"),
            new SearchableField("currentHealingPotionCount", "Current Healing Potion Count", "healing potion count"),
            new SearchableField("healingPotionHealAmount", "Healing Potion Heal Amount", "healing potion heal"),
            new SearchableField("healingPotionUseDuration", "Healing Potion Use Duration", "healing potion duration use time"),
            new SearchableField("healingPotionMoveSpeedMultiplier", "Healing Potion Move Speed Multiplier", "healing potion speed"),
            new SearchableField("healingPotionWorldIconSprite", "Healing Potion World Icon Sprite", "healing potion icon"),
            new SearchableField("healingPotionWorldIconOffset", "Healing Potion World Icon Offset", "healing potion icon offset")
        ),
        new SearchableSection(
            "Death Info",
            "Death collider and how the body settles on the ground.",
            new SearchableField("deathGroundVisualDownOffset", "Death Ground Visual Down Offset", "death visual"),
            new SearchableField("deathGroundVisualBottomPadding", "Death Ground Visual Bottom Padding", "death visual"),
            new SearchableField("deathCollider", "Death Collider", "death collider")
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
            "Player Inspector Search",
            new[] { "Move", "Jump", "Dash", "Basic Attack", "Air Attack", "Fall Attack", "Stamina", "Healing Potion", "Death" }
        );
        SearchableInspectorDrawer.DrawQuickFindButtons(SetSearch, "move", "jump", "dash", "basic attack", "air attack", "fall attack", "stamina", "healing potion", "death");

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
