#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
#endif

public struct CHitInfo
{
    public int PrimitiveID;
    public IPrimitive PrimitiveReference;
    public UVector3 HitPoint;
    public float Distance;
    public UVector3 Normal;
}
