#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMatrix4x4 = UnityEngine.Matrix4x4;
using UMathf = UnityEngine.Mathf;
#else
    // Assuming CustomMath.cs is in the same namespace or accessible
#endif

public class BoxPrimitive : IPrimitive
{
    public int ID { get; set; }
    public UVector3 Position { get; set; }
    public UQuaternion Rotation { get; set; }
    public UVector3 Size { get; set; } // This is the scale of the object

    public UVector3 AABBMin { get; private set; }
    public UVector3 AABBMax { get; private set; }

    private UMatrix4x4 _worldToLocalMatrix;
    private UMatrix4x4 _localToWorldMatrix; // Store this for normal transformation

    public BoxPrimitive(int id, UVector3 position, UQuaternion rotation, UVector3 size)
    {
        ID = id;
        Position = position;
        Rotation = rotation;
        Size = size;
        UpdateTransformMatrix();
        CalculateAABB(out UVector3 min, out UVector3 max);
        AABBMin = min;
        AABBMax = max;
    }

    // Call this whenever Position, Rotation, or Size changes
    public void UpdateTransformMatrix()
    {
        // Local to World matrix (TRS)
        _localToWorldMatrix = UMatrix4x4.TRS(Position, Rotation, Size);
        // World to Local matrix (inverse of TRS)
        _worldToLocalMatrix = _localToWorldMatrix.inverse;
    }

    public void CalculateAABB(out UVector3 min, out UVector3 max)
    {
        // Calculate world-space AABB from OBB
        UVector3[] corners = new UVector3[8];
        // The half-extents of the *unit* cube, then scaled by Size (which is the object's scale)
        // and rotated, then translated.
        // Unity's primitive cube is 1x1x1. Its vertices are at +/-0.5 in local space.
        // So, we use 0.5f * Size for local half-extents.
        UVector3 halfExtentsLocal = Size / 2f; // Size is already the full scale, so half of it is half-extents

        // Get corners in local space (relative to the box's local origin)
        corners[0] = new UVector3(-halfExtentsLocal.x, -halfExtentsLocal.y, -halfExtentsLocal.z);
        corners[1] = new UVector3(halfExtentsLocal.x, -halfExtentsLocal.y, -halfExtentsLocal.z);
        corners[2] = new UVector3(-halfExtentsLocal.x, halfExtentsLocal.y, -halfExtentsLocal.z);
        corners[3] = new UVector3(halfExtentsLocal.x, halfExtentsLocal.y, -halfExtentsLocal.z);
        corners[4] = new UVector3(-halfExtentsLocal.x, -halfExtentsLocal.y, halfExtentsLocal.z);
        corners[5] = new UVector3(halfExtentsLocal.x, -halfExtentsLocal.y, halfExtentsLocal.z);
        corners[6] = new UVector3(-halfExtentsLocal.x, halfExtentsLocal.y, halfExtentsLocal.z);
        corners[7] = new UVector3(halfExtentsLocal.x, halfExtentsLocal.y, halfExtentsLocal.z);

        // Transform corners to world space using the _localToWorldMatrix
        for (int i = 0; i < 8; i++)
        {
            corners[i] = _localToWorldMatrix.MultiplyPoint3x4(corners[i]); // Use MultiplyPoint3x4 for points
        }

        // Find min/max of transformed corners
        min = corners[0];
        max = corners[0];
        for (int i = 1; i < 8; i++)
        {
            min.x = UMathf.Min(min.x, corners[i].x);
            min.y = UMathf.Min(min.y, corners[i].y);
            min.z = UMathf.Min(min.z, corners[i].z);

            max.x = UMathf.Max(max.x, corners[i].x);
            max.y = UMathf.Max(max.y, corners[i].y);
            max.z = UMathf.Max(max.z, corners[i].z);
        }
    }

    public bool IntersectRay(CRay ray, out CHitInfo hitInfo, float maxDistance)
    {
        hitInfo = new CHitInfo();

        // Transform ray to local space of the OBB
        // Use MultiplyPoint3x4 for origin (handles translation)
        UVector3 localRayOrigin = _worldToLocalMatrix.MultiplyPoint3x4(ray.Origin);
        // Use MultiplyVector for direction (handles rotation and scale, ignores translation)
        UVector3 localRayDirection = _worldToLocalMatrix.MultiplyVector(ray.Direction);
        localRayDirection = localRayDirection.normalized; // Re-normalize after transformation

        CRay localRay = new CRay(localRayOrigin, localRayDirection, ray.MaxDistance);

        // Local AABB is centered at origin, with half-extents derived from Size
        UVector3 localMinBounds = -Size / 2f; // Size is the full scale, so half of it is half-extents
        UVector3 localMaxBounds = Size / 2f;

        float tMin = 0.0f;
        float tMax = maxDistance;

        for (int i = 0; i < 3; i++)
        {
            float invDir = 1.0f / localRay.Direction[i];
            float t0 = (localMinBounds[i] - localRay.Origin[i]) * invDir;
            float t1 = (localMaxBounds[i] - localRay.Origin[i]) * invDir;

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

        if (tMin > maxDistance || tMin < UMathf.Epsilon) return false;

        hitInfo.PrimitiveID = ID;
        hitInfo.PrimitiveReference = this;
        hitInfo.Distance = tMin;
        hitInfo.HitPoint = ray.GetPoint(tMin); // Calculate world-space hit point using original ray

        UVector3 localHitPoint = localRay.GetPoint(tMin);
        UVector3 localNormal = UVector3.zero;

        float epsilon = 0.0001f; // Small epsilon for floating point comparisons
        // Determine which face was hit in local space
        if (UMathf.Abs(localHitPoint.x - localMinBounds.x) < epsilon) localNormal = new UVector3(-1, 0, 0);
        else if (UMathf.Abs(localHitPoint.x - localMaxBounds.x) < epsilon) localNormal = new UVector3(1, 0, 0);
        else if (UMathf.Abs(localHitPoint.y - localMinBounds.y) < epsilon) localNormal = new UVector3(0, -1, 0);
        else if (UMathf.Abs(localHitPoint.y - localMaxBounds.y) < epsilon) localNormal = new UVector3(0, 1, 0);
        else if (UMathf.Abs(localHitPoint.z - localMinBounds.z) < epsilon) localNormal = new UVector3(0, 0, -1);
        else if (UMathf.Abs(localHitPoint.z - localMaxBounds.z) < epsilon) localNormal = new UVector3(0, 0, 1);

        // Transform normal to world space using the inverse transpose of the local-to-world matrix
        // This correctly handles non-uniform scaling for normals.
        UMatrix4x4 normalTransformMatrix = _localToWorldMatrix.inverse.transpose;
        hitInfo.Normal = (normalTransformMatrix.MultiplyVector(localNormal)).normalized;

        return true;
    }
}
