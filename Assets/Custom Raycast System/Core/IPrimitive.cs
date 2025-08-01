#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
#else
    // Assuming CustomMath.cs is in the same namespace or accessible
#endif

// Interface for all custom collider primitives.
public interface IPrimitive
{
    public int ID { get; set; }
    UVector3 Position { get; set; }
    UQuaternion Rotation { get; set; }
    UVector3 Size { get; set; } // For Box: extents, For Sphere: (Radius, Radius, Radius)
    UVector3 AABBMin { get; } // World-space AABB min point
    UVector3 AABBMax { get; } // World-space AABB max point

    // Method to check for ray intersection. Returns true if hit, false otherwise.
    // hitInfo will contain details of the closest hit.
    bool IntersectRay(CRay ray, out CHitInfo hitInfo, float maxDistance);

    // Calculates the world-space AABB for the primitive.
    void CalculateAABB(out UVector3 min, out UVector3 max);
}