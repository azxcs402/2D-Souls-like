using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class EnemySlimeAnimatorConfigurator
{
    private const string SkeletonControllerPath = "Assets/Animations/AnimatorControllers/Characters/Enemy_Skeleton.controller";
    private const string SlimeControllerPath = "Assets/Animations/AnimatorControllers/Characters/Enemy_Slime.controller";
    private const string SlimePrefabPath = "Assets/Prefabs/Enemy/Enemy_Slime.prefab";

    private const string SlimeAttackPath = "Assets/Animations/Characters/Slime/slimeAttack.anim";
    private const string SlimeIdlePath = "Assets/Animations/Characters/Slime/slimeIdle.anim";
    private const string SlimeMovePath = "Assets/Animations/Characters/Slime/slimeMove.anim";
    private const string SlimeStunnedPath = "Assets/Animations/Characters/Slime/slimeStunned.anim";
    private const string SlimeStunRecoveryPath = "Assets/Animations/Characters/Slime/slimeStunRecovery.anim";

    static EnemySlimeAnimatorConfigurator()
    {
        EditorApplication.delayCall += ConfigureIfNeeded;
    }

    [MenuItem("Tools/Enemy/Configure Slime Animator")]
    public static void ConfigureSlimeAnimator()
    {
        AnimatorController slimeController = CreateOrUpdateSlimeController();
        if (slimeController == null)
        {
            return;
        }

        ConfigureSlimePrefab(slimeController);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Configured Enemy_Slime to use its own slime Animator Controller and slime animation states.");
    }

    private static void ConfigureIfNeeded()
    {
        AnimatorController existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SlimeControllerPath);
        GameObject slimePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlimePrefabPath);
        Animator prefabAnimator = slimePrefab != null ? slimePrefab.GetComponentInChildren<Animator>(true) : null;

        bool needsController = existingController == null;
        bool needsPrefabController = prefabAnimator != null && prefabAnimator.runtimeAnimatorController != existingController;

        if (needsController || needsPrefabController)
        {
            ConfigureSlimeAnimator();
        }
    }

    private static AnimatorController CreateOrUpdateSlimeController()
    {
        AnimatorController skeletonController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SkeletonControllerPath);
        if (skeletonController == null)
        {
            Debug.LogError($"Missing source Animator Controller: {SkeletonControllerPath}");
            return null;
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(SlimeControllerPath) == null)
        {
            if (!AssetDatabase.CopyAsset(SkeletonControllerPath, SlimeControllerPath))
            {
                Debug.LogError($"Failed to create slime Animator Controller at: {SlimeControllerPath}");
                return null;
            }
        }

        AnimatorController slimeController = AssetDatabase.LoadAssetAtPath<AnimatorController>(SlimeControllerPath);
        if (slimeController == null)
        {
            Debug.LogError($"Failed to load slime Animator Controller: {SlimeControllerPath}");
            return null;
        }

        slimeController.name = "Enemy_Slime";

        AnimationClip slimeAttack = LoadClip(SlimeAttackPath);
        AnimationClip slimeIdle = LoadClip(SlimeIdlePath);
        AnimationClip slimeMove = LoadClip(SlimeMovePath);
        AnimationClip slimeStunned = LoadClip(SlimeStunnedPath);
        AnimationClip slimeStunRecovery = LoadClip(SlimeStunRecoveryPath);

        foreach (AnimatorControllerLayer layer in slimeController.layers)
        {
            UpdateStateMachine(layer.stateMachine, slimeAttack, slimeIdle, slimeMove, slimeStunned, slimeStunRecovery);
        }

        EditorUtility.SetDirty(slimeController);
        return slimeController;
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogError($"Missing slime animation clip: {path}");
        }

        return clip;
    }

    private static void UpdateStateMachine(
        AnimatorStateMachine stateMachine,
        AnimationClip slimeAttack,
        AnimationClip slimeIdle,
        AnimationClip slimeMove,
        AnimationClip slimeStunned,
        AnimationClip slimeStunRecovery)
    {
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            UpdateState(childState.state, slimeAttack, slimeIdle, slimeMove, slimeStunned, slimeStunRecovery);
        }

        foreach (ChildAnimatorStateMachine childStateMachine in stateMachine.stateMachines)
        {
            UpdateStateMachine(childStateMachine.stateMachine, slimeAttack, slimeIdle, slimeMove, slimeStunned, slimeStunRecovery);
        }
    }

    private static void UpdateState(
        AnimatorState state,
        AnimationClip slimeAttack,
        AnimationClip slimeIdle,
        AnimationClip slimeMove,
        AnimationClip slimeStunned,
        AnimationClip slimeStunRecovery)
    {
        if (state == null)
        {
            return;
        }

        switch (state.name)
        {
            case "skeletonAttack":
            case "slimeAttack":
                state.name = "slimeAttack";
                state.motion = slimeAttack;
                break;

            case "skeletonIdle":
            case "slimeIdle":
                state.name = "slimeIdle";
                state.motion = slimeIdle;
                break;

            case "skeletonMove":
            case "slimeMove":
                state.name = "slimeMove";
                state.motion = slimeMove;
                break;

            case "skeletonBattle - idle/move":
            case "slimeBattle - idle/move":
                state.name = "slimeBattle - idle/move";
                UpdateBattleBlendTree(state.motion as BlendTree, slimeIdle, slimeMove);
                break;

            case "skeletonStunned":
            case "slimeStunned":
                state.name = "slimeStunned";
                state.motion = slimeStunned;
                break;

            case "skeletonStunRecovery":
            case "slimeStunRecovery":
                state.name = "slimeStunRecovery";
                state.motion = slimeStunRecovery;
                break;
        }

        EditorUtility.SetDirty(state);
    }

    private static void UpdateBattleBlendTree(BlendTree blendTree, AnimationClip slimeIdle, AnimationClip slimeMove)
    {
        if (blendTree == null)
        {
            return;
        }

        blendTree.name = "slimeBattle - idle/move";
        ChildMotion[] children = blendTree.children;

        for (int i = 0; i < children.Length; i++)
        {
            Motion motion = children[i].motion;
            if (motion == null)
            {
                continue;
            }

            if (motion.name == "skeletonIdle" || motion.name == "slimeIdle")
            {
                children[i].motion = slimeIdle;
            }
            else if (motion.name == "skeletonMove" || motion.name == "slimeMove")
            {
                children[i].motion = slimeMove;
            }
        }

        blendTree.children = children;
        EditorUtility.SetDirty(blendTree);
    }

    private static void ConfigureSlimePrefab(RuntimeAnimatorController slimeController)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SlimePrefabPath);
        if (root == null)
        {
            Debug.LogError($"Missing slime prefab: {SlimePrefabPath}");
            return;
        }

        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.runtimeAnimatorController = slimeController;
                EditorUtility.SetDirty(animator);
            }

            Enemy_Slime slime = root.GetComponent<Enemy_Slime>();
            if (slime != null)
            {
                SerializedObject serializedSlime = new SerializedObject(slime);
                SetString(serializedSlime, "attackAnimationState", "slimeAttack");
                SetString(serializedSlime, "idleAnimationState", "slimeIdle");
                SetString(serializedSlime, "moveAnimationState", "slimeMove");
                SetString(serializedSlime, "battleAnimationState", "slimeBattle - idle/move");
                SetString(serializedSlime, "stunnedAnimationState", "slimeStunned");
                SetString(serializedSlime, "stunRecoveryAnimationState", "slimeStunRecovery");
                serializedSlime.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(slime);
            }

            PrefabUtility.SaveAsPrefabAsset(root, SlimePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.stringValue = value;
        }
    }
}
