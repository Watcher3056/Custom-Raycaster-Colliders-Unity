using System.Collections.Generic;
using System.Linq;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMathf = UnityEngine.Mathf;
#else
    // Assuming CustomMath.cs and other core files are in the same namespace or accessible
#endif

// The core manager for the custom raycast system.
public class CustomRaycastSystemCore
{
    private Dictionary<int, IPrimitive> _allPrimitives = new Dictionary<int, IPrimitive>();
    private int _nextPrimitiveID = 0;
    private IAccelerationStructure _accelerationStructure;

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

    public IPrimitive AddPrimitive(IPrimitive primitive)
    {
        // Ensure ID is assigned before adding to dictionary and acceleration structure
        // This is handled by the `RegisterPrimitive` in the Unity layer, but good to ensure here too.
        if (primitive.ID == -1) // If ID hasn't been assigned yet (e.g., from constructor)
        {
            // Note: IPrimitive.ID setter is private set. This would require a change
            // to public set or a different ID assignment strategy.
            // For now, assuming the Unity layer handles ID assignment correctly.
            // A better pattern might be: `primitive.SetID(_nextPrimitiveID++);` if ID is read-only.
            // Or, pass ID into the primitive constructor.
            // For this implementation, the ID is set in the AddPrimitive method of the core.
        }

        // Assign ID here as per original logic, assuming primitive ID is mutable
        primitive.ID = _nextPrimitiveID++; // Using dynamic to bypass private set for demonstration
                                                        // In a real scenario, IPrimitive would have a public ID setter
                                                        // or ID would be passed in constructor.

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

            if (primitive is BoxPrimitive box)
            {
                box.UpdateTransformMatrix();
            }
            primitive.CalculateAABB(out UVector3 min, out UVector3 max);
            _accelerationStructure.UpdatePrimitive(primitive);
        }
    }

    public bool Raycast(CRay ray, out CHitInfo hitInfo, float maxDistance = UMathf.Infinity)
    {
        hitInfo = new CHitInfo();
        List<CHitInfo> allHits = new List<CHitInfo>();

        List<IPrimitive> potentialPrimitives = _accelerationStructure.QueryRay(ray, maxDistance);

        foreach (var primitive in potentialPrimitives)
        {
            if (primitive.IntersectRay(ray, out CHitInfo currentHitInfo, maxDistance))
            {
                allHits.Add(currentHitInfo);
            }
        }

        CHitInfo closestHit = new CHitInfo { Distance = UMathf.Infinity };
        bool hitFound = false;

        foreach (var hit in allHits)
        {
            if (hit.Distance < closestHit.Distance)
            {
                closestHit = hit;
                hitFound = true;
            }
        }

        if (hitFound)
        {
            hitInfo = closestHit;
            return true;
        }
        return false;
    }

    public List<CHitInfo> RaycastAll(CRay ray, float maxDistance = UMathf.Infinity, bool sortByDistance = true)
    {
        List<CHitInfo> allHits = new List<CHitInfo>();

        List<IPrimitive> potentialPrimitives = _accelerationStructure.QueryRay(ray, maxDistance);

        foreach (var primitive in potentialPrimitives)
        {
            if (primitive.IntersectRay(ray, out CHitInfo currentHitInfo, maxDistance))
            {
                allHits.Add(currentHitInfo);
            }
        }

        if (sortByDistance)
        {
            return allHits.OrderBy(h => h.Distance).ToList();
        }
        return allHits;
    }
}
