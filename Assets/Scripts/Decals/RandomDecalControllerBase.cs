using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Shared stateless operations used by random wall and floor decal controllers.
/// </summary>
public static class RandomDecalControllerBase
{
    public static int GetStableSeed(int generationSeed, Vector3 position)
    {
        unchecked
        {
            int seed = generationSeed;
            seed = seed * 486187739 + Mathf.RoundToInt(position.x * 1000f);
            seed = seed * 486187739 + Mathf.RoundToInt(position.y * 1000f);
            seed = seed * 486187739 + Mathf.RoundToInt(position.z * 1000f);
            return seed;
        }
    }

    public static void RefreshDecals(
        DecalProjector[] decalSlots,
        DecalProjectorPixelFit[] decalScalers,
        Material[] decalMaterials,
        float[] materialWeights,
        Collider surfaceCollider,
        float colliderEdgePadding,
        int stableSeed,
        out int activeDecalIndex,
        out int activeMaterialIndex)
    {
        activeDecalIndex = -1;
        activeMaterialIndex = -1;

        if (decalSlots == null || decalSlots.Length == 0 ||
            decalMaterials == null || decalMaterials.Length == 0)
        {
            DisableAllDecals(decalSlots);
            return;
        }

        var random = new System.Random(stableSeed);
        activeDecalIndex = GetRandomSlotIndex(decalSlots, random);
        activeMaterialIndex = GetWeightedMaterialIndex(decalMaterials, materialWeights, random);

        if (activeDecalIndex >= 0 && activeMaterialIndex >= 0)
        {
            DecalProjector activeProjector = decalSlots[activeDecalIndex];
            activeProjector.material = decalMaterials[activeMaterialIndex];

            DecalProjectorPixelFit scaler = GetScaler(activeProjector, decalScalers, activeDecalIndex);
            if (scaler != null)
            {
                scaler.FitToMainTexture();
            }

            FitInsideCollider(activeProjector, surfaceCollider, colliderEdgePadding);
        }

        for (int i = 0; i < decalSlots.Length; i++)
        {
            if (decalSlots[i] != null)
            {
                decalSlots[i].enabled = i == activeDecalIndex && activeMaterialIndex >= 0;
            }
        }
    }

    public static void DisableAllDecals(DecalProjector[] decalSlots)
    {
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

    public static void FitInsideCollider(
        DecalProjector projector,
        Collider surfaceCollider,
        float colliderEdgePadding)
    {
        if (projector == null || surfaceCollider == null)
        {
            return;
        }

        Bounds surfaceBounds = GetColliderBoundsOnProjectorPlane(surfaceCollider, projector);
        float padding = Mathf.Max(0f, colliderEdgePadding);
        float availableWidth = surfaceBounds.size.x - padding * 2f;
        float availableHeight = surfaceBounds.size.y - padding * 2f;
        Vector3 size = projector.size;

        if (availableWidth <= 0f || availableHeight <= 0f || size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Min(1f, availableWidth / size.x, availableHeight / size.y);
        size.x *= scale;
        size.y *= scale;
        projector.size = size;

        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;
        Vector3 pivot = projector.pivot;
        pivot.x = Mathf.Clamp(
            pivot.x,
            surfaceBounds.min.x + padding + halfWidth,
            surfaceBounds.max.x - padding - halfWidth);
        pivot.y = Mathf.Clamp(
            pivot.y,
            surfaceBounds.min.y + padding + halfHeight,
            surfaceBounds.max.y - padding - halfHeight);
        projector.pivot = pivot;
    }

    private static DecalProjectorPixelFit GetScaler(
        DecalProjector projector,
        DecalProjectorPixelFit[] decalScalers,
        int slotIndex)
    {
        if (decalScalers != null &&
            slotIndex >= 0 &&
            slotIndex < decalScalers.Length &&
            decalScalers[slotIndex] != null)
        {
            return decalScalers[slotIndex];
        }

        return projector.GetComponent<DecalProjectorPixelFit>();
    }

    private static int GetRandomSlotIndex(DecalProjector[] decalSlots, System.Random random)
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

    private static int GetWeightedMaterialIndex(
        Material[] decalMaterials,
        float[] materialWeights,
        System.Random random)
    {
        float totalWeight = 0f;
        for (int i = 0; i < decalMaterials.Length; i++)
        {
            if (decalMaterials[i] != null)
            {
                totalWeight += GetMaterialWeight(materialWeights, i);
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

            roll -= GetMaterialWeight(materialWeights, i);
            if (roll < 0f)
            {
                return i;
            }
        }

        return -1;
    }

    private static float GetMaterialWeight(float[] materialWeights, int index)
    {
        if (materialWeights == null || index >= materialWeights.Length)
        {
            return 1f;
        }

        return Mathf.Max(0f, materialWeights[index]);
    }

    private static Bounds GetColliderBoundsOnProjectorPlane(
        Collider targetCollider,
        DecalProjector projector)
    {
        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, 0f);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, 0f);

        if (targetCollider is BoxCollider boxCollider)
        {
            Vector3 halfSize = boxCollider.size * 0.5f;
            AddLocalBoundsCorners(
                boxCollider.transform,
                boxCollider.center,
                halfSize,
                projector,
                ref min,
                ref max);
        }
        else
        {
            AddWorldBoundsCorners(targetCollider.bounds, projector, ref min, ref max);
        }

        Bounds result = new Bounds();
        result.SetMinMax(min, max);
        return result;
    }

    private static void AddLocalBoundsCorners(
        Transform sourceTransform,
        Vector3 center,
        Vector3 extents,
        DecalProjector projector,
        ref Vector3 min,
        ref Vector3 max)
    {
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 sourcePoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                    Vector3 worldPoint = sourceTransform.TransformPoint(sourcePoint);
                    AddPoint(WorldToProjectorPoint(projector, worldPoint), ref min, ref max);
                }
            }
        }
    }

    private static void AddWorldBoundsCorners(
        Bounds bounds,
        DecalProjector projector,
        ref Vector3 min,
        ref Vector3 max)
    {
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 worldPoint = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    AddPoint(WorldToProjectorPoint(projector, worldPoint), ref min, ref max);
                }
            }
        }
    }

    private static Vector3 WorldToProjectorPoint(DecalProjector projector, Vector3 worldPoint)
    {
        if (projector.scaleMode == DecalScaleMode.InheritFromHierarchy)
        {
            return projector.transform.InverseTransformPoint(worldPoint);
        }

        return Quaternion.Inverse(projector.transform.rotation) *
               (worldPoint - projector.transform.position);
    }

    private static void AddPoint(Vector3 point, ref Vector3 min, ref Vector3 max)
    {
        min.x = Mathf.Min(min.x, point.x);
        min.y = Mathf.Min(min.y, point.y);
        max.x = Mathf.Max(max.x, point.x);
        max.y = Mathf.Max(max.y, point.y);
    }
}
