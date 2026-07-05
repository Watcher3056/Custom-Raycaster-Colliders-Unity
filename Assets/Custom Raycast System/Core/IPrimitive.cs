#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
#endif

public interface IPrimitive
{
    public int ID { get; set; }
    UVector3 Position { get; set; }
    UQuaternion Rotation { get; set; }
    UVector3 Size { get; set; } // For Box: extents, For Sphere: (Radius, Radius, Radius)
    UVector3 AABBMin { get; } // World-space AABB min point
    UVector3 AABBMax { get; } // World-space AABB max point

    // hitInfo will contain details of the closest hit.
    bool IntersectRay(CRay ray, out CHitInfo hitInfo, float maxDistance);

    void CalculateAABB(out UVector3 min, out UVector3 max);
}