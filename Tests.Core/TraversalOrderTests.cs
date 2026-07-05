// The pruned Raycast must return exactly the sorted-RaycastAll winner (both structures).
using System;
using Xunit;

public class TraversalOrderTests
{
    [Fact]
    public void Raycast_Equals_FirstOfSortedRaycastAll_OnDenseCorridor()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            // A corridor of overlapping spheres straight down +Z, plus jittered off-axis noise.
            var r = new Random(555);
            for (int i = 0; i < 40; i++)
            {
                sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 0, 5 + i * 1.5f), 1.2f));
            }
            for (int i = 0; i < 100; i++)
            {
                sys.AddPrimitive(new SpherePrimitive(0, TestSceneBuilder.RandVec(r, 30f), (float)(0.5 + r.NextDouble())));
            }

            var rray = new Random(556);
            for (int i = 0; i < 120; i++)
            {
                // Half the rays go straight down the corridor, half are random.
                CRay ray = (i % 2 == 0)
                    ? new CRay(new UVector3((float)(r.NextDouble() * 0.5), 0, 0), new UVector3(0, 0, 1), 1000f)
                    : new CRay(TestSceneBuilder.RandVec(rray, 30f), TestSceneBuilder.RandDir(rray), 1000f);

                bool hit = sys.Raycast(ray, out CHitInfo closest, 1000f);
                var all = sys.RaycastAll(ray, 1000f, true);

                Assert.True(hit == (all.Count > 0),
                    $"useQuadTree={useQuadTree}: Raycast hit={hit} but RaycastAll count={all.Count}");
                if (hit)
                {
                    Assert.True(Math.Abs(closest.Distance - all[0].Distance) <= 1e-5f,
                        $"useQuadTree={useQuadTree}: early-out winner {closest.Distance:F6} != exhaustive winner {all[0].Distance:F6}");
                }
            }
        }
    }

    [Fact]
    public void Raycast_AgreesAcrossStructures_OnRaysAlongEveryAxisSign()
    {
        // Covers all 4 child-visit orders and axis-parallel rays (zero direction components).
        var simple = TestSceneBuilder.NewSystem(false);
        var quad = TestSceneBuilder.NewSystem(true);
        TestSceneBuilder.BuildScene(simple, 909, 60, 25f, true);
        TestSceneBuilder.BuildScene(quad, 909, 60, 25f, true);

        UVector3[] dirs =
        {
            new UVector3(1, 0, 0), new UVector3(-1, 0, 0),
            new UVector3(0, 1, 0), new UVector3(0, -1, 0),
            new UVector3(0, 0, 1), new UVector3(0, 0, -1),
            new UVector3(1, 0, 1), new UVector3(-1, 0, 1),
            new UVector3(1, 0, -1), new UVector3(-1, 0, -1),
            new UVector3(1, 1, 1), new UVector3(-1, -1, -1)
        };
        var r = new Random(910);
        foreach (var d in dirs)
        {
            for (int i = 0; i < 15; i++)
            {
                var origin = TestSceneBuilder.RandVec(r, 25f);
                var ray = new CRay(origin, d, 1000f);
                bool hS = simple.Raycast(ray, out CHitInfo hitS, 1000f);
                bool hQ = quad.Raycast(ray, out CHitInfo hitQ, 1000f);
                Assert.True(hS == hQ, $"dir {d.x},{d.y},{d.z}: hit mismatch");
                if (hS) Assert.True(Math.Abs(hitS.Distance - hitQ.Distance) <= 1e-4f,
                    $"dir {d.x},{d.y},{d.z}: distance mismatch {hitS.Distance:F6} vs {hitQ.Distance:F6}");
            }
        }
    }
}
