using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy_AbyssMageTeleportTriggerAnchor : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float radius = 2f;

    public Enemy_AbyssMage Mage => GetComponentInParent<Enemy_AbyssMage>();
    public float Radius => radius;

    private void OnValidate()
    {
        radius = Mathf.Max(.1f, radius);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Mage == null)
        {
            return;
        }

        DrawRange(transform.position, radius, name);
    }

    public static void DrawRange(Vector3 center, float radius, string labelPrefix)
    {
        if (radius <= 0f)
        {
            return;
        }

        Color fillColor = new Color(1f, .85f, .2f, .08f);
        Color wireColor = new Color(1f, .85f, .2f, 1f);

        Handles.color = fillColor;
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        Handles.color = wireColor;
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (radius + .12f), $"{labelPrefix} / Radius {radius:0.##}", labelStyle);
    }
#endif
}
