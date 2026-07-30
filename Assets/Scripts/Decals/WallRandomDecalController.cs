using FIMSpace.Generating;
using LevelGeneration;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Shows one deterministic random decal material in one wall slot while the non-perpendicular wall is active.
/// </summary>
public sealed class WallRandomDecalController : MonoBehaviour
{
    [SerializeField] private OrientationSwitcher orientationSwitcher;
    [SerializeField] private GameObject wallNoPerp;
    [SerializeField, Tooltip("Collider whose wall-facing side limits the decal projection.")]
    private Collider wallCollider;
    [SerializeField, Min(0f), Tooltip("Extra space kept between the decal and collider edges.")]
    private float colliderEdgePadding = 0.01f;
    [SerializeField, Tooltip("Wall positions where a selected decal material can be projected.")]
    private DecalProjector[] decalSlots;
    [SerializeField, Tooltip("Pixel-fit components matching Decal Slots by index.")]
    private DecalProjectorPixelFit[] decalScalers;
    [SerializeField] private Material[] decalMaterials;
    [SerializeField, Tooltip("Relative chance for each matching decal material. Zero disables that material.")]
    private float[] materialWeights;

    private int activeDecalIndex = -1;
    private int activeMaterialIndex = -1;
    private int generationSeed;

    private void Awake()
    {
        if (orientationSwitcher == null)
        {
            orientationSwitcher = GetComponent<OrientationSwitcher>();
        }

        if (wallCollider == null)
        {
            wallCollider = GetComponentInChildren<Collider>(true);
        }

        generationSeed = FGenerators.LatestSeed;
    }

    private void OnEnable()
    {
        if (orientationSwitcher != null)
        {
            orientationSwitcher.OrientationChanged += OnOrientationChanged;
        }
    }

    private void Start()
    {
        RefreshDecals();
    }

    private void Update()
    {
        if (wallNoPerp == null || !wallNoPerp.activeInHierarchy)
        {
            DisableAllDecals();
            return;
        }

        if (activeDecalIndex < 0)
        {
            RefreshDecals();
        }
    }

    private void OnDisable()
    {
        if (orientationSwitcher != null)
        {
            orientationSwitcher.OrientationChanged -= OnOrientationChanged;
        }

        DisableAllDecals();
    }

    private void OnOrientationChanged(OrientationSwitcher.Orientation orientation)
    {
        RefreshDecals();
    }

    private void RefreshDecals()
    {
        if (wallNoPerp == null || !wallNoPerp.activeInHierarchy)
        {
            DisableAllDecals();
            return;
        }

        RandomDecalControllerBase.RefreshDecals(
            decalSlots,
            decalScalers,
            decalMaterials,
            materialWeights,
            wallCollider,
            colliderEdgePadding,
            RandomDecalControllerBase.GetStableSeed(generationSeed, transform.position),
            out activeDecalIndex,
            out activeMaterialIndex);
    }

    private void DisableAllDecals()
    {
        activeDecalIndex = -1;
        activeMaterialIndex = -1;

        RandomDecalControllerBase.DisableAllDecals(decalSlots);
    }
}
