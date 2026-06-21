using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies the project's pixel-art import profile to every Texture2D asset.
/// Textures whose file names end in "_nmap" or "_normal" are imported as normal maps.
/// </summary>
public static class PixelArtTextureImportUtility
{
    private const string ArtFolderPath = "Assets/Art";
    private const int MaxTextureSize = 512;

    private enum TextureUsage
    {
        Sprite,
        Material,
        NormalMap
    }

    [MenuItem("Tools/Room Horror/Textures/Configure All 2D Textures for Pixel Art")]
    private static void ConfigureAllTextures()
    {
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolderPath });
        HashSet<string> materialTexturePaths = FindMaterialTexturePaths();

        if (!EditorUtility.DisplayDialog(
                "Configure 2D Textures",
                $"Apply pixel-art import settings to {textureGuids.Length} Texture2D assets in {ArtFolderPath}?\n\n" +
                "Normal maps use the '_nmap' or '_normal' suffix. Textures used by materials remain Default; all other textures become Sprites.",
                "Apply Settings",
                "Cancel"))
        {
            return;
        }

        int changedCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < textureGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                TextureUsage usage = GetTextureUsage(assetPath, materialTexturePaths);

                EditorUtility.DisplayProgressBar(
                    "Configuring Pixel-Art Textures",
                    assetPath,
                    (float)i / textureGuids.Length);

                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                ConfigureImporter(importer, usage);
                importer.SaveAndReimport();
                changedCount++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"Pixel-art texture import settings applied to {changedCount} texture(s).");
        EditorUtility.DisplayDialog(
            "Configure 2D Textures",
            $"Pixel-art settings applied to {changedCount} texture(s).",
            "OK");
    }

    private static HashSet<string> FindMaterialTexturePaths()
    {
        var texturePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

        foreach (string materialGuid in materialGuids)
        {
            string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
            foreach (string dependencyPath in AssetDatabase.GetDependencies(materialPath, false))
            {
                if (dependencyPath.StartsWith(ArtFolderPath + "/", StringComparison.OrdinalIgnoreCase))
                {
                    texturePaths.Add(dependencyPath);
                }
            }
        }

        return texturePaths;
    }

    private static TextureUsage GetTextureUsage(string assetPath, HashSet<string> materialTexturePaths)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        bool isNormalMap = fileName.EndsWith("_nmap", StringComparison.OrdinalIgnoreCase) ||
                           fileName.EndsWith("_normal", StringComparison.OrdinalIgnoreCase);

        if (isNormalMap)
        {
            return TextureUsage.NormalMap;
        }

        return materialTexturePaths.Contains(assetPath)
            ? TextureUsage.Material
            : TextureUsage.Sprite;
    }

    private static void ConfigureImporter(TextureImporter importer, TextureUsage usage)
    {
        importer.textureType = usage == TextureUsage.NormalMap
            ? TextureImporterType.NormalMap
            : usage == TextureUsage.Material
                ? TextureImporterType.Default
                : TextureImporterType.Sprite;
        importer.sRGBTexture = usage != TextureUsage.NormalMap;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.maxTextureSize = MaxTextureSize;
    }
}
