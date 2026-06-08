using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReadOnlyFieldAttribute : PropertyAttribute
{
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyFieldAttribute))]
public class ReadOnlyFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool previousEnabled = GUI.enabled;
        GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = previousEnabled;
    }
}
#endif
