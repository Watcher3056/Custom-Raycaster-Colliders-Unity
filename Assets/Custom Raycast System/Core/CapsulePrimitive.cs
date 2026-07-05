#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMatrix4x4 = UnityEngine.Matrix4x4;
using UMathf = UnityEngine.Mathf;
#endif

// Oriented capsule (cylinder + hemispherical caps), local axis +Y.
// Size = (radius, totalHeight, radius); totalHeight is clamped to >= 2*radius.
public class CapsulePrimitive : IPrimitive, ITransformedPrimitive
{
    public int ID { get; set; }
    public UVector3 Position { get; set; }
    public UQuaternion Rotation { get; set; }

    // IPrimitive.Size = (radius, totalHeight, radius).
    public UVector3 Size { get; set; }

    public float Radius => Size.x;
    // Full height including the hemispherical caps.
    public float Height => Size.y;

    public UVector3 AABBMin { get; private set; }
    public UVector3 AABBMax { get; private set; }

    private UMatrix4x4 _localToWorldMatrix;
    private UMatrix4x4 _worldToLocalMatrix;

    public CapsulePrimitive(int id, UVector3 position, UQuaternion rotation, UVector3 size)
    {
        ID = id;
        Position = position;
        Rotation = rotation;
        Size = size;
        UpdateTransformMatrix();
        CalculateAABB(out UVector3 min, out UVector3 max);
    }

    public void UpdateTransformMatrix()
    {
        // Rotation + translation only; the capsule's extents come from Size directly.
        _localToWorldMatrix = UMatrix4x4.TRS(Position, Rotation, UVector3.one);
        _worldToLocalMatrix = _localToWorldMatrix.inverse;
    }

    // Half-length of the cylindrical part (segment half-extent along local +Y).
    private float CylinderHalf()
    {
        float r = Radius;
        float half = Size.y * 0.5f - r;
        return half > 0f ? half : 0f;
    }

    // BUG FIX: stores the recomputed bounds (see SpherePrimitive).
    public void CalculateAABB(out UVector3 min, out UVector3 max)
    {
        float r = Radius;
        float hh = CylinderHalf();
        // Local segment endpoints, transformed to world (rotation+translation only).
        UVector3 a = _localToWorldMatrix.MultiplyPoint3x4(new UVector3(0, -hh, 0));
        UVector3 b = _localToWorldMatrix.MultiplyPoint3x4(new UVector3(0, hh, 0));

        UVector3 lo = new UVector3(UMathf.Min(a.x, b.x), UMathf.Min(a.y, b.y), UMathf.Min(a.z, b.z));
        UVector3 hi = new UVector3(UMathf.Max(a.x, b.x), UMathf.Max(a.y, b.y), UMathf.Max(a.z, b.z));

        UVector3 rad = new UVector3(r, r, r);
        min = lo - rad;
        max = hi + rad;
        AABBMin = min;
        AABBMax = max;
    }

    public bool IntersectRay(CRay ray, out CHitInfo hitInfo, float maxDistance)
    {
        RaycastDiagnostics.CountIntersection();
        hitInfo = new CHitInfo();

        // Transform the ray into local space (orthonormal transform: direction stays unit).
        UVector3 o = _worldToLocalMatrix.MultiplyPoint3x4(ray.Origin);
        UVector3 d = _worldToLocalMatrix.MultiplyVector(ray.Direction);

        float r = Radius;
        float hh = CylinderHalf();

        float bestT = UMathf.Infinity;
        UVector3 bestLocalNormal = UVector3.zero;
        bool found = false;

        // --- Cylindrical body: infinite cylinder about local Y, then clamp to [-hh, +hh] ---
        // (ox + t dx)^2 + (oz + t dz)^2 = r^2
        float a2 = d.x * d.x + d.z * d.z;
        float b2 = 2f * (o.x * d.x + o.z * d.z);
        float c2 = o.x * o.x + o.z * o.z - r * r;

        if (a2 > 1e-12f)
        {
            float disc = b2 * b2 - 4f * a2 * c2;
            if (disc >= 0f)
            {
                float sq = UMathf.Sqrt(disc);
                float inv2a = 0.5f / a2;
                // near then far root
                float t0 = (-b2 - sq) * inv2a;
                float t1 = (-b2 + sq) * inv2a;
                ConsiderCylinder(t0, o, d, hh, r, ref bestT, ref bestLocalNormal, ref found, maxDistance);
                if (!found || t1 < bestT)
                    ConsiderCylinder(t1, o, d, hh, r, ref bestT, ref bestLocalNormal, ref found, maxDistance);
            }
        }

        // --- Hemispherical caps: spheres of radius r at (0, +/-hh, 0) ---
        ConsiderCap(new UVector3(0, hh, 0), +1f, o, d, hh, r, ref bestT, ref bestLocalNormal, ref found, maxDistance);
        ConsiderCap(new UVector3(0, -hh, 0), -1f, o, d, hh, r, ref bestT, ref bestLocalNormal, ref found, maxDistance);

        if (!found) return false;

        hitInfo.PrimitiveID = ID;
        hitInfo.PrimitiveReference = this;
        hitInfo.Distance = bestT;
        hitInfo.HitPoint = ray.GetPoint(bestT);
        hitInfo.Normal = (_localToWorldMatrix.MultiplyVector(bestLocalNormal)).normalized;
        return true;
    }

    private static void ConsiderCylinder(float t, UVector3 o, UVector3 d, float hh, float r,
        ref float bestT, ref UVector3 bestNormal, ref bool found, float maxDistance)
    {
        if (t < UMathf.Epsilon || t > maxDistance) return;
        if (found && t >= bestT) return;
        float y = o.y + t * d.y;
        if (y < -hh || y > hh) return; // outside the cylindrical body -> caps handle it
        float hx = o.x + t * d.x;
        float hz = o.z + t * d.z;
        UVector3 n = new UVector3(hx, 0f, hz);
        float m = n.magnitude;
        bestNormal = m > 1e-8f ? n / m : new UVector3(1, 0, 0);
        bestT = t;
        found = true;
    }

    private static void ConsiderCap(UVector3 center, float side, UVector3 o, UVector3 d, float hh, float r,
        ref float bestT, ref UVector3 bestNormal, ref bool found, float maxDistance)
    {
        // Ray-sphere about 'center'; accept only the correct hemisphere (sign of y - center.y).
        UVector3 oc = o - center;
        float b = 2f * UVector3.Dot(oc, d);
        float c = UVector3.Dot(oc, oc) - r * r;
        float disc = b * b - 4f * c; // a = |d|^2 = 1 (unit local direction)
        if (disc < 0f) return;
        float sq = UMathf.Sqrt(disc);
        float t0 = (-b - sq) * 0.5f;
        float t1 = (-b + sq) * 0.5f;
        ConsiderCapRoot(t0, center, side, o, d, hh, r, ref bestT, ref bestNormal, ref found, maxDistance);
        ConsiderCapRoot(t1, center, side, o, d, hh, r, ref bestT, ref bestNormal, ref found, maxDistance);
    }

    private static void ConsiderCapRoot(float t, UVector3 center, float side, UVector3 o, UVector3 d, float hh, float r,
        ref float bestT, ref UVector3 bestNormal, ref bool found, float maxDistance)
    {
        if (t < UMathf.Epsilon || t > maxDistance) return;
        if (found && t >= bestT) return;
        float y = o.y + t * d.y;
        // Top cap (side +1) owns points with y >= hh; bottom cap (side -1) owns y <= -hh.
        if (side > 0f) { if (y < hh) return; }
        else { if (y > -hh) return; }
        UVector3 hit = new UVector3(o.x + t * d.x, y, o.z + t * d.z);
        UVector3 n = hit - center;
        float m = n.magnitude;
        bestNormal = m > 1e-8f ? n / m : new UVector3(0, side, 0);
        bestT = t;
        found = true;
    }
}
