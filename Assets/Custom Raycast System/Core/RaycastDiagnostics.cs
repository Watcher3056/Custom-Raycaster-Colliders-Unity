// Optional op counters for profiling; Enabled = false by default so the hot path costs nothing.
public static class RaycastDiagnostics
{
    public static bool Enabled = false;

    public static long IntersectionTests;   // primitive IntersectRay calls
    public static long NodesVisited;         // acceleration-structure nodes entered
    public static long AabbTests;            // ray-vs-AABB slab tests performed

    public static void Reset()
    {
        IntersectionTests = 0;
        NodesVisited = 0;
        AabbTests = 0;
    }

    public static void CountIntersection() { if (Enabled) IntersectionTests++; }
    public static void CountNode() { if (Enabled) NodesVisited++; }
    public static void CountAabb() { if (Enabled) AabbTests++; }
}
