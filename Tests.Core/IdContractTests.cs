// Placeholder ids (commonly all 0) must get unique sequential ids -- never reject, never throw.
using System.Collections.Generic;
using Xunit;

public class IdContractTests
{
    [Fact]
    public void AllZeroPlaceholderIds_GetUniqueSequentialIds()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            var seen = new HashSet<int>();
            for (int i = 0; i < 50; i++)
            {
                var p = sys.AddPrimitive(new SpherePrimitive(0, new UVector3(i * 2f, 0, 0), 0.5f));
                Assert.Equal(i, p.ID); // sequential from 0
                Assert.True(seen.Add(p.ID), $"duplicate id {p.ID}");
            }
        }
    }

    [Fact]
    public void MultiPrimitiveScene_WithAllZeroIds_IsFullyRaycastable()
    {
        // The common caller pattern: many primitives, every one constructed with id=0.
        var sys = TestSceneBuilder.NewSystem(true);
        for (int i = 0; i < 10; i++)
        {
            sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 0, 5 + i * 5f), 1f));
        }
        // The closest one (z=5) wins; every one is individually reachable after removals.
        Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 1000f), out CHitInfo hit, 1000f));
        Assert.Equal(0, hit.PrimitiveID);
        Assert.True(System.Math.Abs(hit.Distance - 4f) < 1e-3f);

        sys.RemovePrimitive(0);
        Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 1000f), out hit, 1000f));
        Assert.Equal(1, hit.PrimitiveID); // next-closest (z=10) after removing the first
    }

    [Fact]
    public void RemoveAndUpdate_UseTheAssignedId()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            var a = sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 0, 10), 1f));
            var b = sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 0, 20), 1f));

            sys.RemovePrimitive(a.ID);
            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 1000f), out CHitInfo hit, 1000f));
            Assert.Equal(b.ID, hit.PrimitiveID);

            // Update by id moves the survivor; removing a stale id is a harmless no-op.
            sys.UpdatePrimitive(b.ID, new UVector3(0, 0, 30), UQuaternion.identity, b.Size);
            sys.RemovePrimitive(a.ID);
            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 1000f), out hit, 1000f));
            Assert.True(System.Math.Abs(hit.Distance - 29f) < 1e-3f, $"Expected 29, got {hit.Distance}");
        }
    }
}
