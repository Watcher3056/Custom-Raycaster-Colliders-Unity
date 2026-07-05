// Moved/resized primitives must be hittable at their NEW location and not at the old one.
using System;
using Xunit;

public class StaleAabbRegressionTests
{
    [Fact]
    public void MovedSphere_IsHitAtNewLocation_AndMissedAtOldLocation()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            var p = sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 0, 10), 1.5f));

            // Move it far away along +X.
            sys.UpdatePrimitive(p.ID, new UVector3(30, 0, 0), UQuaternion.identity, p.Size);

            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(1, 0, 0), 100f), out CHitInfo hit, 100f),
                $"useQuadTree={useQuadTree}: sphere must be hittable at its NEW location");
            Assert.True(Math.Abs(hit.Distance - 28.5f) < 0.05f, $"Expected ~28.5, got {hit.Distance}");

            Assert.False(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 100f), out _, 100f),
                $"useQuadTree={useQuadTree}: sphere must NOT be hittable at its OLD location");
        }
    }

    [Fact]
    public void MovedBox_IsHitAtNewLocation_AndMissedAtOldLocation()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            var p = sys.AddPrimitive(new BoxPrimitive(0, new UVector3(0, 0, 10), UQuaternion.identity, new UVector3(2, 2, 2)));

            sys.UpdatePrimitive(p.ID, new UVector3(-20, 0, 0), UQuaternion.identity, new UVector3(2, 2, 2));

            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(-1, 0, 0), 100f), out CHitInfo hit, 100f),
                $"useQuadTree={useQuadTree}: box must be hittable at its NEW location");
            Assert.True(Math.Abs(hit.Distance - 19f) < 1e-2f, $"Expected 19, got {hit.Distance}");

            Assert.False(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 100f), out _, 100f),
                $"useQuadTree={useQuadTree}: box must NOT be hittable at its OLD location");
        }
    }

    [Fact]
    public void ResizedPrimitive_AABB_Follows()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            var p = sys.AddPrimitive(new SpherePrimitive(0, new UVector3(0, 8, 0), 1f));

            // Grow the sphere so a ray passing y=8 at x-offset 4 now hits (radius 1 -> 5).
            sys.UpdatePrimitive(p.ID, new UVector3(0, 8, 0), UQuaternion.identity, new UVector3(5, 5, 5));

            var ray = new CRay(new UVector3(4, -20, 0), new UVector3(0, 1, 0), 100f);
            Assert.True(sys.Raycast(ray, out CHitInfo hit, 100f),
                $"useQuadTree={useQuadTree}: grown sphere must be hit by the offset ray");
            Assert.True(hit.Distance > 0f && hit.Distance < 100f);
        }
    }

    [Fact]
    public void MovedCapsule_IsHitAtNewLocation_AndMissedAtOldLocation()
    {
        foreach (bool useQuadTree in new[] { false, true })
        {
            var sys = TestSceneBuilder.NewSystem(useQuadTree);
            var p = sys.AddPrimitive(new CapsulePrimitive(0, new UVector3(0, 0, 12), UQuaternion.identity, new UVector3(1f, 4f, 1f)));

            sys.UpdatePrimitive(p.ID, new UVector3(0, 25, 0), UQuaternion.identity, new UVector3(1f, 4f, 1f));

            Assert.True(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 1, 0), 100f), out CHitInfo hit, 100f),
                $"useQuadTree={useQuadTree}: capsule must be hittable at its NEW location");
            // Bottom cap: center 25, cylinder half = 4/2 - 1 = 1, bottom sphere at y=24, r=1 -> hit at 23.
            Assert.True(Math.Abs(hit.Distance - 23f) < 0.05f, $"Expected ~23, got {hit.Distance}");

            Assert.False(sys.Raycast(new CRay(UVector3.zero, new UVector3(0, 0, 1), 100f), out _, 100f),
                $"useQuadTree={useQuadTree}: capsule must NOT be hittable at its OLD location");
        }
    }
}
