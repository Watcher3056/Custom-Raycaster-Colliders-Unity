using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UMathf = UnityEngine.Mathf;
#endif

public class SimpleListAccelerationStructure : IAccelerationStructure
{
    private List<IPrimitive> _primitives = new List<IPrimitive>();

    public void AddPrimitive(IPrimitive primitive)
    {
        _primitives.Add(primitive);
    }

    public void RemovePrimitive(IPrimitive primitive)
    {
        _primitives.Remove(primitive);
    }

    public void UpdatePrimitive(IPrimitive primitive)
    {
        // Only the cached world AABB needs refreshing (CalculateAABB stores it).
        primitive.CalculateAABB(out UVector3 min, out UVector3 max);
    }

    // Original allocating form (public-API compatibility). Delegates to the buffer form.
    public List<IPrimitive> QueryRay(CRay ray, float maxDistance)
    {
        List<IPrimitive> potentialHits = new List<IPrimitive>();
        QueryRay(ray, maxDistance, potentialHits);
        return potentialHits;
    }

    // Zero-alloc form: appends candidates into a caller-owned buffer.
    public void QueryRay(CRay ray, float maxDistance, List<IPrimitive> results)
    {
        for (int i = 0; i < _primitives.Count; i++)
        {
            IPrimitive primitive = _primitives[i];
            if (RayAabb.Intersects(ray, primitive.AABBMin, primitive.AABBMax, maxDistance))
            {
                results.Add(primitive);
            }
        }
    }

    // Early-out scan: the best hit shrinks the AABB window; skipped candidates cannot beat it (t_hit >= t_entry).
    public bool RaycastClosest(CRay ray, float maxDistance, out CHitInfo hitInfo)
    {
        hitInfo = new CHitInfo();
        // Infinity (not maxDistance) so a hit at exactly maxDistance still counts.
        float bestT = UMathf.Infinity;
        bool found = false;

        for (int i = 0; i < _primitives.Count; i++)
        {
            IPrimitive primitive = _primitives[i];
            float cullT = bestT < maxDistance ? bestT : maxDistance;
            if (!RayAabb.Intersects(ray, primitive.AABBMin, primitive.AABBMax, cullT))
                continue;
            if (primitive.IntersectRay(ray, out CHitInfo currentHit, maxDistance) &&
                currentHit.Distance < bestT)
            {
                bestT = currentHit.Distance;
                hitInfo = currentHit;
                found = true;
            }
        }
        return found;
    }
}
