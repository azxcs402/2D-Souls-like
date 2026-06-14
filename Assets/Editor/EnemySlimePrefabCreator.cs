using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemySlimePrefabCreator
{
    private const string TargetObjectName = "Enemy_Slime";
    private const string PrefabPath = "Assets/Prefabs/Enemy/Enemy_Slime.prefab";
    private const string SlimeOverrideControllerPath = "Assets/Animations/AnimatorControllers/Characters/Enemy_Slime.overrideController";

    [MenuItem("Tools/Enemy/Convert Selected Copy To Slime Prefab")]
    public static void ConvertSelectedCopyToSlimePrefab()
    {
        GameObject target = Selection.activeGameObject != null
            ? Selection.activeGameObject
            : FindRootObjectByName(TargetObjectName);

        if (target == null)
        {
            Debug.LogError($"Could not find a root scene object named '{TargetObjectName}'. Select the copied skeleton object first and try again.");
            return;
        }

        if (target.transform.parent != null)
        {
            Debug.LogError("Select the root object, not a child.");
            return;
        }

        if (PrefabUtility.IsPartOfPrefabInstance(target))
        {
            PrefabUtility.UnpackPrefabInstance(target, PrefabUnpackMode.Completely, InteractionMode.UserAction);
        }

        ConfigureAsSlime(target);

        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
        GameObject prefabInstance = PrefabUtility.SaveAsPrefabAssetAndConnect(
            target,
            PrefabPath,
            InteractionMode.UserAction
        );

        if (prefabInstance == null)
        {
            Debug.LogError($"Failed to create slime prefab at '{PrefabPath}'.");
            return;
        }

        Scene scene = target.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Converted '{target.name}' into an independent slime prefab '{PrefabPath}'.", prefabInstance);
    }

    private static void ConfigureAsSlime(GameObject target)
    {
        Undo.RegisterFullObjectHierarchyUndo(target, "Convert Enemy To Slime");

        Enemy_Skeleton skeleton = target.GetComponent<Enemy_Skeleton>();
        Enemy_Slime slime = target.GetComponent<Enemy_Slime>();
        Enemy_Healthy health = target.GetComponent<Enemy_Healthy>();
        Animator animator = target.GetComponentInChildren<Animator>(true);

        if (slime == null)
        {
            slime = Undo.AddComponent<Enemy_Slime>(target);
        }

        if (health == null)
        {
            health = Undo.AddComponent<Enemy_Healthy>(target);
        }

        if (skeleton != null)
        {
            Undo.DestroyObjectImmediate(skeleton);
        }

        if (animator != null)
        {
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(SlimeOverrideControllerPath);
            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
            }
        }

        ConfigureSlimeSerializedFields(slime);
        ConfigureStunnedCollider(target);
        ConfigureHealthBar(target);
        target.name = TargetObjectName;
        EditorUtility.SetDirty(target);
        EditorUtility.SetDirty(slime);
        if (health != null)
        {
            EditorUtility.SetDirty(health);
        }
    }

    private static void ConfigureSlimeSerializedFields(Enemy_Slime slime)
    {
        if (slime == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(slime);
        SetIfExists(so, "questTargetId", "enemy_slime");
        SetIfExists(so, "battleMoveSpeed", 3f);
        SetIfExists(so, "attackDistance", 1.5f);
        SetIfExists(so, "attackCooldown", 0.5f);
        SetIfExists(so, "canChasePlayer", true);
        SetIfExists(so, "battleTimeDuration", 5f);
        SetIfExists(so, "minRetreatDistance", 1f);
        SetIfExists(so, "battleStopDistance", 0.08f);
        SetIfExists(so, "retreatVelocity", new Vector2(4f, 2f));
        SetIfExists(so, "stunnedDuration", 1f);
        SetIfExists(so, "stunnedVelocity", new Vector2(7f, 2f));
        SetIfExists(so, "canBeStunned", true);
        SetIfExists(so, "idleDurationMin", 3f);
        SetIfExists(so, "idleDurationMax", 5f);
        SetIfExists(so, "moveDurationMin", 5f);
        SetIfExists(so, "moveDurationMax", 9f);
        SetIfExists(so, "patrolTurnChance", 0.5f);
        SetIfExists(so, "patrolTurnDelay", 0.15f);
        SetIfExists(so, "moveSpeed", 1.4f);
        SetIfExists(so, "moveAnimSpeedMultiplier", 1f);
        SetIfExists(so, "hasRecoveryAnimation", true);
        SetIfExists(so, "canBeKnockedBack", true);
        SetIfExists(so, "amountOfSlimesToCreate", 2);
        SetIfExists(so, "newSlimeVelocity", new Vector2(4f, 3f));

        Transform playerCheck = FindChild(slime.transform.root, "TargetCheck") ?? FindChild(slime.transform.root, "PlayerCheck");
        SetIfExists(so, "playerCheck", playerCheck);

        Transform primaryWallCheck = FindChild(slime.transform.root, "PrimaryWallCheck");
        Transform secondaryWallCheck = FindChild(slime.transform.root, "SecondaryWallCheck");
        SetIfExists(so, "primaryWallCheck", primaryWallCheck);
        SetIfExists(so, "secondaryWallCheck", secondaryWallCheck);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureStunnedCollider(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(target, "Configure Stunned Collider");

        Transform root = target.transform;
        Transform existingTransform = root.Find("StunnedCollider");
        GameObject stunnedColliderObject = existingTransform != null
            ? existingTransform.gameObject
            : new GameObject("StunnedCollider");

        if (existingTransform == null)
        {
            Undo.RegisterCreatedObjectUndo(stunnedColliderObject, "Configure Stunned Collider");
            stunnedColliderObject.transform.SetParent(root, false);
            stunnedColliderObject.layer = target.layer;
        }

        CapsuleCollider2D stunnedCollider = stunnedColliderObject.GetComponent<CapsuleCollider2D>();
        if (stunnedCollider == null)
        {
            stunnedCollider = Undo.AddComponent<CapsuleCollider2D>(stunnedColliderObject);
        }

        CapsuleCollider2D aliveCollider = target.GetComponent<CapsuleCollider2D>();
        if (aliveCollider != null)
        {
            stunnedCollider.direction = aliveCollider.direction;
            stunnedCollider.size = aliveCollider.size;
            stunnedCollider.offset = aliveCollider.offset;
        }

        stunnedCollider.isTrigger = false;
        stunnedCollider.enabled = false;

        Enemy_Slime slime = target.GetComponent<Enemy_Slime>();
        if (slime != null)
        {
            SerializedObject serializedSlime = new SerializedObject(slime);
            SetIfExists(serializedSlime, "stunnedCollider", stunnedCollider);
            serializedSlime.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slime);
        }

        Selection.activeGameObject = stunnedColliderObject;
        EditorGUIUtility.PingObject(stunnedColliderObject);
    }

    private static void ConfigureHealthBar(GameObject target)
    {
        // Keep the existing child health bar setup from the copied skeleton.
        // No extra work required here unless the hierarchy was changed manually.
    }

    private static void SetIfExists(SerializedObject so, string propertyName, string value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }

    private static void SetIfExists(SerializedObject so, string propertyName, bool value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void SetIfExists(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.Float)
        {
            property.floatValue = value;
        }
    }

    private static void SetIfExists(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
    }

    private static void SetIfExists(SerializedObject so, string propertyName, Object value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetIfExists(SerializedObject so, string propertyName, Vector2 value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        if (property.propertyType == SerializedPropertyType.Vector2)
        {
            property.vector2Value = value;
        }
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != null && child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static GameObject FindRootObjectByName(string objectName)
    {
        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();

        foreach (GameObject root in roots)
        {
            if (root != null && root.name == objectName)
            {
                return root;
            }
        }

        return null;
    }
}
