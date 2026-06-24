using UnityEngine;
using UnityEngine.UI;

public static class UIRectangularButtonUtility
{
    public static void MakeRectangular(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.useSpriteMesh = false;
        image.preserveAspect = false;
    }

    public static void MakeRectangular(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            MakeRectangular(buttons[i]);
        }
    }

    public static void NormalizeMainMenuLayerOrder(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Transform backdrop = FindDeepChild(root, "Backdrop");
        Transform backdropLeft = FindDeepChild(root, "BackdropAccentLeft");
        Transform backdropRight = FindDeepChild(root, "BackdropAccentRight");
        Transform mainPanel = FindDeepChild(root, "MainPanel");
        Transform optionsPanel = FindDeepChild(root, "OptionsPanel");

        int siblingIndex = 0;
        SetBackdropOrder(backdrop, ref siblingIndex);
        SetBackdropOrder(backdropLeft, ref siblingIndex);
        SetBackdropOrder(backdropRight, ref siblingIndex);

        if (mainPanel != null)
        {
            mainPanel.SetSiblingIndex(siblingIndex++);
        }

        if (optionsPanel != null)
        {
            optionsPanel.SetSiblingIndex(siblingIndex);
        }
    }

    private static void SetBackdropOrder(Transform target, ref int siblingIndex)
    {
        if (target == null)
        {
            return;
        }

        target.SetSiblingIndex(siblingIndex++);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        foreach (Transform child in root)
        {
            if (child.name == childName)
            {
                return child;
            }

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
