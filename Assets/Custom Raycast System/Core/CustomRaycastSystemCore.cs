using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMathf = UnityEngine.Mathf;
#endif

// Single-threaded, like the original library (RaycastAll reuses a pooled instance buffer).
public class CustomRaycastSystemCore
{
    private Dictionary<int, IPrimitive> _allPrimitives = new Dictionary<int, IPrimitive>();
    private int _nextPrimitiveID = 0;
    private IAccelerationStructure _accelerationStructure;

    // Pooled candidate buffer for RaycastAll; never handed to callers.
    private readonly List<IPrimitive> _candidateBuffer = new List<IPrimitive>(64);

    // Deterministic order (Distance, then PrimitiveID); static instance keeps Sort allocation-free.
    private sealed class HitDistanceComparer : IComparer<CHitInfo>
    {
        public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
        public int Compare(CHitInfo a, CHitInfo b)
        {
            int byDistance = a.Distance.CompareTo(b.Distance);
            if (byDistance != 0) return byDistance;
            return a.PrimitiveID.CompareTo(b.PrimitiveID);
        }
    }

    public CustomRaycastSystemCore(bool useQuadTree, UVector3 quadTreeCenter, UVector3 quadTreeSize, int quadTreeCapacity)
    {
        if (useQuadTree)
        {
            _accelerationStructure = new QuadTree(quadTreeCenter, quadTreeSize, quadTreeCapacity);
        }
        else
        {
            _accelerationStructure = new SimpleListAccelerationStructure();
        }
    }

    // Constructor ids are placeholders: AddPrimitive assigns unique sequential ids (never rejects/throws).
    private void AssignId(IPrimitive primitive)
    {
        primitive.ID = _nextPrimitiveID++;
    }

    public IPrimitive AddPrimitive(IPrimitive primitive)
    {
        AssignId(primitive);
        _allPrimitives[primitive.ID] = primitive;
        _accelerationStructure.AddPrimitive(primitive);
        return primitive;
    }

    public void RemovePrimitive(int primitiveId)
    {
        if (_allPrimitives.TryGetValue(primitiveId, out IPrimitive primitive))
        {
            _accelerationStructure.RemovePrimitive(primitive);
            _allPrimitives.Remove(primitiveId);
        }
    }

    public void UpdatePrimitive(int primitiveId, UVector3 newPosition, UQuaternion newRotation, UVector3 newSize)
    {
        if (_allPrimitives.TryGetValue(primitiveId, out IPrimitive primitive))
        {
            primitive.Position = newPosition;
            primitive.Rotation = newRotation;
            primitive.Size = newSize;

            // Refresh cached matrices (no concrete-type branching).
            (primitive as ITransformedPrimitive)?.UpdateTransformMatrix();

            // The structure re-stores the AABB itself AFTER removing under the old bounds.
            _accelerationStructure.UpdatePrimitive(primitive);
        }
    }

    public bool Raycast(CRay ray, out CHitInfo hitInfo, float maxDistance = UMathf.Infinity)
    {
        // Allocation-free; result identical to the historical gather-all-then-min loop.
        return _accelerationStructure.RaycastClosest(ray, maxDistance, out hitInfo);
    }

    public List<CHitInfo> RaycastAll(CRay ray, float maxDistance = UMathf.Infinity, bool sortByDistance = true)
    {
        // Exactly one allocation: the returned list. Candidates reuse the pooled buffer; sort is in-place.
        List<CHitInfo> allHits = new List<CHitInfo>();

        _candidateBuffer.Clear();
        _accelerationStructure.QueryRay(ray, maxDistance, _candidateBuffer);
        for (int i = 0; i < _candidateBuffer.Count; i++)
        {
            if (_candidateBuffer[i].IntersectRay(ray, out CHitInfo currentHitInfo, maxDistance))
            {
                allHits.Add(currentHitInfo);
            }
        }

        if (sortByDistance)
        {
            allHits.Sort(HitDistanceComparer.Instance);
        }
        return allHits;
    }
}
