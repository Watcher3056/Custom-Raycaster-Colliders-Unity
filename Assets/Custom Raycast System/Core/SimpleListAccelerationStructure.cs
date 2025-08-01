using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UMathf = UnityEngine.Mathf;
#else
    // Assuming CustomMath.cs and CRay.cs are in the same namespace or accessible
#endif

// A basic acceleration structure that uses a simple list (linear search).
public class SimpleListAccelerationStructure : IAccelerationStructure
{
    private List<IPrimitive> _primitives = new List<IPrimitive>();

    public void AddPrimitive(IPrimitive primitive)
    {
        _primitives.Add(primitive);
    }

    public void RemovePrimitive(IPrimitive primitive)
    {
        _primitives.Remove(primitive);
    }

    public void UpdatePrimitive(IPrimitive primitive)
    {
        // No specific update logic needed for a simple list,
        // as primitives are referenced directly.
        // However, if AABB is cached, it should be recalculated.
        primitive.CalculateAABB(out UVector3 min, out UVector3 max);
        // (AABB is updated within the primitive itself)
    }

    public List<IPrimitive> QueryRay(CRay ray, float maxDistance)
    {
        List<IPrimitive> potentialHits = new List<IPrimitive>();
        foreach (var primitive in _primitives)
        {
            // Simple AABB check before detailed intersection for minor optimization
            if (RayIntersectsAABB(ray, primitive.AABBMin, primitive.AABBMax, maxDistance))
            {
                potentialHits.Add(primitive);
            }
        }
        return potentialHits;
    }

    // Helper for AABB intersection (used by QuadTree and SimpleList)
    private bool RayIntersectsAABB(CRay ray, UVector3 minBounds, UVector3 maxBounds, float maxDistance)
    {
        float tMin = 0.0f;
        float tMax = maxDistance;

        for (int i = 0; i < 3; i++)
        {
            float invDir = 1.0f / ray.Direction[i];
            float t0 = (minBounds[i] - ray.Origin[i]) * invDir;
            float t1 = (maxBounds[i] - ray.Origin[i]) * invDir;

            if (invDir < 0.0f)
            {
                float temp = t0;
                t0 = t1;
                t1 = temp;
            }

            tMin = UMathf.Max(t0, tMin);
            tMax = UMathf.Min(t1, tMax);

            if (tMax < tMin) return false;
        }
        return tMin < maxDistance && tMax > UMathf.Epsilon;
    }
}
