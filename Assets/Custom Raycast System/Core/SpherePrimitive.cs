#if UNITY_5_3_OR_NEWER
using UnityEditor.PackageManager;
using UMathf = UnityEngine.Mathf;
using UQuaternion = UnityEngine.Quaternion;
using UVector3 = UnityEngine.Vector3;
#else
    // Assuming CustomMath.cs is in the same namespace or accessible
#endif

// Implementation of a Sphere primitive.
public class SpherePrimitive : IPrimitive
{
    public int ID { get; set; }
    public UVector3 Position { get; set; }
    public UQuaternion Rotation { get; set; } // Not used for sphere, but part of IPrimitive
    public float Radius { get; set; }
    public UVector3 Size // IPrimitive requirement: (Radius,Radius,Radius)
    {
        get => new UVector3(Radius, Radius, Radius);
        set => Radius = value.x; // Only x component is used for radius
    }

    public UVector3 AABBMin { get; private set; }
    public UVector3 AABBMax { get; private set; }

    public SpherePrimitive(int id, UVector3 position, float radius)
    {
        ID = id;
        Position = position;
        Radius = radius;
        Rotation = UQuaternion.identity; // Spheres don't rotate
        CalculateAABB(out UVector3 min, out UVector3 max);
        AABBMin = min;
        AABBMax = max;
    }

    public void CalculateAABB(out UVector3 min, out UVector3 max)
    {
        min = Position - new UVector3(Radius, Radius, Radius);
        max = Position + new UVector3(Radius, Radius, Radius);
    }

    public bool IntersectRay(CRay ray, out CHitInfo hitInfo, float maxDistance)
    {
        hitInfo = new CHitInfo();
        UVector3 L = Position - ray.Origin;
        float tca = UVector3.Dot(L, ray.Direction);

        // If tca < 0, sphere center is behind the ray origin.
        // If ray starts inside sphere, tca can be positive.
        // We ignore rays starting inside for simplicity.
        if (tca < 0) return false;

        float d2 = UVector3.Dot(L, L) - tca * tca;
        float radius2 = Radius * Radius;

        if (d2 > radius2) return false; // Ray misses sphere

        float thc = UMathf.Sqrt(radius2 - d2);
        float t = tca - thc; // First intersection point

        if (t > maxDistance || t < UMathf.Epsilon) // Check if hit is beyond maxDistance or too close (ignore inside hits)
        {
            t = tca + thc; // Second intersection point
            if (t > maxDistance || t < UMathf.Epsilon) return false;
        }

        hitInfo.PrimitiveID = ID;
        hitInfo.PrimitiveReference = this;
        hitInfo.Distance = t;
        hitInfo.HitPoint = ray.GetPoint(t);
        hitInfo.Normal = (hitInfo.HitPoint - Position).normalized; // Normal points from center to hit point

        return true;
    }
}
