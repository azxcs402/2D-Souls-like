using System.IO;
using UnityEditor;
using UnityEngine;

public static class EnemyMagePrefabCreator
{
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemy/Enemy_Skeleton.prefab";
    private const string MagePrefabPath = "Assets/Prefabs/Enemy/Enemy_Mage.prefab";
    private const string MageControllerPath = "Assets/Animations/AnimatorControllers/Characters/Enemy_Mage.controller";
    private const string MageProjectilePrefabPath = "Assets/Prefabs/Enemy/Enemy_Mage_Projectile.prefab";
    private const string MageSpriteSheetPath = "Assets/Graphics/Characters/Mage/Enemy Mage.png";
    private const string MageName = "Enemy_Mage";
    private static readonly string[] TeleportTriggerRangeNames =
    {
        "TeleportTriggerRange_A",
        "TeleportTriggerRange_B",
        "TeleportTriggerRange_C"
    };

    [MenuItem("Tools/Enemy/Create Enemy Mage Prefab")]
    public static void CreateEnemyMagePrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SkeletonPrefabPath) == null)
        {
            Debug.LogError($"Missing source prefab: {SkeletonPrefabPath}");
            return;
        }

        RuntimeAnimatorController mageController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MageControllerPath);
        if (mageController == null)
        {
            Debug.LogError($"Missing mage Animator Controller: {MageControllerPath}");
            return;
        }

        GameObject mageProjectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MageProjectilePrefabPath);
        if (mageProjectilePrefab == null)
        {
            Debug.LogError($"Missing mage projectile prefab: {MageProjectilePrefabPath}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(MagePrefabPath));

        if (AssetDatabase.LoadAssetAtPath<GameObject>(MagePrefabPath) == null)
        {
            if (!AssetDatabase.CopyAsset(SkeletonPrefabPath, MagePrefabPath))
            {
                Debug.LogError($"Failed to create mage prefab at: {MagePrefabPath}");
                return;
            }
        }

        GameObject root = PrefabUtility.LoadPrefabContents(MagePrefabPath);
        if (root == null)
        {
            Debug.LogError($"Failed to load prefab contents: {MagePrefabPath}");
            return;
        }

        try
        {
            ConfigureAsMage(root, mageController, mageProjectilePrefab);
            PrefabUtility.SaveAsPrefabAsset(root, MagePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(MagePrefabPath));
        Debug.Log($"Created or updated mage prefab: {MagePrefabPath}");
    }

    private static void ConfigureAsMage(GameObject root, RuntimeAnimatorController mageController, GameObject mageProjectilePrefab)
    {
        root.name = MageName;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        Enemy_Skeleton skeleton = root.GetComponent<Enemy_Skeleton>();
        Enemy_Mage mage = root.GetComponent<Enemy_Mage>();
        Enemy_Healthy health = root.GetComponent<Enemy_Healthy>();
        CapsuleCollider2D capsule = root.GetComponent<CapsuleCollider2D>();
        Animator animator = root.GetComponentInChildren<Animator>(true);
        SpriteRenderer spriteRenderer = animator != null ? animator.GetComponent<SpriteRenderer>() : null;

        if (mage == null)
        {
            mage = root.AddComponent<Enemy_Mage>();
        }

        if (health == null)
        {
            health = root.AddComponent<Enemy_Healthy>();
        }

        if (skeleton != null)
        {
            Object.DestroyImmediate(skeleton, true);
        }

        if (animator != null)
        {
            animator.runtimeAnimatorController = mageController;
            animator.transform.localPosition = new Vector3(0f, -0.27f, 0f);
            animator.transform.localScale = new Vector3(2.08f, 2.08f, 2.08f);
            animator.transform.localRotation = Quaternion.identity;
        }

        if (spriteRenderer != null)
        {
            Sprite idleSprite = LoadFirstSprite(MageSpriteSheetPath);
            if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }

            spriteRenderer.sortingLayerName = "Enemy";
            spriteRenderer.sortingOrder = -1;
        }

        if (capsule != null)
        {
            capsule.offset = new Vector2(-0.07571983f, -0.40121365f);
            capsule.size = new Vector2(1.0232773f, 1.8222394f);
            capsule.direction = CapsuleDirection2D.Vertical;
        }

        EnsureChild(root.transform, "GroundCheck", new Vector3(0.62f, -0.929f, 0f));
        Transform targetCheck = EnsureChild(root.transform, "TargetCheck", new Vector3(0.383f, -0.385f, 0f));
        Transform spellStartPoint = EnsureChild(root.transform, "SpellStartPoint", new Vector3(0.38f, 0.46f, 0.05305192f), new Vector3(0.2f, 0.2f, 0.2f));
        Transform projectileHoverArea = EnsureHoverAreaAnchor(root.transform, new Vector3(0f, 2.2f, 0f), new Vector2(4f, 1.8f));
        Transform teleportArea = EnsureTeleportAreaAnchor(root.transform, new Vector3(0f, 0.8f, 0f), new Vector2(7f, 3f));
        EnsureTeleportTriggerRanges(root.transform);
        Transform behindCheck = EnsureChild(root.transform, "BehindCheck", new Vector3(-0.93f, -0.73f, 0f));

        SpriteRenderer spellPointRenderer = spellStartPoint.GetComponent<SpriteRenderer>();
        if (spellPointRenderer == null)
        {
            spellPointRenderer = spellStartPoint.gameObject.AddComponent<SpriteRenderer>();
        }

        spellPointRenderer.enabled = false;

        SerializedObject mageSo = new SerializedObject(mage);
        SetObjectReference(mageSo, "playerCheck", targetCheck);
        SetObjectReference(mageSo, "spellPrefab", mageProjectilePrefab);
        SetObjectReference(mageSo, "spellStartPosition", spellStartPoint);
        SetObjectReference(mageSo, "projectileHoverAreaAnchor", projectileHoverArea);
        SetObjectReference(mageSo, "teleportAreaAnchor", teleportArea);
        SetObjectReference(mageSo, "behindCollisionCheck", behindCheck);
        SetFloat(mageSo, "battleMoveSpeed", 4f);
        SetFloat(mageSo, "attackDistance", 1.3f);
        SetFloat(mageSo, "attackCooldown", 0.5f);
        SetBool(mageSo, "canChasePlayer", true);
        SetFloat(mageSo, "battleTimeDuration", 5f);
        SetFloat(mageSo, "minRetreatDistance", 1f);
        SetFloat(mageSo, "battleStopDistance", 0.08f);
        SetVector2(mageSo, "retreatVelocity", new Vector2(7f, 4f));
        SetFloat(mageSo, "retreatCooldown", 4f);
        SetFloat(mageSo, "retreatMaxDistance", 8f);
        SetFloat(mageSo, "retreatSpeed", 15f);
        SetInteger(mageSo, "teleportMaxPlacementAttempts", 18);
        SetFloat(mageSo, "teleportGroundSearchHeight", 2f);
        SetFloat(mageSo, "teleportGroundSearchDistance", 8f);
        SetInteger(mageSo, "teleportImageEchoCount", 6);
        SetFloat(mageSo, "teleportImageEchoLifetime", 0.3f);
        SetFloat(mageSo, "teleportPostDelay", 0.06f);
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
        SetInteger(mageSo, "amountToCast", 3);
        SetFloat(mageSo, "spellCastCooldown", 0.3f);
        SetBool(mageSo, "hasRecoveryAnimation", true);
        SetBool(mageSo, "canBeKnockedBack", true);
        SetString(mageSo, "questTargetId", "enemy_mage");
        SetString(mageSo, "attackAnimationState", "skeletonAttack");
        SetString(mageSo, "idleAnimationState", "mageIdle");
        SetString(mageSo, "moveAnimationState", "mageMove");
        SetString(mageSo, "battleAnimationState", "mageBattle - idle/move");
        SetString(mageSo, "stunnedAnimationState", "mageStunned");
        SetString(mageSo, "stunRecoveryAnimationState", "mageStunRecovery");
        SetString(mageSo, "spellCastAnimationState", "mageSpellCast");
        SetString(mageSo, "spellCastPerformedAnimationState", "mageSpellCast_performed");
        SetFloat(mageSo, "deadFallSpeed", 2.25f);
        SetFloat(mageSo, "deadSlideSpeed", 0.65f);
        SetFloat(mageSo, "deadSlideAcceleration", 1.2f);
        SetFloat(mageSo, "deadDropThroughDelay", 0f);
        SetFloat(mageSo, "deadDisappearDelay", 4f);
        SetFloat(mageSo, "deadFallAngle", 90f);
        SetLayerMask(mageSo, "whatIsPlayer", LayerMask.GetMask("Player"));
        mageSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject enemySo = new SerializedObject(mage);
        SetInteger(enemySo, "maxHealth", 5);
        SetBool(enemySo, "canTakeDamage", true);
        enemySo.ApplyModifiedPropertiesWithoutUndo();

        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInteger(healthSo, "maxHealth", 5);
            SetBool(healthSo, "canTakeDamage", true);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(mage);
        if (health != null)
        {
            EditorUtility.SetDirty(health);
        }

        if (animator != null)
        {
            EditorUtility.SetDirty(animator);
        }
    }

    private static Transform EnsureChild(Transform root, string childName, Vector3 localPosition)
    {
        return EnsureChild(root, childName, localPosition, Vector3.one);
    }

    private static Transform EnsureChild(Transform root, string childName, Vector3 localPosition, Vector3 localScale)
    {
        Transform child = root.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(root, false);
            child.gameObject.layer = root.gameObject.layer;
        }

        child.localPosition = localPosition;
        child.localRotation = Quaternion.identity;
        child.localScale = localScale;
        return child;
    }

    private static Transform EnsureHoverAreaAnchor(Transform root, Vector3 defaultLocalPosition, Vector2 defaultSize)
    {
        Transform child = root.Find("ProjectileHoverArea")
            ?? root.Find("SpellHoverArea")
            ?? root.Find("ProjectileHoverAreaAnchor");

        if (child == null)
        {
            GameObject childObject = new GameObject("ProjectileHoverArea");
            child = childObject.transform;
            child.SetParent(root, false);
            child.gameObject.layer = root.gameObject.layer;
            child.localPosition = defaultLocalPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }
        else
        {
            child.SetParent(root, false);
            if (child.name != "ProjectileHoverArea")
            {
                child.name = "ProjectileHoverArea";
            }
            if (child.localScale == Vector3.zero)
            {
                child.localScale = Vector3.one;
            }
        }

        if (child.GetComponent<Enemy_MageProjectileHoverAreaAnchor>() == null)
        {
            child.gameObject.AddComponent<Enemy_MageProjectileHoverAreaAnchor>();
        }

        Enemy_MageProjectileHoverAreaAnchor anchor = child.GetComponent<Enemy_MageProjectileHoverAreaAnchor>();
        if (anchor != null)
        {
            SerializedObject anchorSo = new SerializedObject(anchor);
            SerializedProperty sizeProperty = anchorSo.FindProperty("hoverAreaSize");
            if (sizeProperty != null)
            {
                sizeProperty.vector2Value = defaultSize;
                anchorSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        return child;
    }

    private static Transform EnsureTeleportAreaAnchor(Transform root, Vector3 defaultLocalPosition, Vector2 defaultSize)
    {
        Transform child = root.Find("TeleportArea")
            ?? root.Find("MageTeleportArea")
            ?? root.Find("TeleportRange");

        if (child == null)
        {
            GameObject childObject = new GameObject("TeleportArea");
            child = childObject.transform;
            child.SetParent(root, false);
            child.gameObject.layer = root.gameObject.layer;
            child.localPosition = defaultLocalPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }
        else
        {
            child.SetParent(root, false);
            child.name = "TeleportArea";
            if (child.localScale == Vector3.zero)
            {
                child.localScale = Vector3.one;
            }
        }

        if (child.GetComponent<Enemy_MageTeleportAreaAnchor>() == null)
        {
            child.gameObject.AddComponent<Enemy_MageTeleportAreaAnchor>();
        }

        Enemy_MageTeleportAreaAnchor anchor = child.GetComponent<Enemy_MageTeleportAreaAnchor>();
        if (anchor != null)
        {
            SerializedObject anchorSo = new SerializedObject(anchor);
            SerializedProperty sizeProperty = anchorSo.FindProperty("areaSize");
            if (sizeProperty != null)
            {
                sizeProperty.vector2Value = defaultSize;
                anchorSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        return child;
    }

    private static void EnsureTeleportTriggerRanges(Transform root)
    {
        Vector3[] defaultPositions =
        {
            new Vector3(-2.2f, 0.2f, 0f),
            new Vector3(0f, 0.2f, 0f),
            new Vector3(2.2f, 0.2f, 0f)
        };

        for (int i = 0; i < TeleportTriggerRangeNames.Length; i++)
        {
            Transform child = root.Find(TeleportTriggerRangeNames[i]);
            if (child == null)
            {
                GameObject childObject = new GameObject(TeleportTriggerRangeNames[i]);
                child = childObject.transform;
                child.SetParent(root, false);
                child.gameObject.layer = root.gameObject.layer;
            }

            child.localPosition = defaultPositions[i];
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;

            if (child.GetComponent<Enemy_MageTeleportTriggerAnchor>() == null)
            {
                child.gameObject.AddComponent<Enemy_MageTeleportTriggerAnchor>();
            }

            Enemy_MageTeleportTriggerAnchor anchor = child.GetComponent<Enemy_MageTeleportTriggerAnchor>();
            if (anchor != null)
            {
                SerializedObject anchorSo = new SerializedObject(anchor);
                SerializedProperty radiusProperty = anchorSo.FindProperty("radius");
                if (radiusProperty != null)
                {
                    radiusProperty.floatValue = 2f;
                    anchorSo.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }
    }

    private static Sprite LoadFirstSprite(string assetPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] is Sprite sprite)
            {
                return sprite;
            }
        }

        return null;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.String)
        {
            property.stringValue = value;
        }
    }

    private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Boolean)
        {
            property.boolValue = value;
        }
    }

    private static void SetInteger(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Integer)
        {
            property.intValue = value;
        }
    }

    private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Float)
        {
            property.floatValue = value;
        }
    }

    private static void SetVector2(SerializedObject serializedObject, string propertyName, Vector2 value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.Vector2)
        {
            property.vector2Value = value;
        }
    }

    private static void SetLayerMask(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.LayerMask)
        {
            property.intValue = value;
        }
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
        {
            property.objectReferenceValue = value;
        }
    }
}
