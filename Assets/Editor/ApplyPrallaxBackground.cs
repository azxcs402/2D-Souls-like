#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ApplyPrallaxBackground
{
    private const string SessionKey = "Codex.ApplyPrallaxBackground.Teleport.Done";

    static ApplyPrallaxBackground()
    {
        EditorApplication.delayCall += TryApplyOnce;
    }

    [MenuItem("Codex/Apply Parallax Background")]
    public static void ApplyFromMenu()
    {
        ApplyToActiveScene();
    }

    private static void TryApplyOnce()
    {
        if (SessionState.GetBool(SessionKey, false))
        {
            return;
        }

        if (ApplyToActiveScene())
        {
            SessionState.SetBool(SessionKey, true);
        }
    }

    private static bool ApplyToActiveScene()
    {
        GameObject backgroundObject = GameObject.Find("Background");
        if (backgroundObject == null)
        {
            return false;
        }

        PrallaxBackground parallaxBackground = backgroundObject.GetComponent<PrallaxBackground>();
        if (parallaxBackground == null)
        {
            parallaxBackground = backgroundObject.AddComponent<PrallaxBackground>();
        }

        Transform sky = FindChildRecursive(backgroundObject.transform, "Background_sky");
        Transform town = FindChildRecursive(backgroundObject.transform, "Background_Town");
        Transform townLeft = FindChildRecursive(backgroundObject.transform, "Background_Town_Left");
        Transform townRight = FindChildRecursive(backgroundObject.transform, "Background_Town_RIght");
        GameObject playerObject = GameObject.Find("Player");

        SerializedObject serializedObject = new SerializedObject(parallaxBackground);
        serializedObject.FindProperty("background").objectReferenceValue = backgroundObject.transform;
        serializedObject.FindProperty("cameraTransform").objectReferenceValue = null;
        serializedObject.FindProperty("infiniteHorizontal").boolValue = true;
        serializedObject.FindProperty("targetTransform").objectReferenceValue = playerObject != null ? playerObject.transform : null;
        serializedObject.FindProperty("centerBackground").objectReferenceValue = town;
        serializedObject.FindProperty("rightBackground").objectReferenceValue = townRight;
        serializedObject.FindProperty("leftBackground").objectReferenceValue = townLeft;
        serializedObject.FindProperty("teleportTriggerOffset").floatValue = 0f;
        serializedObject.FindProperty("triggerLineHeight").floatValue = 8f;

        SerializedProperty layersProperty = serializedObject.FindProperty("backgroundLayers");
        layersProperty.arraySize = 2;

        SerializedProperty skyLayer = layersProperty.GetArrayElementAtIndex(0);
        skyLayer.FindPropertyRelative("layer").objectReferenceValue = sky;
        skyLayer.FindPropertyRelative("parallaxMultiplier").vector2Value = new Vector2(0.08f, 0.02f);

        SerializedProperty townLayer = layersProperty.GetArrayElementAtIndex(1);
        townLayer.FindPropertyRelative("layer").objectReferenceValue = town;
        townLayer.FindPropertyRelative("parallaxMultiplier").vector2Value = new Vector2(0.22f, 0.12f);

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(parallaxBackground);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return true;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
#endif
