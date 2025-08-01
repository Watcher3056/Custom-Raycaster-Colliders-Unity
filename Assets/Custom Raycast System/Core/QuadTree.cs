using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UMathf = UnityEngine.Mathf;
#else
    // Assuming CustomMath.cs and CRay.cs are in the same namespace or accessible
#endif

public class QuadTree : IAccelerationStructure
{
    private QuadTreeNode _root;
    private int _nodeCapacity;
    private Dictionary<int, IPrimitive> _primitiveMap = new Dictionary<int, IPrimitive>();

    public QuadTree(UVector3 center, UVector3 size, int nodeCapacity = 4)
    {
        _root = new QuadTreeNode(center, size, nodeCapacity, 0);
        _nodeCapacity = nodeCapacity;
    }

    public void AddPrimitive(IPrimitive primitive)
    {
        _primitiveMap[primitive.ID] = primitive;
        _root.Insert(primitive);
    }

    public void RemovePrimitive(IPrimitive primitive)
    {
        if (_primitiveMap.ContainsKey(primitive.ID))
        {
            // Call the updated Remove method on the root, which will traverse and remove
            _root.Remove(primitive);
            // Fix: Explicitly use the Dictionary.Remove overload that takes an out parameter, discarding the value.
            _primitiveMap.Remove(primitive.ID, out var _);
        }
    }

    public void UpdatePrimitive(IPrimitive primitive)
    {
        _root.Remove(primitive); // Remove from old location
        primitive.CalculateAABB(out UVector3 min, out UVector3 max); // Re-calculate AABB for new position/size
        _root.Insert(primitive); // Re-insert into new location
    }

    public List<IPrimitive> QueryRay(CRay ray, float maxDistance)
    {
        List<IPrimitive> potentialHits = new List<IPrimitive>();
        _root.QueryRay(ray, maxDistance, potentialHits);
        return potentialHits;
    }

    private class QuadTreeNode
    {
        public UVector3 Center;
        public UVector3 HalfSize;
        public int Capacity;
        public int Depth;

        private List<IPrimitive> _primitives = new List<IPrimitive>();
        private QuadTreeNode[] _children;
        private bool _isLeaf;

        public QuadTreeNode(UVector3 center, UVector3 size, int capacity, int depth)
        {
            Center = center;
            HalfSize = size / 2f;
            Capacity = capacity;
            Depth = depth;
            _isLeaf = true;
        }

        private bool Contains(IPrimitive primitive)
        {
            // Check if the primitive's AABB is fully contained within this node's bounds
            return primitive.AABBMin.x >= (Center.x - HalfSize.x) && primitive.AABBMax.x <= (Center.x + HalfSize.x) &&
                   primitive.AABBMin.y >= (Center.y - HalfSize.y) && primitive.AABBMax.y <= (Center.y + HalfSize.y) &&
                   primitive.AABBMin.z >= (Center.z - HalfSize.z) && primitive.AABBMax.z <= (Center.z + HalfSize.z);
        }

        private bool Intersects(IPrimitive primitive)
        {
            // Check if the primitive's AABB intersects this node's bounds
            UVector3 nodeMin = Center - HalfSize;
            UVector3 nodeMax = Center + HalfSize;

            return !(primitive.AABBMax.x < nodeMin.x || primitive.AABBMin.x > nodeMax.x ||
                     primitive.AABBMax.y < nodeMin.y || primitive.AABBMin.y > nodeMax.y ||
                     primitive.AABBMax.z < nodeMin.z || primitive.AABBMin.z > nodeMax.z);
        }

        public void Insert(IPrimitive primitive)
        {
            if (_isLeaf)
            {
                if (_primitives.Count < Capacity)
                {
                    _primitives.Add(primitive);
                }
                else
                {
                    // Node is full and needs to subdivide
                    Subdivide(); // This creates children and sets _isLeaf = false

                    // Collect all primitives (current + the new one) that need redistribution
                    // Make a copy to avoid "Collection modified" exception during iteration
                    List<IPrimitive> allPrimitivesToRedistribute = new List<IPrimitive>(_primitives);
                    allPrimitivesToRedistribute.Add(primitive);

                    _primitives.Clear(); // Clear the current node's primitives, as they will be re-inserted

                    // Now, redistribute them into children or keep them in this node if they don't fit
                    foreach (var p in allPrimitivesToRedistribute)
                    {
                        bool insertedIntoChild = false;
                        foreach (var child in _children)
                        {
                            if (child.Contains(p))
                            {
                                child.Insert(p); // Recursively insert into child
                                insertedIntoChild = true;
                                break;
                            }
                        }
                        if (!insertedIntoChild)
                        {
                            // If a primitive cannot be fully contained by any child, it remains in this node.
                            _primitives.Add(p);
                        }
                    }
                }
            }
            else // Not a leaf (already subdivided)
            {
                // Try to insert into children
                bool insertedIntoChild = false;
                foreach (var child in _children)
                {
                    if (child.Contains(primitive))
                    {
                        child.Insert(primitive);
                        insertedIntoChild = true;
                        break;
                    }
                }
                if (!insertedIntoChild)
                {
                    // If the primitive cannot be fully contained by any child, it stays in this node.
                    _primitives.Add(primitive);
                }
            }
        }

        private void Subdivide()
        {
            _isLeaf = false;
            _children = new QuadTreeNode[4];

            UVector3 quarterSize = HalfSize / 2f;

            // Child nodes covering the quadrants around the current node's center
            // For a 2D QuadTree, we assume Z-axis is 'depth' so we vary X and Z
            _children[0] = new QuadTreeNode(Center + new UVector3(-quarterSize.x, 0, -quarterSize.z), quarterSize * 2, Capacity, Depth + 1); // Bottom-Left (relative to XZ plane)
            _children[1] = new QuadTreeNode(Center + new UVector3(quarterSize.x, 0, -quarterSize.z), quarterSize * 2, Capacity, Depth + 1);  // Bottom-Right
            _children[2] = new QuadTreeNode(Center + new UVector3(-quarterSize.x, 0, quarterSize.z), quarterSize * 2, Capacity, Depth + 1);  // Top-Left
            _children[3] = new QuadTreeNode(Center + new UVector3(quarterSize.x, 0, quarterSize.z), quarterSize * 2, Capacity, Depth + 1);   // Top-Right
        }

        public bool Remove(IPrimitive primitive) // Changed return type to bool
        {
            if (_isLeaf)
            {
                // Try to remove from this node's list
                return _primitives.Remove(primitive);
            }
            else
            {
                // Recursively try to remove from children first
                foreach (var child in _children)
                {
                    // Only recurse if the primitive *could* be in this child's bounds
                    if (child.Intersects(primitive))
                    {
                        if (child.Remove(primitive)) // If successfully removed from a child
                        {
                            // After removal, consider merging if children become sparse
                            // (Not implemented for simplicity, but a future optimization)
                            return true;
                        }
                    }
                }
                // If not removed from any child, it must be in this node's direct primitives (due to Contains logic)
                return _primitives.Remove(primitive);
            }
        }

        public void QueryRay(CRay ray, float maxDistance, List<IPrimitive> result)
        {
            if (!RayIntersectsAABB(ray, Center - HalfSize, Center + HalfSize, maxDistance))
            {
                return; // Ray does not intersect this node's AABB
            }

            // Add primitives directly held by this node
            foreach (var primitive in _primitives)
            {
                result.Add(primitive);
            }

            // If not a leaf, recurse into children
            if (!_isLeaf)
            {
                foreach (var child in _children)
                {
                    child.QueryRay(ray, maxDistance, result);
                }
            }
        }

        private bool RayIntersectsAABB(CRay ray, UVector3 minBounds, UVector3 maxBounds, float maxDistance)
        {
            float tMin = 0.0f;
            float tMax = maxDistance;

            for (int i = 0; i < 3; i++)
            {
                float invDir = 1.0f / ray.Direction[i];
                float t0 = (minBounds[i] - ray.Origin[i]) * invDir;
                float t1 = (maxBounds[i] - ray.Origin[i]) * invDir;

                if (invDir < 0.0f)
                {
                    float temp = t0;
                    t0 = t1;
                    t1 = temp;
                }

                tMin = UMathf.Max(t0, tMin);
                tMax = UMathf.Min(t1, tMax);

                if (tMax < tMin) return false;
            }
            return tMin < maxDistance && tMax > UMathf.Epsilon;
        }
    }
}
