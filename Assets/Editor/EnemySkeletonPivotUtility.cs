using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class EnemySkeletonPivotUtility
{
    [MenuItem("Tools/Enemy/Bake Skeleton Animator Offset Into Root")]
    private static void BakeSkeletonAnimatorOffsetIntoRoot()
    {
        Enemy_Skeleton skeleton = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<Enemy_Skeleton>()
            : null;

        if (skeleton == null)
        {
            Debug.LogWarning("Select Enemy_Skeleton or its Animator child first.");
            return;
        }

        Animator animator = skeleton.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Selected skeleton does not have an Animator child.");
            return;
        }

        Transform root = skeleton.transform;
        Transform visual = animator.transform;
        Vector3 offset = visual.localPosition;

        if (offset == Vector3.zero)
        {
            Debug.Log("Animator is already at local zero. Nothing to bake.");
            return;
        }

        Undo.RecordObjects(new Object[] { root, visual }, "Bake Skeleton Animator Offset");

        Vector3 worldOffset = root.TransformVector(offset);
        root.position += worldOffset;
        visual.localPosition = Vector3.zero;

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);

        Debug.Log($"Baked Animator offset {offset} into {root.name}. Animator local position reset to zero.");
    }

    [MenuItem("Tools/Enemy/Bake Skeleton Animator Offset Into Root", true)]
    private static bool CanBakeSkeletonAnimatorOffsetIntoRoot()
    {
        return Selection.activeGameObject != null
            && Selection.activeGameObject.GetComponentInParent<Enemy_Skeleton>() != null;
    }
}
