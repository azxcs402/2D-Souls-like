using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy_AbyssMageTeleportAreaAnchor : MonoBehaviour
{
    [SerializeField, Min(.1f)] private Vector2 areaSize = new Vector2(7f, 3f);

    public Enemy_AbyssMage Mage => GetComponentInParent<Enemy_AbyssMage>();
    public Vector2 AreaSize => areaSize;

    private void OnValidate()
    {
        areaSize = new Vector2(
            Mathf.Max(.1f, areaSize.x),
            Mathf.Max(.1f, areaSize.y)
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Mage == null)
        {
            return;
        }

        DrawOutline(transform.position, areaSize);
    }

    public static void DrawOutline(Vector3 center, Vector2 size)
    {
        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        Vector3 half = new Vector3(size.x * .5f, size.y * .5f, 0f);
        Vector3[] corners =
        {
            center + new Vector3(-half.x, -half.y, 0f),
            center + new Vector3(-half.x, half.y, 0f),
            center + new Vector3(half.x, half.y, 0f),
            center + new Vector3(half.x, -half.y, 0f)
        };

        Color wireColor = new Color(.95f, .85f, .2f, 1f);
        Handles.color = wireColor;
        Handles.DrawAAPolyLine(3f, corners[0], corners[1], corners[2], corners[3], corners[0]);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (size.y * .5f + .12f), $"Teleport Area / {size.x:0.##} x {size.y:0.##}", labelStyle);
    }
#endif
}
