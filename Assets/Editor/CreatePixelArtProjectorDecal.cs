using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates a local copy of URP's Decal Shader Graph, which Decal Projector can render.
/// </summary>
public static class CreatePixelArtProjectorDecal
{
    private const string SourceGraphPath = "Packages/com.unity.render-pipelines.universal/Shaders/Decal.shadergraph";
    private const string GraphPath = "Assets/Art/Shaders/PixelArtProjectorDecal.shadergraph";
    private const string MaterialPath = "Assets/Art/Shaders/PixelArtProjectorDecal.mat";

    [MenuItem("Tools/Room Horror/Decals/Create Pixel-Art Projector Decal")]
    private static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(GraphPath) != null)
        {
            EditorUtility.DisplayDialog(
                "Pixel-Art Projector Decal",
                "The decal Shader Graph already exists at " + GraphPath + ".",
                "OK");
            return;
        }

        if (!AssetDatabase.CopyAsset(SourceGraphPath, GraphPath))
        {
            Debug.LogError("Could not copy URP's Decal Shader Graph.");
            return;
        }

        AssetDatabase.ImportAsset(GraphPath, ImportAssetOptions.ForceSynchronousImport);
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(GraphPath);
        if (shader == null)
        {
            Debug.LogError("The copied Decal Shader Graph did not import as a Shader.");
            return;
        }

        var material = new Material(shader)
        {
            name = "PixelArtProjectorDecal"
        };
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(material);

        Debug.Log("Created Pixel-Art Projector Decal Shader Graph and material in Assets/Art/Shaders.");
    }
}
