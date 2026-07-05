using System.Collections.Generic;

public interface IAccelerationStructure
{
    void AddPrimitive(IPrimitive primitive);
    void RemovePrimitive(IPrimitive primitive);
    void UpdatePrimitive(IPrimitive primitive);

    // Allocating form: returns a superset of candidates within maxDistance.
    List<IPrimitive> QueryRay(CRay ray, float maxDistance);

    // Allocation-free form: appends the same superset into a caller-owned buffer (caller clears).
    void QueryRay(CRay ray, float maxDistance, List<IPrimitive> results);

    // Closest-hit inside the structure (best-t pruning); must equal gather-then-min. Allocation-free.
    bool RaycastClosest(CRay ray, float maxDistance, out CHitInfo hitInfo);
}
