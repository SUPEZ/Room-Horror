using FIMSpace.Generating;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Shows one deterministic random decal in one of the projector slots below a floor prefab.
/// </summary>
public sealed class FloorRandomDecalController : MonoBehaviour
{
    [SerializeField, Tooltip("Floor collider whose top side limits the decal projection.")]
    private Collider floorCollider;
    [SerializeField, Tooltip("Hierarchy root containing the floor's Decal Projector slots. Defaults to this transform.")]
    private Transform decalRoot;
    [SerializeField, Min(0f), Tooltip("Extra space kept between the decal and collider edges.")]
    private float colliderEdgePadding = 0.01f;
    [SerializeField] private Material[] decalMaterials;
    [SerializeField, Tooltip("Relative chance for each matching decal material. Zero disables that material.")]
    private float[] materialWeights;

    private DecalProjector[] decalSlots;
    private int generationSeed;

    private void Reset()
    {
        floorCollider = GetComponent<Collider>();
        decalRoot = transform;
    }

    private void Awake()
    {
        CacheHierarchy();
        generationSeed = FGenerators.LatestSeed;
    }

    private void OnEnable()
    {
        if (decalSlots == null)
        {
            CacheHierarchy();
        }

        RefreshDecals();
    }

    private void OnDisable()
    {
        RandomDecalControllerBase.DisableAllDecals(decalSlots);
    }

    [ContextMenu("Refresh Random Floor Decal")]
    private void RefreshDecals()
    {
        if (decalSlots == null)
        {
            CacheHierarchy();
        }

        RandomDecalControllerBase.RefreshDecals(
            decalSlots,
            null,
            decalMaterials,
            materialWeights,
            floorCollider,
            colliderEdgePadding,
            RandomDecalControllerBase.GetStableSeed(generationSeed, transform.position),
            out _,
            out _);
    }

    private void CacheHierarchy()
    {
        if (floorCollider == null)
        {
            floorCollider = GetComponent<Collider>();
            if (floorCollider == null)
            {
                floorCollider = GetComponentInChildren<Collider>(true);
            }
        }

        if (decalRoot == null)
        {
            decalRoot = transform;
        }

        decalSlots = decalRoot.GetComponentsInChildren<DecalProjector>(true);
    }
}
