using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
// No specific Unity types needed here
#else
    // Assuming CustomMath.cs and CRay.cs are in the same namespace or accessible
#endif

// Interface for spatial partitioning structures.
public interface IAccelerationStructure
{
    void AddPrimitive(IPrimitive primitive);
    void RemovePrimitive(IPrimitive primitive);
    void UpdatePrimitive(IPrimitive primitive); // Called when primitive's properties change
    List<IPrimitive> QueryRay(CRay ray, float maxDistance);
}
