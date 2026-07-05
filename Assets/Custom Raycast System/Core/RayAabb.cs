#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UMathf = UnityEngine.Mathf;
#endif

// Shared slab test keeps both broad phases in sync (the QuadTree==SimpleList differential relies on it).
public static class RayAabb
{
    public static bool Intersects(CRay ray, UVector3 minBounds, UVector3 maxBounds, float maxDistance)
    {
        RaycastDiagnostics.CountAabb();
        float tMin = 0.0f;
        float tMax = maxDistance;

        for (int i = 0; i < 3; i++)
        {
            float dir = ray.Direction[i];
            if (UMathf.Abs(dir) < 1e-12f)
            {
                // Ray parallel to this slab: miss if origin is outside the slab.
                if (ray.Origin[i] < minBounds[i] || ray.Origin[i] > maxBounds[i]) return false;
                continue;
            }
            float invDir = 1.0f / dir;
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
