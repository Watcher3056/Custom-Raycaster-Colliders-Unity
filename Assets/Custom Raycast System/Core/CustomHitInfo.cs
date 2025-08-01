#if UNITY_5_3_OR_NEWER
using UnityEngine;

// CustomHitInfo for Unity, wrapping CHitInfo and adding GameObject reference
public struct CustomHitInfo
{
    public CHitInfo CoreHitInfo;
    public GameObject HitGameObject;

    public int PrimitiveID => CoreHitInfo.PrimitiveID;
    public IPrimitive PrimitiveReference => CoreHitInfo.PrimitiveReference;
    public Vector3 HitPoint => CoreHitInfo.HitPoint;
    public float Distance => CoreHitInfo.Distance;
    public Vector3 Normal => CoreHitInfo.Normal;

    public CustomHitInfo(CHitInfo coreHitInfo, GameObject hitGameObject)
    {
        CoreHitInfo = coreHitInfo;
        HitGameObject = hitGameObject;
    }
}
#endif
