using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;

public static class CombatBalanceApplier
{
    private const string ReaperPrefabPath = "Assets/Prefabs/Enemy/Enemy_Reaper.prefab";
    private const string MagePrefabPath = "Assets/Prefabs/Enemy/Enemy_Mage.prefab";
    private const string SlimePrefabPath = "Assets/Prefabs/Enemy/Enemy_Slime.prefab";
    private const string SkeletonPrefabPath = "Assets/Prefabs/Enemy/Enemy_Skeleton.prefab";
    private const string MageProjectilePrefabPath = "Assets/Prefabs/Enemy/Enemy_Mage_Projectile.prefab";
    private const string AbyssMagePrefabPath = "Assets/Prefabs/Enemy/Boss/Enemy_AbyssMage.prefab";
    private const string AbyssMageFireballPrefabPath = "Assets/Prefabs/Enemy/Boss/Enemy_AbyssMage_Fireball.prefab";
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

        foreach (Enemy_AbyssMage abyssMage in Object.FindObjectsOfType<Enemy_AbyssMage>(true))
        {
            ApplyAbyssMageBalance(abyssMage);
        }

        foreach (Enemy_Reaper reaper in Object.FindObjectsOfType<Enemy_Reaper>(true))
        {
            ApplyReaperBalance(reaper);
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

        ApplyPrefabAtPath(AbyssMagePrefabPath, root =>
        {
            ApplyAbyssMageBalance(root.GetComponent<Enemy_AbyssMage>());
        });

        ApplyPrefabAtPath(ReaperPrefabPath, root =>
        {
            ApplyReaperBalance(root.GetComponent<Enemy_Reaper>());
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
            ApplyProjectileDamage(root, 12);
        });

        ApplyPrefabAtPath(AbyssMageFireballPrefabPath, root =>
        {
            ApplyProjectileDamage(root, 8);
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
        SetInt(playerSo, "fallAttackDamage", 22);
        playerSo.ApplyModifiedPropertiesWithoutUndo();
        PlayerBasicAttackDamagesField?.SetValue(player, new[] { 12, 14, 18 });
        PlayerAirAttackDamagesField?.SetValue(player, new[] { 10, 12, 16 });
        PlayerFallAttackDamageField?.SetValue(player, 22);

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
        Debug.Log($"Applied player balance: HP=100, basic=[12,14,18], air=[10,12,16], fall=22, baseDamage=12", player);
    }

    private static void ApplyMageBalance(Enemy_Mage mage)
    {
        if (mage == null)
        {
            return;
        }

        SerializedObject mageSo = new SerializedObject(mage);
        SetInt(mageSo, "maxHealth", 55);
        mageSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(mage, 55);
        EnemyCurrentHealthField?.SetValue(mage, 55);

        Enemy_Healthy health = mage.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 55);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 55);
            EntityHealthCurrentHealthField?.SetValue(health, 55);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = mage.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 16);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 16);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(mage);
        PrefabUtility.RecordPrefabInstancePropertyModifications(mage);
        Debug.Log($"Applied mage balance: maxHealth={mage.MaxHealth}, currentHealth={mage.CurrentHealth}, meleeDamage={(combat != null ? combat.Damage : -1)}", mage);
    }

    private static void ApplyAbyssMageBalance(Enemy_AbyssMage abyssMage)
    {
        if (abyssMage == null)
        {
            return;
        }

        SerializedObject mageSo = new SerializedObject(abyssMage);
        SetInt(mageSo, "maxHealth", 60);
        mageSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(abyssMage, 60);
        EnemyCurrentHealthField?.SetValue(abyssMage, 60);

        Enemy_Healthy health = abyssMage.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 60);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 60);
            EntityHealthCurrentHealthField?.SetValue(health, 60);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = abyssMage.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 16);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 16);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(abyssMage);
        PrefabUtility.RecordPrefabInstancePropertyModifications(abyssMage);
        Debug.Log($"Applied abyss mage balance: maxHealth={abyssMage.MaxHealth}, currentHealth={abyssMage.CurrentHealth}, meleeDamage={(combat != null ? combat.Damage : -1)}", abyssMage);
    }

    private static void ApplyReaperBalance(Enemy_Reaper reaper)
    {
        if (reaper == null)
        {
            return;
        }

        SerializedObject reaperSo = new SerializedObject(reaper);
        SetInt(reaperSo, "maxHealth", 90);
        SerializedProperty spellDamageScale = reaperSo.FindProperty("spellDamageScale");
        if (spellDamageScale != null)
        {
            SerializedProperty phyiscal = spellDamageScale.FindPropertyRelative("phyiscal");
            if (phyiscal != null)
            {
                phyiscal.floatValue = 0.45f;
            }
        }
        reaperSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(reaper, 90);
        EnemyCurrentHealthField?.SetValue(reaper, 90);

        Enemy_Healthy health = reaper.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 90);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 90);
            EntityHealthCurrentHealthField?.SetValue(health, 90);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = reaper.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 18);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 18);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(reaper);
        PrefabUtility.RecordPrefabInstancePropertyModifications(reaper);
        Debug.Log($"Applied reaper balance: maxHealth={reaper.MaxHealth}, currentHealth={reaper.CurrentHealth}, meleeDamage={(combat != null ? combat.Damage : -1)}", reaper);
    }

    private static void ApplySlimeBalance(Enemy_Slime slime)
    {
        if (slime == null)
        {
            return;
        }

        SerializedObject slimeSo = new SerializedObject(slime);
        SetInt(slimeSo, "maxHealth", 44);
        SetFloat(slimeSo, "splitChildAttackLockDuration", 0.8f);
        slimeSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(slime, 44);
        EnemyCurrentHealthField?.SetValue(slime, 44);

        Enemy_Healthy health = slime.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 44);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 44);
            EntityHealthCurrentHealthField?.SetValue(health, 44);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = slime.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 14);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 14);
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
        SetInt(skeletonSo, "maxHealth", 70);
        SetInt(skeletonSo, "skeletonContactDamage", 22);
        skeletonSo.ApplyModifiedPropertiesWithoutUndo();
        EnemyMaxHealthField?.SetValue(skeleton, 70);
        EnemyCurrentHealthField?.SetValue(skeleton, 70);
        SkeletonContactDamageField?.SetValue(skeleton, 22);

        Enemy_Healthy health = skeleton.GetComponent<Enemy_Healthy>();
        if (health != null)
        {
            SerializedObject healthSo = new SerializedObject(health);
            SetInt(healthSo, "maxHealth", 70);
            healthSo.ApplyModifiedPropertiesWithoutUndo();
            EntityHealthMaxHealthField?.SetValue(health, 70);
            EntityHealthCurrentHealthField?.SetValue(health, 70);
            EditorUtility.SetDirty(health);
        }

        Entity_Combat combat = skeleton.GetComponent<Entity_Combat>();
        if (combat != null)
        {
            SerializedObject combatSo = new SerializedObject(combat);
            SetInt(combatSo, "damage", 22);
            combatSo.ApplyModifiedPropertiesWithoutUndo();
            EntityCombatDamageField?.SetValue(combat, 22);
            EditorUtility.SetDirty(combat);
        }

        EditorUtility.SetDirty(skeleton);
        PrefabUtility.RecordPrefabInstancePropertyModifications(skeleton);
        Debug.Log($"Applied skeleton balance: maxHealth={skeleton.MaxHealth}, currentHealth={skeleton.CurrentHealth}, damage={(combat != null ? combat.Damage : -1)}", skeleton);
    }

    private static void ApplyProjectileDamage(GameObject root, int damage)
    {
        if (root == null)
        {
            return;
        }

        Entity_Combat combat = root.GetComponent<Entity_Combat>();
        if (combat == null)
        {
            return;
        }

        SerializedObject combatSo = new SerializedObject(combat);
        SetInt(combatSo, "damage", damage);
        combatSo.ApplyModifiedPropertiesWithoutUndo();
        EntityCombatDamageField?.SetValue(combat, damage);
        EditorUtility.SetDirty(combat);
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

    private static void SetFloat(SerializedObject so, string propertyName, float value)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null)
        {
            property.floatValue = value;
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
