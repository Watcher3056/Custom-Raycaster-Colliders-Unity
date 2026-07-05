using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UMathf = UnityEngine.Mathf;
#endif

// XZ quadtree: full-height columns, cached subtree bounds, near-to-far best-t-pruned traversal.
public class QuadTree : IAccelerationStructure
{
    private QuadTreeNode _root;
    private Dictionary<int, IPrimitive> _primitiveMap = new Dictionary<int, IPrimitive>();

    public QuadTree(UVector3 center, UVector3 size, int nodeCapacity = 4)
    {
        _root = new QuadTreeNode(center, size, nodeCapacity, 0);
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
            _root.Remove(primitive);
            _primitiveMap.Remove(primitive.ID); // Remove(key, out) overload is ns2.1-only
        }
    }

    public void UpdatePrimitive(IPrimitive primitive)
    {
        // Remove under the OLD stored AABB first; CalculateAABB then stores the new bounds.
        _root.Remove(primitive);
        primitive.CalculateAABB(out UVector3 min, out UVector3 max);
        _root.Insert(primitive);
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
        _root.QueryRay(ray, maxDistance, results);
    }

    // Closest-hit query with near-to-far traversal and best-t pruning (see node method).
    public bool RaycastClosest(CRay ray, float maxDistance, out CHitInfo hitInfo)
    {
        hitInfo = new CHitInfo();
        float bestT = UMathf.Infinity;
        bool found = false;
        _root.RaycastClosest(ray, maxDistance, ref hitInfo, ref bestT, ref found);
        return found;
    }

    private class QuadTreeNode
    {
        // Past this depth a leaf grows beyond Capacity (subdividing cannot separate co-located primitives).
        private const int MaxDepth = 10;

        // Near-to-far child order per ray XZ signs; affects only how fast bestT shrinks, never the winner.
        private static readonly int[,] ChildOrder =
        {
            { 0, 1, 2, 3 }, // dx >= 0, dz >= 0   (children: 0=-X-Z, 1=+X-Z, 2=-X+Z, 3=+X+Z)
            { 1, 0, 3, 2 }, // dx <  0, dz >= 0
            { 2, 3, 0, 1 }, // dx >= 0, dz <  0
            { 3, 2, 1, 0 }  // dx <  0, dz <  0
        };

        public UVector3 Center;
        public UVector3 HalfSize;
        public int Capacity;
        public int Depth;

        private List<IPrimitive> _primitives = new List<IPrimitive>();
        private QuadTreeNode[] _children;
        private bool _isLeaf;

        // Union AABB of everything in this subtree; one test can skip the whole subtree.
        private bool _hasContent;
        private UVector3 _subMin;
        private UVector3 _subMax;

        public QuadTreeNode(UVector3 center, UVector3 size, int capacity, int depth)
        {
            Center = center;
            HalfSize = size / 2f;
            Capacity = capacity;
            Depth = depth;
            _isLeaf = true;
            _hasContent = false;
        }

        // Grow this node's cached content-bounds to include the given primitive's AABB.
        private void ExpandBounds(IPrimitive p)
        {
            if (!_hasContent)
            {
                _subMin = p.AABBMin;
                _subMax = p.AABBMax;
                _hasContent = true;
            }
            else
            {
                if (p.AABBMin.x < _subMin.x) _subMin.x = p.AABBMin.x;
                if (p.AABBMin.y < _subMin.y) _subMin.y = p.AABBMin.y;
                if (p.AABBMin.z < _subMin.z) _subMin.z = p.AABBMin.z;
                if (p.AABBMax.x > _subMax.x) _subMax.x = p.AABBMax.x;
                if (p.AABBMax.y > _subMax.y) _subMax.y = p.AABBMax.y;
                if (p.AABBMax.z > _subMax.z) _subMax.z = p.AABBMax.z;
            }
        }

        // Recompute content-bounds from scratch: union of held primitives + children subtrees.
        private void RecomputeBounds()
        {
            _hasContent = false;
            for (int i = 0; i < _primitives.Count; i++) ExpandBounds(_primitives[i]);
            if (!_isLeaf && _children != null)
            {
                for (int c = 0; c < _children.Length; c++)
                {
                    var ch = _children[c];
                    if (ch != null && ch._hasContent)
                    {
                        if (!_hasContent) { _subMin = ch._subMin; _subMax = ch._subMax; _hasContent = true; }
                        else
                        {
                            if (ch._subMin.x < _subMin.x) _subMin.x = ch._subMin.x;
                            if (ch._subMin.y < _subMin.y) _subMin.y = ch._subMin.y;
                            if (ch._subMin.z < _subMin.z) _subMin.z = ch._subMin.z;
                            if (ch._subMax.x > _subMax.x) _subMax.x = ch._subMax.x;
                            if (ch._subMax.y > _subMax.y) _subMax.y = ch._subMax.y;
                            if (ch._subMax.z > _subMax.z) _subMax.z = ch._subMax.z;
                        }
                    }
                }
            }
        }

        private bool Contains(IPrimitive primitive)
        {
            return primitive.AABBMin.x >= (Center.x - HalfSize.x) && primitive.AABBMax.x <= (Center.x + HalfSize.x) &&
                   primitive.AABBMin.y >= (Center.y - HalfSize.y) && primitive.AABBMax.y <= (Center.y + HalfSize.y) &&
                   primitive.AABBMin.z >= (Center.z - HalfSize.z) && primitive.AABBMax.z <= (Center.z + HalfSize.z);
        }

        private bool Intersects(IPrimitive primitive)
        {
            UVector3 nodeMin = Center - HalfSize;
            UVector3 nodeMax = Center + HalfSize;

            return !(primitive.AABBMax.x < nodeMin.x || primitive.AABBMin.x > nodeMax.x ||
                     primitive.AABBMax.y < nodeMin.y || primitive.AABBMin.y > nodeMax.y ||
                     primitive.AABBMax.z < nodeMin.z || primitive.AABBMin.z > nodeMax.z);
        }

        public void Insert(IPrimitive primitive)
        {
            // Every node on the insertion path covers this primitive in its content-bounds.
            ExpandBounds(primitive);

            if (_isLeaf)
            {
                if (_primitives.Count < Capacity || Depth >= MaxDepth)
                {
                    _primitives.Add(primitive);
                }
                else
                {
                    Subdivide();

                    // Make a copy to avoid "Collection modified" exception during iteration
                    List<IPrimitive> allPrimitivesToRedistribute = new List<IPrimitive>(_primitives);
                    allPrimitivesToRedistribute.Add(primitive);

                    _primitives.Clear();

                    foreach (var p in allPrimitivesToRedistribute)
                    {
                        bool insertedIntoChild = false;
                        foreach (var child in _children)
                        {
                            if (child.Contains(p))
                            {
                                child.Insert(p);
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
            else
            {
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

            // Children split X/Z but span the FULL parent Y (full-height columns) for even distribution.
            UVector3 childSize = new UVector3(HalfSize.x, HalfSize.y * 2f, HalfSize.z);
            _children[0] = new QuadTreeNode(Center + new UVector3(-quarterSize.x, 0, -quarterSize.z), childSize, Capacity, Depth + 1); // -X -Z
            _children[1] = new QuadTreeNode(Center + new UVector3(quarterSize.x, 0, -quarterSize.z), childSize, Capacity, Depth + 1);  // +X -Z
            _children[2] = new QuadTreeNode(Center + new UVector3(-quarterSize.x, 0, quarterSize.z), childSize, Capacity, Depth + 1);  // -X +Z
            _children[3] = new QuadTreeNode(Center + new UVector3(quarterSize.x, 0, quarterSize.z), childSize, Capacity, Depth + 1);   // +X +Z
        }

        public bool Remove(IPrimitive primitive)
        {
            if (_isLeaf)
            {
                if (_primitives.Remove(primitive))
                {
                    RecomputeBounds(); // keep cached content-bounds correct after removal
                    return true;
                }
                return false;
            }
            else
            {
                foreach (var child in _children)
                {
                    // Recurse only where it could be (content-bounds first, node bounds fallback).
                    if (child.ContentOrNodeIntersects(primitive))
                    {
                        if (child.Remove(primitive))
                        {
                            RecomputeBounds(); // refresh this node's bounds from updated children
                            return true;
                        }
                    }
                }
                // If not removed from any child, it must be in this node's direct primitives.
                if (_primitives.Remove(primitive))
                {
                    RecomputeBounds();
                    return true;
                }
                return false;
            }
        }

        // Prefer cached content-bounds; fall back to geometric node bounds.
        private bool ContentOrNodeIntersects(IPrimitive primitive)
        {
            if (_hasContent)
            {
                return !(primitive.AABBMax.x < _subMin.x || primitive.AABBMin.x > _subMax.x ||
                         primitive.AABBMax.y < _subMin.y || primitive.AABBMin.y > _subMax.y ||
                         primitive.AABBMax.z < _subMin.z || primitive.AABBMin.z > _subMax.z);
            }
            return Intersects(primitive);
        }

        public void QueryRay(CRay ray, float maxDistance, List<IPrimitive> result)
        {
            // One test against the subtree bounds can skip everything below (never skips a reachable primitive).
            if (!_hasContent) return;
            if (!RayAabb.Intersects(ray, _subMin, _subMax, maxDistance)) return;

            RaycastDiagnostics.CountNode();

            for (int p = 0; p < _primitives.Count; p++)
            {
                IPrimitive primitive = _primitives[p];
                if (RayAabb.Intersects(ray, primitive.AABBMin, primitive.AABBMax, maxDistance))
                {
                    result.Add(primitive);
                }
            }

            // Recurse into children (each self-culls on its own content-bounds at entry).
            if (!_isLeaf)
            {
                for (int c = 0; c < _children.Length; c++)
                {
                    _children[c].QueryRay(ray, maxDistance, result);
                }
            }
        }

        // Near-to-far, AABB windows capped at the best hit: skipped candidates have t_entry >= bestT,
        // so the winner never changes.
        public void RaycastClosest(CRay ray, float maxDistance, ref CHitInfo best, ref float bestT, ref bool found)
        {
            if (!_hasContent) return;
            float cullT = bestT < maxDistance ? bestT : maxDistance;
            if (!RayAabb.Intersects(ray, _subMin, _subMax, cullT)) return;

            RaycastDiagnostics.CountNode();

            // Straddlers held directly at this node.
            for (int p = 0; p < _primitives.Count; p++)
            {
                IPrimitive primitive = _primitives[p];
                float pCullT = bestT < maxDistance ? bestT : maxDistance;
                if (!RayAabb.Intersects(ray, primitive.AABBMin, primitive.AABBMax, pCullT))
                    continue;
                if (primitive.IntersectRay(ray, out CHitInfo currentHit, maxDistance) &&
                    currentHit.Distance < bestT)
                {
                    bestT = currentHit.Distance;
                    best = currentHit;
                    found = true;
                }
            }

            if (!_isLeaf)
            {
                int orderIndex = (ray.Direction.x < 0f ? 1 : 0) + (ray.Direction.z < 0f ? 2 : 0);
                for (int c = 0; c < 4; c++)
                {
                    _children[ChildOrder[orderIndex, c]].RaycastClosest(ray, maxDistance, ref best, ref bestT, ref found);
                }
            }
        }
    }
}
