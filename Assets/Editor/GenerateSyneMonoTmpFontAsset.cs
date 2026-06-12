using System;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class GenerateSyneMonoTmpFontAsset
{
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/SyneMono-Regular.ttf";
    private const string OutputFontAssetPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/SyneMono-Regular SDF.asset";

    [MenuItem("Tools/TextMeshPro/Generate SyneMono Font Asset")]
    public static void Run()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            throw new InvalidOperationException($"Source font not found at '{SourceFontPath}'.");
        }

        TMP_FontAsset existingAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputFontAssetPath);
        if (existingAsset != null)
        {
            Debug.Log($"TMP font asset already exists: {OutputFontAssetPath}");
            return;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset == null)
        {
            throw new InvalidOperationException("TMP_FontAsset.CreateFontAsset returned null.");
        }

        fontAsset.name = "SyneMono-Regular SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        AssetDatabase.CreateAsset(fontAsset, OutputFontAssetPath);
        AddSubAssetIfNeeded(fontAsset.material, "SyneMono-Regular SDF Material", fontAsset);

        foreach (Texture2D atlasTexture in fontAsset.atlasTextures)
        {
            AddSubAssetIfNeeded(atlasTexture, atlasTexture != null ? atlasTexture.name : "Atlas", fontAsset);
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated TMP font asset: {OutputFontAssetPath}");
    }

    private static void AddSubAssetIfNeeded(UnityEngine.Object asset, string fallbackName, TMP_FontAsset parentAsset)
    {
        if (asset == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset)))
        {
            return;
        }

        asset.name = string.IsNullOrEmpty(asset.name) ? fallbackName : asset.name;
        AssetDatabase.AddObjectToAsset(asset, parentAsset);
    }
}
