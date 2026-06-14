using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy_MageProjectileHoverAreaAnchor : MonoBehaviour
{
    [SerializeField, Min(.1f)] private Vector2 hoverAreaSize = new Vector2(4f, 1.8f);

    public Enemy_Mage Mage => GetComponentInParent<Enemy_Mage>();
    public Vector2 HoverAreaSize => hoverAreaSize;

    private void OnValidate()
    {
        hoverAreaSize = new Vector2(
            Mathf.Max(.1f, hoverAreaSize.x),
            Mathf.Max(.1f, hoverAreaSize.y)
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Enemy_Mage mage = Mage;
        if (mage == null)
        {
            return;
        }

        Vector3 center = transform.position;
        Vector2 size = HoverAreaSize;

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

        Color fillColor = new Color(.35f, .8f, 1f, .12f);
        Color wireColor = new Color(.35f, .8f, 1f, 1f);

        Handles.DrawSolidRectangleWithOutline(corners, fillColor, wireColor);

        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal =
            {
                textColor = wireColor
            }
        };

        Handles.Label(center + Vector3.up * (size.y * .5f + .12f), $"Spell Hover Area / {size.x:0.##} x {size.y:0.##}", labelStyle);
    }
#endif
}
