// QuadTree and SimpleList must return identical results -- including after move/remove/re-add.
using System;
using System.Collections.Generic;
using Xunit;

public class DifferentialTests
{
    private static void AssertRayAgreement(CustomRaycastSystemCore simple, CustomRaycastSystemCore quad,
        int raySeed, int rays, float range, float maxDistance, string context)
    {
        var rray = new Random(raySeed);
        for (int i = 0; i < rays; i++)
        {
            var ray = new CRay(TestSceneBuilder.RandVec(rray, range), TestSceneBuilder.RandDir(rray), maxDistance);

            bool hS = simple.Raycast(ray, out CHitInfo hitS, maxDistance);
            bool hQ = quad.Raycast(ray, out CHitInfo hitQ, maxDistance);
            Assert.True(hS == hQ, $"{context}: ray {i} hit mismatch simple={hS} quad={hQ}");
            if (hS)
            {
                Assert.True(Math.Abs(hitS.Distance - hitQ.Distance) <= 1e-4f,
                    $"{context}: ray {i} distance simple={hitS.Distance:F6} quad={hitQ.Distance:F6}");
            }

            List<CHitInfo> allS = simple.RaycastAll(ray, maxDistance, true);
            List<CHitInfo> allQ = quad.RaycastAll(ray, maxDistance, true);
            Assert.True(allS.Count == allQ.Count,
                $"{context}: ray {i} RaycastAll count simple={allS.Count} quad={allQ.Count}");
            for (int k = 0; k < allS.Count; k++)
            {
                Assert.True(Math.Abs(allS[k].Distance - allQ[k].Distance) <= 1e-4f,
                    $"{context}: ray {i} RaycastAll[{k}] distance mismatch");
                Assert.True(allS[k].PrimitiveID == allQ[k].PrimitiveID,
                    $"{context}: ray {i} RaycastAll[{k}] id simple={allS[k].PrimitiveID} quad={allQ[k].PrimitiveID}");
            }
        }
    }

    [Fact]
    public void QuadTree_Matches_SimpleList_SpheresAndBoxes()
    {
        for (int scene = 0; scene < 15; scene++)
        {
            var simple = TestSceneBuilder.NewSystem(false);
            var quad = TestSceneBuilder.NewSystem(true);
            TestSceneBuilder.BuildScene(simple, 1000 + scene, 30, TestSceneBuilder.World, false);
            TestSceneBuilder.BuildScene(quad, 1000 + scene, 30, TestSceneBuilder.World, false);
            AssertRayAgreement(simple, quad, 5000 + scene, 60, TestSceneBuilder.World, 1000f, $"static scene {scene}");
        }
    }

    [Fact]
    public void QuadTree_Matches_SimpleList_WithCapsules()
    {
        for (int scene = 0; scene < 10; scene++)
        {
            var simple = TestSceneBuilder.NewSystem(false);
            var quad = TestSceneBuilder.NewSystem(true);
            TestSceneBuilder.BuildScene(simple, 2000 + scene, 30, TestSceneBuilder.World, true);
            TestSceneBuilder.BuildScene(quad, 2000 + scene, 30, TestSceneBuilder.World, true);
            AssertRayAgreement(simple, quad, 6000 + scene, 60, TestSceneBuilder.World, 1000f, $"capsule scene {scene}");
        }
    }

    [Fact]
    public void QuadTree_Matches_SimpleList_WithShortMaxDistance()
    {
        var simple = TestSceneBuilder.NewSystem(false);
        var quad = TestSceneBuilder.NewSystem(true);
        TestSceneBuilder.BuildScene(simple, 42, 40, 25f, true);
        TestSceneBuilder.BuildScene(quad, 42, 40, 25f, true);
        AssertRayAgreement(simple, quad, 43, 80, 25f, 12f, "short maxDistance");
    }

    [Fact]
    public void QuadTree_Matches_SimpleList_AfterMutations()
    {
        for (int scene = 0; scene < 8; scene++)
        {
            var simple = TestSceneBuilder.NewSystem(false);
            var quad = TestSceneBuilder.NewSystem(true);
            TestSceneBuilder.BuildScene(simple, 3000 + scene, 30, TestSceneBuilder.World, true);
            TestSceneBuilder.BuildScene(quad, 3000 + scene, 30, TestSceneBuilder.World, true);

            // Identical mutation script for both systems (ids 0..29, auto-assigned in add order).
            var rm = new Random(7000 + scene);
            for (int step = 0; step < 20; step++)
            {
                int id = rm.Next(0, 30);
                double op = rm.NextDouble();
                if (op < 0.6)
                {
                    // Move (and re-orient / resize) an existing primitive.
                    var newPos = TestSceneBuilder.RandVec(rm, TestSceneBuilder.World);
                    var newRot = TestSceneBuilder.AxisAngle(TestSceneBuilder.RandDir(rm), (float)(rm.NextDouble() * 360.0));
                    var newSize = new UVector3((float)(0.5 + rm.NextDouble() * 2.0),
                                               (float)(0.5 + rm.NextDouble() * 2.0),
                                               (float)(0.5 + rm.NextDouble() * 2.0));
                    simple.UpdatePrimitive(id, newPos, newRot, newSize);
                    quad.UpdatePrimitive(id, newPos, newRot, newSize);
                }
                else if (op < 0.8)
                {
                    // Remove (removing an already-removed id must be a harmless no-op).
                    simple.RemovePrimitive(id);
                    quad.RemovePrimitive(id);
                }
                else
                {
                    // Add a fresh primitive (id=0 placeholder; system assigns).
                    var pos = TestSceneBuilder.RandVec(rm, TestSceneBuilder.World);
                    float radius = (float)(0.5 + rm.NextDouble() * 1.5);
                    simple.AddPrimitive(new SpherePrimitive(0, pos, radius));
                    quad.AddPrimitive(new SpherePrimitive(0, pos, radius));
                }
            }

            AssertRayAgreement(simple, quad, 8000 + scene, 60, TestSceneBuilder.World, 1000f, $"mutated scene {scene}");
        }
    }

    [Fact]
    public void AllocatingQueryRay_And_BufferQueryRay_ReturnSameCandidates()
    {
        // Both QueryRay entry points must return identical candidates.
        IAccelerationStructure[] structures =
        {
            new SimpleListAccelerationStructure(),
            new QuadTree(UVector3.zero, new UVector3(160, 160, 160), 4)
        };
        foreach (var structure in structures)
        {
            var r = new Random(11);
            for (int i = 0; i < 25; i++)
            {
                var p = new SpherePrimitive(i, TestSceneBuilder.RandVec(r, 30f), (float)(0.5 + r.NextDouble()));
                structure.AddPrimitive(p);
            }
            var rray = new Random(12);
            var buffer = new List<IPrimitive>();
            for (int i = 0; i < 40; i++)
            {
                var ray = new CRay(TestSceneBuilder.RandVec(rray, 30f), TestSceneBuilder.RandDir(rray), 500f);
                List<IPrimitive> a = structure.QueryRay(ray, 500f);
                buffer.Clear();
                structure.QueryRay(ray, 500f, buffer);
                Assert.Equal(a.Count, buffer.Count);
                for (int k = 0; k < a.Count; k++) Assert.Same(a[k], buffer[k]);
            }
        }
    }
}
