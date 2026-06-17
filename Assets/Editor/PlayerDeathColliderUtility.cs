using UnityEditor;
using UnityEngine;

public static class PlayerDeathColliderUtility
{
    private const string DeathColliderName = "DeathCollider";

    [MenuItem("Tools/Player/Create Death Collider Child")]
    public static void CreateDeathColliderChild()
    {
        GameObject selected = Selection.activeGameObject;
        Player player = selected != null ? selected.GetComponentInParent<Player>() : null;

        if (player == null)
        {
            Debug.LogError("Select the Player root or any of its children first.");
            return;
        }

        GameObject playerObject = player.gameObject;
        Undo.RegisterFullObjectHierarchyUndo(playerObject, "Create Death Collider Child");

        Transform existingTransform = playerObject.transform.Find(DeathColliderName);
        GameObject deathColliderObject = existingTransform != null
            ? existingTransform.gameObject
            : new GameObject(DeathColliderName);

        if (existingTransform == null)
        {
            Undo.RegisterCreatedObjectUndo(deathColliderObject, "Create Death Collider Child");
            deathColliderObject.transform.SetParent(playerObject.transform, false);
            deathColliderObject.layer = playerObject.layer;
        }

        CapsuleCollider2D deathCollider = deathColliderObject.GetComponent<CapsuleCollider2D>();
        if (deathCollider == null)
        {
            deathCollider = Undo.AddComponent<CapsuleCollider2D>(deathColliderObject);
        }

        deathCollider.direction = CapsuleDirection2D.Horizontal;
        deathCollider.isTrigger = false;
        deathCollider.enabled = false;

        SerializedObject playerObjectSerialized = new SerializedObject(player);
        SerializedProperty deathColliderProperty = playerObjectSerialized.FindProperty("deathCollider");
        if (deathColliderProperty != null)
        {
            deathColliderProperty.objectReferenceValue = deathCollider;
            playerObjectSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        Selection.activeGameObject = deathColliderObject;
        EditorGUIUtility.PingObject(deathColliderObject);
        Debug.Log($"Created or updated '{DeathColliderName}' on '{playerObject.name}'. Select it and use 'Edit Collider' to drag the shape in Scene.");
    }
}
