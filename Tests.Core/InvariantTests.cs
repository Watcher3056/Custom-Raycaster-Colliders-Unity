// Public-API invariants.
using System;
using Xunit;

public class InvariantTests
{
    [Fact]
    public void RaycastAll_IsSortedByDistance_BothStructures()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            TestSceneBuilder.BuildScene(sys, 7, 40, 30f, true);
            var rray = new Random(99);
            for (int i = 0; i < 60; i++)
            {
                var ray = new CRay(TestSceneBuilder.RandVec(rray, 30f), TestSceneBuilder.RandDir(rray), 1000f);
                var hits = sys.RaycastAll(ray, 1000f, true);
                for (int k = 1; k < hits.Count; k++)
                {
                    Assert.True(hits[k - 1].Distance <= hits[k].Distance + 1e-4f,
                        "RaycastAll not sorted ascending");
                    if (hits[k - 1].Distance == hits[k].Distance)
                    {
                        // Determinism: equal distances tie-break by PrimitiveID.
                        Assert.True(hits[k - 1].PrimitiveID < hits[k].PrimitiveID,
                            "equal-distance hits must be ordered by PrimitiveID");
                    }
                }
            }
        }
    }

    [Fact]
    public void Hit_RespectsMaxDistance_And_PointLiesOnRay()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            TestSceneBuilder.BuildScene(sys, 11, 40, 30f, true);
            var rray = new Random(123);
            for (int i = 0; i < 100; i++)
            {
                float maxD = (float)(5 + rray.NextDouble() * 40);
                var ray = new CRay(TestSceneBuilder.RandVec(rray, 30f), TestSceneBuilder.RandDir(rray), maxD);
                if (sys.Raycast(ray, out CHitInfo hit, maxD))
                {
                    Assert.True(hit.Distance >= -1e-3f && hit.Distance <= maxD + 1e-2f,
                        $"Hit distance {hit.Distance} outside [0,{maxD}]");
                    var p = ray.GetPoint(hit.Distance);
                    Assert.True((p - hit.HitPoint).magnitude < 1e-1f, "Reported hit point is not on the ray");
                    Assert.NotNull(hit.PrimitiveReference);
                    Assert.Equal(hit.PrimitiveReference.ID, hit.PrimitiveID);
                }
            }
        }
    }

    [Fact]
    public void DirectRay_Hits_OppositeRay_Misses()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 0, 10), 1.5f));
            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 100f), out CHitInfo hit, 100f),
                "A ray fired straight at a sphere ahead should hit it");
            Assert.True(Math.Abs(hit.Distance - 8.5f) < 0.25f, $"Expected hit distance ~8.5, got {hit.Distance}");
            Assert.False(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, -1), 100f), out _, 100f),
                "A ray fired away from the sphere should miss");
        }
    }

    [Fact]
    public void Box_ReportsWorldDistance_NotLocal()
    {
        // Regression pin for the box-math fix: a 2x2x2 box centered at x=10 hit along
        // +X from the origin must report distance 9 (10 - half-extent 1), not a scaled value.
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            sys.AddPrimitive(new BoxPrimitive(0, new UVector3(10, 0, 0), UQuaternion.identity, new UVector3(2, 2, 2)));
            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(1, 0, 0), 100f), out CHitInfo hit, 100f));
            Assert.True(Math.Abs(hit.Distance - 9f) < 1e-3f, $"Expected 9, got {hit.Distance}");
            Assert.True(Math.Abs(hit.Normal.x - (-1f)) < 1e-3f, "Expected -X face normal");
        }
    }
}
