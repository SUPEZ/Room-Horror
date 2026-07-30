using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates PixelArtProjectorDecal materials next to the selected Texture2D assets.
/// </summary>
public static class CreateDecalMaterialsFromTextures
{
    private const string MenuPath = "Assets/Room Horror/Create Decal Materials from Textures";
    private const string DecalShaderPath = "Assets/Art/Shaders/PixelArtProjectorDecal.shadergraph";
    private const string MaterialSuffix = "_decal_mat";

    private static readonly int BaseMapId = Shader.PropertyToID("Base_Map");
    private static readonly int UrpBaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");

    [MenuItem(MenuPath, false, 2000)]
    private static void CreateMaterials()
    {
        Texture2D[] textures = GetSelectedTextures();
        Shader decalShader = AssetDatabase.LoadAssetAtPath<Shader>(DecalShaderPath);
        if (decalShader == null)
        {
            EditorUtility.DisplayDialog(
                "Create Decal Materials",
                $"Decal shader was not found at:\n{DecalShaderPath}",
                "OK");
            return;
        }

        var createdMaterials = new List<Material>();
        int skippedCount = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (Texture2D texture in textures)
            {
                string texturePath = AssetDatabase.GetAssetPath(texture);
                string folderPath = Path.GetDirectoryName(texturePath)?.Replace('\\', '/');
                if (string.IsNullOrEmpty(folderPath))
                {
                    skippedCount++;
                    continue;
                }

                string textureFileName = Path.GetFileNameWithoutExtension(texturePath);
                string materialPath = $"{folderPath}/{textureFileName}{MaterialSuffix}.mat";
                if (AssetDatabase.LoadMainAssetAtPath(materialPath) != null)
                {
                    skippedCount++;
                    continue;
                }

                var material = new Material(decalShader)
                {
                    name = textureFileName + MaterialSuffix
                };

                SetTextureIfSupported(material, BaseMapId, texture);
                SetTextureIfSupported(material, UrpBaseMapId, texture);
                SetTextureIfSupported(material, MainTextureId, texture);

                AssetDatabase.CreateAsset(material, materialPath);
                createdMaterials.Add(material);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        if (createdMaterials.Count > 0)
        {
            Selection.objects = createdMaterials.ToArray();
            EditorGUIUtility.PingObject(createdMaterials[0]);
        }

        Debug.Log(
            $"Created {createdMaterials.Count} decal material(s) next to selected textures. " +
            $"Skipped {skippedCount} existing or invalid asset(s).");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanCreateMaterials()
    {
        return GetSelectedTextures().Length > 0;
    }

    private static Texture2D[] GetSelectedTextures()
    {
        var textures = new List<Texture2D>();

        foreach (Object selectedObject in Selection.objects)
        {
            if (selectedObject is not Texture2D texture)
            {
                continue;
            }

            string path = AssetDatabase.GetAssetPath(texture);
            if (path.StartsWith("Assets/"))
            {
                textures.Add(texture);
            }
        }

        return textures.ToArray();
    }

    private static void SetTextureIfSupported(Material material, int propertyId, Texture texture)
    {
        if (material.HasProperty(propertyId))
        {
            material.SetTexture(propertyId, texture);
        }
    }
}
