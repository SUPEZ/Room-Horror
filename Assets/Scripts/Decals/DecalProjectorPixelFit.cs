using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Fits a URP Decal Projector's width and height to its material's main texture.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(DecalProjector))]
public sealed class DecalProjectorPixelFit : MonoBehaviour
{
    private enum TextureSource
    {
        ProjectorMaterial,
        ReferenceMaterial,
        RelativeToReferenceMaterial,
        ReferenceRendererPixelDensity
    }

    [SerializeField] private TextureSource textureSource = TextureSource.ProjectorMaterial;

    [Tooltip("Used only when Texture Source is set to Reference Material.")]
    [SerializeField] private Material referenceMaterial;

    [Tooltip("Wall or floor renderer whose texture pixel density should be matched. Used only in Reference Renderer Pixel Density mode.")]
    [SerializeField] private Renderer referenceRenderer;

    [Tooltip("World-space width and height occupied by one full reference texture. Used only in Relative To Reference Material mode.")]
    [SerializeField] private Vector2 referenceProjectionSize = Vector2.one;

    [Tooltip("How many texture pixels occupy one world unit. Lower values make the projected decal larger.")]
    [Min(1f)]
    [SerializeField] private float pixelsPerUnit = 32f;

    [Tooltip("Updates the projector whenever this component is edited in the Inspector.")]
    [SerializeField] private bool updateInEditor = true;

    private static readonly int BaseMapId = Shader.PropertyToID("Base_Map");
    private static readonly int UrpBaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");

    private DecalProjector decalProjector;

    private void Reset()
    {
        FitToMainTexture();
    }

    private void OnValidate()
    {
        if (updateInEditor)
        {
            FitToMainTexture();
        }
    }

    [ContextMenu("Fit Projection to Main Texture")]
    public void FitToMainTexture()
    {
        DecalProjector projector = GetProjector();
        if (projector == null)
        {
            return;
        }

        if (textureSource == TextureSource.RelativeToReferenceMaterial)
        {
            FitRelativeToReference(projector);
            return;
        }

        if (textureSource == TextureSource.ReferenceRendererPixelDensity)
        {
            FitToReferenceRenderer(projector);
            return;
        }

        Material sourceMaterial = textureSource == TextureSource.ReferenceMaterial
            ? referenceMaterial
            : projector.material;
        if (sourceMaterial == null)
        {
            return;
        }

        Texture texture = GetMainTexture(sourceMaterial);
        if (texture == null || texture.width == 0 || texture.height == 0)
        {
            return;
        }

        Vector3 size = projector.size;
        size.x = texture.width / pixelsPerUnit;
        size.y = texture.height / pixelsPerUnit;
        ApplyProjectionSize(projector, size);
    }

    [ContextMenu("Use Current Projector Size as Reference")]
    public void UseCurrentProjectorSizeAsReference()
    {
        DecalProjector projector = GetProjector();
        if (projector == null)
        {
            return;
        }

        referenceProjectionSize = new Vector2(projector.size.x, projector.size.y);
    }

    private void FitRelativeToReference(DecalProjector projector)
    {
        if (projector.material == null || referenceMaterial == null)
        {
            return;
        }

        Texture decalTexture = GetMainTexture(projector.material);
        Texture referenceTexture = GetMainTexture(referenceMaterial);
        if (decalTexture == null || referenceTexture == null ||
            referenceTexture.width == 0 || referenceTexture.height == 0)
        {
            return;
        }

        Vector2 decalTiling = GetMainTextureTiling(projector.material);
        Vector2 referenceTiling = GetMainTextureTiling(referenceMaterial);
        float referencePixelsX = referenceTexture.width * Mathf.Max(Mathf.Abs(referenceTiling.x), 0.0001f);
        float referencePixelsY = referenceTexture.height * Mathf.Max(Mathf.Abs(referenceTiling.y), 0.0001f);

        Vector3 size = projector.size;
        size.x = referenceProjectionSize.x * decalTexture.width * Mathf.Abs(decalTiling.x) / referencePixelsX;
        size.y = referenceProjectionSize.y * decalTexture.height * Mathf.Abs(decalTiling.y) / referencePixelsY;
        ApplyProjectionSize(projector, size);
    }

    private void FitToReferenceRenderer(DecalProjector projector)
    {
        if (projector.material == null || referenceRenderer == null)
        {
            return;
        }

        Material wallMaterial = referenceMaterial != null
            ? referenceMaterial
            : referenceRenderer.sharedMaterial;
        if (wallMaterial == null)
        {
            return;
        }

        Texture decalTexture = GetMainTexture(projector.material);
        Texture wallTexture = GetMainTexture(wallMaterial);
        if (decalTexture == null || wallTexture == null || wallTexture.width == 0 || wallTexture.height == 0)
        {
            return;
        }

        Vector2 wallSize = GetSizeOnProjectorPlane(referenceRenderer.bounds, projector.transform);
        Vector2 decalTiling = GetMainTextureTiling(projector.material);
        Vector2 wallTiling = GetMainTextureTiling(wallMaterial);
        float wallPixelsX = wallTexture.width * Mathf.Max(Mathf.Abs(wallTiling.x), 0.0001f);
        float wallPixelsY = wallTexture.height * Mathf.Max(Mathf.Abs(wallTiling.y), 0.0001f);

        Vector3 size = projector.size;
        size.x = wallSize.x * decalTexture.width * Mathf.Abs(decalTiling.x) / wallPixelsX;
        size.y = wallSize.y * decalTexture.height * Mathf.Abs(decalTiling.y) / wallPixelsY;
        ApplyProjectionSize(projector, size);
    }

    private DecalProjector GetProjector()
    {
        if (decalProjector == null)
        {
            decalProjector = GetComponent<DecalProjector>();
        }

        return decalProjector;
    }

    private static Texture GetMainTexture(Material material)
    {
        if (material.HasProperty(BaseMapId))
        {
            return material.GetTexture(BaseMapId);
        }

        if (material.HasProperty(UrpBaseMapId))
        {
            return material.GetTexture(UrpBaseMapId);
        }

        return material.HasProperty(MainTextureId)
            ? material.GetTexture(MainTextureId)
            : material.mainTexture;
    }

    private static Vector2 GetMainTextureTiling(Material material)
    {
        if (material.HasProperty(BaseMapId))
        {
            return material.GetTextureScale(BaseMapId);
        }

        if (material.HasProperty(UrpBaseMapId))
        {
            return material.GetTextureScale(UrpBaseMapId);
        }

        return material.HasProperty(MainTextureId)
            ? material.GetTextureScale(MainTextureId)
            : material.mainTextureScale;
    }

    private static Vector2 GetSizeOnProjectorPlane(Bounds bounds, Transform projectorTransform)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 localPoint = projectorTransform.InverseTransformPoint(corner);
                    minX = Mathf.Min(minX, localPoint.x);
                    maxX = Mathf.Max(maxX, localPoint.x);
                    minY = Mathf.Min(minY, localPoint.y);
                    maxY = Mathf.Max(maxY, localPoint.y);
                }
            }
        }

        return new Vector2(maxX - minX, maxY - minY);
    }

    private static void ApplyProjectionSize(DecalProjector projector, Vector3 size)
    {
        projector.size = size;
        projector.pivot = new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
    }
}
