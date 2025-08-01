#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
#else
    // Assuming CustomMath.cs is in the same namespace or accessible
#endif

// Defines the custom HitInfo struct.
public struct CHitInfo
{
    public int PrimitiveID;
    public IPrimitive PrimitiveReference;
    public UVector3 HitPoint;
    public float Distance;
    public UVector3 Normal;
}
