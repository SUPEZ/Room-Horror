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

        if (decalSlots == null || decalSlots.Length == 0 || decalMaterials == null || decalMaterials.Length == 0)
        {
            DisableAllDecals();
            return;
        }

        var random = new System.Random(GetStableSeed());
        activeDecalIndex = GetRandomSlotIndex(random);
        activeMaterialIndex = GetWeightedMaterialIndex(random);

        if (activeDecalIndex >= 0 && activeMaterialIndex >= 0)
        {
            DecalProjector activeProjector = decalSlots[activeDecalIndex];
            activeProjector.material = decalMaterials[activeMaterialIndex];

            if (decalScalers != null && activeDecalIndex < decalScalers.Length && decalScalers[activeDecalIndex] != null)
            {
                decalScalers[activeDecalIndex].FitToMainTexture();
            }
        }

        for (int i = 0; i < decalSlots.Length; i++)
        {
            if (decalSlots[i] != null)
            {
                decalSlots[i].enabled = i == activeDecalIndex && activeMaterialIndex >= 0;
            }
        }
    }

    private void DisableAllDecals()
    {
        activeDecalIndex = -1;
        activeMaterialIndex = -1;

        if (decalSlots == null)
        {
            return;
        }

        foreach (DecalProjector decalSlot in decalSlots)
        {
            if (decalSlot != null)
            {
                decalSlot.enabled = false;
            }
        }
    }

    private int GetStableSeed()
    {
        Vector3 position = transform.position;

        unchecked
        {
            int seed = generationSeed;
            seed = seed * 486187739 + Mathf.RoundToInt(position.x * 1000f);
            seed = seed * 486187739 + Mathf.RoundToInt(position.y * 1000f);
            seed = seed * 486187739 + Mathf.RoundToInt(position.z * 1000f);
            return seed;
        }
    }

    private int GetRandomSlotIndex(System.Random random)
    {
        int validSlotCount = 0;
        foreach (DecalProjector decalSlot in decalSlots)
        {
            if (decalSlot != null)
            {
                validSlotCount++;
            }
        }

        if (validSlotCount == 0)
        {
            return -1;
        }

        int target = random.Next(0, validSlotCount);
        for (int i = 0; i < decalSlots.Length; i++)
        {
            if (decalSlots[i] != null && target-- == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetWeightedMaterialIndex(System.Random random)
    {
        float totalWeight = 0f;
        for (int i = 0; i < decalMaterials.Length; i++)
        {
            if (decalMaterials[i] != null)
            {
                totalWeight += GetMaterialWeight(i);
            }
        }

        if (totalWeight <= 0f)
        {
            return -1;
        }

        float roll = (float)random.NextDouble() * totalWeight;
        for (int i = 0; i < decalMaterials.Length; i++)
        {
            if (decalMaterials[i] == null)
            {
                continue;
            }

            roll -= GetMaterialWeight(i);
            if (roll < 0f)
            {
                return i;
            }
        }

        return -1;
    }

    private float GetMaterialWeight(int index)
    {
        if (materialWeights == null || index >= materialWeights.Length)
        {
            return 1f;
        }

        return Mathf.Max(0f, materialWeights[index]);
    }
}
