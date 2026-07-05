// After warm-up, Raycast must allocate ZERO bytes on the calling thread (both structures).
using System;
using Xunit;

public class ZeroAllocTests
{
    private static long MeasureRaycastAllocations(bool useQuadTree)
    {
        var sys = TestSceneBuilder.NewSystem(useQuadTree, 8);
        TestSceneBuilder.BuildScene(sys, 77, 500, TestSceneBuilder.World, true);

        // Pre-build the ray list so its allocation is outside the measured window.
        var rr = new Random(78);
        var origins = new UVector3[2000];
        var dirs = new UVector3[2000];
        for (int i = 0; i < origins.Length; i++)
        {
            origins[i] = TestSceneBuilder.RandVec(rr, TestSceneBuilder.World);
            dirs[i] = TestSceneBuilder.RandDir(rr);
        }

        long hits = 0;

        // Warm-up: JIT everything and let any lazy structures reach steady state.
        for (int i = 0; i < origins.Length; i++)
        {
            var ray = new CRay(origins[i], dirs[i], 500f);
            if (sys.Raycast(ray, out CHitInfo h, 500f)) hits += h.PrimitiveID >= 0 ? 1 : 0;
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int pass = 0; pass < 5; pass++)
        {
            for (int i = 0; i < origins.Length; i++)
            {
                var ray = new CRay(origins[i], dirs[i], 500f);
                if (sys.Raycast(ray, out CHitInfo h, 500f)) hits += h.PrimitiveID >= 0 ? 1 : 0;
            }
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(hits > 0, "workload sanity: some rays must hit");
        return allocated;
    }

    [Fact]
    public void Raycast_AllocatesNothing_QuadTree()
    {
        long allocated = MeasureRaycastAllocations(true);
        Assert.True(allocated == 0, $"QuadTree Raycast path allocated {allocated} bytes over 10,000 rays; expected 0");
    }

    [Fact]
    public void Raycast_AllocatesNothing_SimpleList()
    {
        long allocated = MeasureRaycastAllocations(false);
        Assert.True(allocated == 0, $"SimpleList Raycast path allocated {allocated} bytes over 10,000 rays; expected 0");
    }
}
