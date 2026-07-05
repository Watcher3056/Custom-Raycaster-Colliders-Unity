#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMatrix4x4 = UnityEngine.Matrix4x4;
using UMathf = UnityEngine.Mathf;
#endif

public class BoxPrimitive : IPrimitive, ITransformedPrimitive
{
    public int ID { get; set; }
    public UVector3 Position { get; set; }
    public UQuaternion Rotation { get; set; }
    public UVector3 Size { get; set; } // This is the scale of the object

    public UVector3 AABBMin { get; private set; }
    public UVector3 AABBMax { get; private set; }

    private UMatrix4x4 _worldToLocalMatrix;
    private UMatrix4x4 _localToWorldMatrix; // Store this for normal transformation
    // Inverse-transpose cached per transform change (not per hit).
    private UMatrix4x4 _normalMatrix;

    public BoxPrimitive(int id, UVector3 position, UQuaternion rotation, UVector3 size)
    {
        ID = id;
        Position = position;
        Rotation = rotation;
        Size = size;
        UpdateTransformMatrix();
        CalculateAABB(out UVector3 min, out UVector3 max);
    }

    // Call this whenever Position, Rotation, or Size changes
    public void UpdateTransformMatrix()
    {
        // Scale-free TRS: Size acts as local half-extents (+/- Size/2), so local t equals world distance.
        _localToWorldMatrix = UMatrix4x4.TRS(Position, Rotation, UVector3.one);
        _worldToLocalMatrix = _localToWorldMatrix.inverse;
        _normalMatrix = _worldToLocalMatrix.transpose; // normal transform, correct under non-uniform scale
    }

    // BUG FIX: stores the bounds; world AABB via |R|*halfExtents (exact and allocation-free).
    public void CalculateAABB(out UVector3 min, out UVector3 max)
    {
        UVector3 h = Size / 2f; // local half-extents (Size = full world dimensions)

        float ex = UMathf.Abs(_localToWorldMatrix.m00) * h.x + UMathf.Abs(_localToWorldMatrix.m01) * h.y + UMathf.Abs(_localToWorldMatrix.m02) * h.z;
        float ey = UMathf.Abs(_localToWorldMatrix.m10) * h.x + UMathf.Abs(_localToWorldMatrix.m11) * h.y + UMathf.Abs(_localToWorldMatrix.m12) * h.z;
        float ez = UMathf.Abs(_localToWorldMatrix.m20) * h.x + UMathf.Abs(_localToWorldMatrix.m21) * h.y + UMathf.Abs(_localToWorldMatrix.m22) * h.z;

        min = new UVector3(Position.x - ex, Position.y - ey, Position.z - ez);
        max = new UVector3(Position.x + ex, Position.y + ey, Position.z + ez);
        AABBMin = min;
        AABBMax = max;
    }

    public bool IntersectRay(CRay ray, out CHitInfo hitInfo, float maxDistance)
    {
        RaycastDiagnostics.CountIntersection();
        hitInfo = new CHitInfo();

        UVector3 localRayOrigin = _worldToLocalMatrix.MultiplyPoint3x4(ray.Origin);
        // Direction stays UN-normalized so the slab t is a world-space distance.
        UVector3 localRayDirection = _worldToLocalMatrix.MultiplyVector(ray.Direction);

        UVector3 localMinBounds = -Size / 2f; // local AABB: origin-centered, half-extents = Size/2
        UVector3 localMaxBounds = Size / 2f;

        float tMin = 0.0f;
        float tMax = UMathf.Infinity;

        for (int i = 0; i < 3; i++)
        {
            float dir = localRayDirection[i];
            if (UMathf.Abs(dir) < 1e-12f)
            {
                // Ray parallel to this slab: miss if origin outside the slab.
                if (localRayOrigin[i] < localMinBounds[i] || localRayOrigin[i] > localMaxBounds[i])
                    return false;
                continue;
            }
            float invDir = 1.0f / dir;
            float t0 = (localMinBounds[i] - localRayOrigin[i]) * invDir;
            float t1 = (localMaxBounds[i] - localRayOrigin[i]) * invDir;

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

        // Reject beyond maxDistance or at/behind origin (ray starting inside = miss; historical semantics).
        if (tMin > maxDistance || tMin < UMathf.Epsilon) return false;

        hitInfo.PrimitiveID = ID;
        hitInfo.PrimitiveReference = this;
        hitInfo.Distance = tMin;
        hitInfo.HitPoint = ray.GetPoint(tMin); // World-space hit point along the original world ray

        // Local hit point for face/normal classification (use the same un-normalized local ray).
        UVector3 localHitPoint = localRayOrigin + localRayDirection * tMin;
        UVector3 localNormal = UVector3.zero;

        float epsilon = 0.0001f;
        if (UMathf.Abs(localHitPoint.x - localMinBounds.x) < epsilon) localNormal = new UVector3(-1, 0, 0);
        else if (UMathf.Abs(localHitPoint.x - localMaxBounds.x) < epsilon) localNormal = new UVector3(1, 0, 0);
        else if (UMathf.Abs(localHitPoint.y - localMinBounds.y) < epsilon) localNormal = new UVector3(0, -1, 0);
        else if (UMathf.Abs(localHitPoint.y - localMaxBounds.y) < epsilon) localNormal = new UVector3(0, 1, 0);
        else if (UMathf.Abs(localHitPoint.z - localMinBounds.z) < epsilon) localNormal = new UVector3(0, 0, -1);
        else if (UMathf.Abs(localHitPoint.z - localMaxBounds.z) < epsilon) localNormal = new UVector3(0, 0, 1);

        // Cached inverse-transpose handles non-uniform scale correctly.
        hitInfo.Normal = (_normalMatrix.MultiplyVector(localNormal)).normalized;

        return true;
    }
}
