#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UMathf = UnityEngine.Mathf;
#else
    // Assuming CustomMath.cs is in the same namespace or accessible
#endif

// Defines the custom Ray struct.
public struct CRay
{
    public UVector3 Origin;
    public UVector3 Direction; // Normalized
    public float MaxDistance;

    public CRay(UVector3 origin, UVector3 direction, float maxDistance = UMathf.Infinity)
    {
        Origin = origin;
        Direction = direction.normalized; // Ensure direction is normalized
        MaxDistance = maxDistance;
    }

    public UVector3 GetPoint(float distance)
    {
        return Origin + Direction * distance;
    }
}
