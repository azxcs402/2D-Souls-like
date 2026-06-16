using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy_ReaperVisionAnchor : MonoBehaviour
{
    [SerializeField, Min(.1f)] private float frontSightDistance = 4f;
    [SerializeField, Min(.1f)] private float backSightDistance = 2f;
    [SerializeField, Min(.1f)] private float chaseVerticalDistance = 3f;

    public Enemy_Reaper Reaper => GetComponentInParent<Enemy_Reaper>();
    public float FrontSightDistance => frontSightDistance;
    public float BackSightDistance => backSightDistance;
    public float ChaseVerticalDistance => chaseVerticalDistance;

    private void OnValidate()
    {
        frontSightDistance = Mathf.Max(.1f, frontSightDistance);
        backSightDistance = Mathf.Max(.1f, backSightDistance);
        chaseVerticalDistance = Mathf.Max(.1f, chaseVerticalDistance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Reaper == null)
        {
            return;
        }

        DrawOutline(transform.position, frontSightDistance, backSightDistance, chaseVerticalDistance, "Vision");
    }

    public static void DrawOutline(Vector3 center, float frontSightDistance, float backSightDistance, float chaseVerticalDistance, string labelPrefix)
    {
        float totalWidth = Mathf.Max(.1f, frontSightDistance + backSightDistance);
        float totalHeight = Mathf.Max(.1f, chaseVerticalDistance * 2f);
        Vector3 drawCenter = center + new Vector3((frontSightDistance - backSightDistance) * .5f, 0f, 0f);
        Vector3 half = new Vector3(totalWidth * .5f, totalHeight * .5f, 0f);
        Vector3[] corners =
        {
            drawCenter + new Vector3(-half.x, -half.y, 0f),
            drawCenter + new Vector3(-half.x, half.y, 0f),
            drawCenter + new Vector3(half.x, half.y, 0f),
            drawCenter + new Vector3(half.x, -half.y, 0f)
        };

        Color wireColor = new Color(1f, .85f, .2f, 1f);
        Handles.color = wireColor;
        Handles.DrawAAPolyLine(3f, corners[0], corners[1], corners[2], corners[3], corners[0]);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(drawCenter + Vector3.up * (half.y + .12f), $"{labelPrefix} / {frontSightDistance:0.##} front, {backSightDistance:0.##} back", labelStyle);
    }
#endif
}
