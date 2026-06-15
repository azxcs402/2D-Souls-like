using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;

public static class CombatBalanceApplier
{
    private const string MagePrefabPath = "Assets/Prefabs/Enemy/Enemy_Mage.prefab";
    private const string SlimePrefabPath = "Assets/Prefabs/Enemy/Enemy_Slime.prefab";
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemy/Enemy_Skeleton.prefab";
    private const string MageProjectilePrefabPath = "Assets/Prefabs/Enemy/Enemy_Mage_Projectile.prefab";
    private static readonly BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo EnemyMaxHealthField = typeof(Enemy).GetField("maxHealth", InstanceFlags);
    private static readonly FieldInfo EnemyCurrentHealthField = typeof(Enemy).GetField("currentHealth", InstanceFlags);
    private static readonly FieldInfo EntityHealthMaxHealthField = typeof(Entity_Health).GetField("maxHealth", InstanceFlags);
    private static readonly FieldInfo EntityHealthCurrentHealthField = typeof(Entity_Health).GetField("currentHealth", InstanceFlags);
    private static readonly FieldInfo EntityCombatDamageField = typeof(Entity_Combat).GetField("damage", InstanceFlags);
    private static readonly FieldInfo PlayerBasicAttackDamagesField = typeof(Player).GetField("basicAttackDamages", InstanceFlags);
    private static readonly FieldInfo PlayerAirAttackDamagesField = typeof(Player).GetField("airAttackDamages", InstanceFlags);
    private static readonly FieldInfo PlayerFallAttackDamageField = typeof(Player).GetField("fallAttackDamage", InstanceFlags);
    private static readonly FieldInfo SkeletonContactDamageField = typeof(Enemy_Skeleton).GetField("skeletonContactDamage", InstanceFlags);

    [MenuItem("Tools/Balance/Apply Requested Combat Balance")]
    public static void ApplyRequestedCombatBalance()
    {
        ApplySceneBalance();
        ApplyPrefabBalance();

        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Applied requested combat balance to open scenes and enemy prefabs.");
    }

    private static void ApplySceneBalance()
    {
        foreach (Player player in Object.FindObjectsOfType<Player>(true))
        {
            ApplyPlayerBalance(player);
        }

        foreach (Enemy_Mage mage in Object.FindObjectsOfType<Enemy_Mage>(true))
        {
            ApplyMageBalance(mage);
        }

        foreach (Enemy_Slime slime in Object.FindObjectsOfType<Enemy_Slime>(true))
        {
            ApplySlimeBalance(slime);
        }

        foreach (Enemy_Skeleton skeleton in Object.FindObjectsOfType<Enemy_Skeleton>(true))
        {
            ApplySkeletonBalance(skeleton);
        }
    }

    private static void ApplyPrefabBalance()
    {
        ApplyPrefabAtPath(MagePrefabPath, root =>
        {
            ApplyMageBalance(root.GetComponent<Enemy_Mage>());
        });

        ApplyPrefabAtPath(SlimePrefabPath, root =>
        {
            ApplySlimeBalance(root.GetComponent<Enemy_Slime>());
        });

        ApplyPrefabAtPath(SkeletonPrefabPath, root =>
        {
            ApplySkeletonBalance(root.GetComponent<Enemy_Skeleton>());
        });

        ApplyPrefabAtPath(MageProjectilePrefabPath, root =>
        {
            Entity_Combat combat = root.GetComponent<Entity_Combat>();
            if (combat != null)
            {
                SerializedObject combatSo = new SerializedObject(combat);
                SetInt(combatSo, "damage", 10);
                combatSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(combat);
            }
        });
    }

    private static void ApplyPlayerBalance(Player player)
    {
        if (player == null)
        {
            return;
        }

        SerializedObject playerSo = new SerializedObject(player);
        SetIntArray(playerSo, "basicAttackDamages", 12, 14, 18);
        SetIntArray(playerSo, "airAttackDamages", 10, 12, 16);
        SetInt(playerSo, "fallAttackDamage", 20);
        playerSo.ApplyModifiedPropertiesWithoutUndo();
        PlayerBasicAttackDamagesField?.SetValue(player, new[] { 12, 14, 18 });
        PlayerAirAttackDamagesField?.SetValue(player, new[] { 10, 12, 16 });
        PlayerFallAttackDamageField?.SetValue(player, 20);

        Entity_Health health = player.GetComponent<Entity_Health>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 100);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 100);
            EntityHealthCurrentHealthField?.SetValue(health, 100);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = player.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 12);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 12);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(player);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player);
        Debug.Log($"Applied player balance: HP=100, basic=[12,14,18], air=[10,12,16], fall=20, baseDamage=12", player);
    }

    private static void ApplyMageBalance(Enemy_Mage mage)
    {
        if (mage == null)
        {
            return;
        }

        SerializedObject mageSo = new SerializedObject(mage);
        SetInt(mageSo, "maxHealth", 65);
        mageSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(mage, 65);
        EnemyCurrentHealthField?.SetValue(mage, 65);

        Enemy_Healthy health = mage.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 65);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 65);
            EntityHealthCurrentHealthField?.SetValue(health, 65);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = mage.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 20);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 20);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(mage);
        PrefabUtility.RecordPrefabInstancePropertyModifications(mage);
        Debug.Log($"Applied mage balance: maxHealth={mage.MaxHealth}, currentHealth={mage.CurrentHealth}, meleeDamage={(combat != null ? combat.Damage : -1)}", mage);
    }

    private static void ApplySlimeBalance(Enemy_Slime slime)
    {
        if (slime == null)
        {
            return;
        }

        SerializedObject slimeSo = new SerializedObject(slime);
        SetInt(slimeSo, "maxHealth", 45);
        slimeSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(slime, 45);
        EnemyCurrentHealthField?.SetValue(slime, 45);

        Enemy_Healthy health = slime.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 45);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 45);
            EntityHealthCurrentHealthField?.SetValue(health, 45);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = slime.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 17);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 17);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(slime);
        PrefabUtility.RecordPrefabInstancePropertyModifications(slime);
        Debug.Log($"Applied slime balance: maxHealth={slime.MaxHealth}, currentHealth={slime.CurrentHealth}, damage={(combat != null ? combat.Damage : -1)}", slime);
    }

    private static void ApplySkeletonBalance(Enemy_Skeleton skeleton)
    {
        if (skeleton == null)
        {
            return;
        }

        SerializedObject skeletonSo = new SerializedObject(skeleton);
        SetInt(skeletonSo, "maxHealth", 65);
        SetInt(skeletonSo, "skeletonContactDamage", 25);
        skeletonSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(skeleton, 65);
        EnemyCurrentHealthField?.SetValue(skeleton, 65);
        SkeletonContactDamageField?.SetValue(skeleton, 25);

        Enemy_Healthy health = skeleton.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 65);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 65);
            EntityHealthCurrentHealthField?.SetValue(health, 65);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = skeleton.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 25);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 25);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(skeleton);
        PrefabUtility.RecordPrefabInstancePropertyModifications(skeleton);
        Debug.Log($"Applied skeleton balance: maxHealth={skeleton.MaxHealth}, currentHealth={skeleton.CurrentHealth}, damage={(combat != null ? combat.Damage : -1)}", skeleton);
    }

    private static void ApplyPrefabAtPath(string path, System.Action<GameObject> apply)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogWarning($"Could not load prefab at {path}");
            return;
        }

        try
        {
            apply(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetInt(SerializedObject so, string propertyName, int value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }

    private static void SetIntArray(SerializedObject so, string propertyName, params int[] values)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return;
        }

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).intValue = values[i];
        }
    }
}
