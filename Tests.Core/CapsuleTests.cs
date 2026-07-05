// Geometry tests for the capsule primitive, independent of any structure.
using System;
using Xunit;

public class CapsuleTests
{
    // Radius 1, height 6 -> cylinder half-length 2, cap centers at y = +/-2.
    private static CapsulePrimitive MakeVertical()
        => new CapsulePrimitive(1, UVector3.zero, UQuaternion.identity, new UVector3(1f, 6f, 1f));

    [Fact]
    public void SideRay_HitsCylinderWall_AtExpectedDistance()
    {
        var cap = MakeVertical();
        var ray = new CRay(new UVector3(-10, 0, 0), new UVector3(1, 0, 0), 100f);
        Assert.True(cap.IntersectRay(ray, out CHitInfo hit, 100f));
        Assert.True(Math.Abs(hit.Distance - 9f) < 1e-3f, $"Expected 9 (wall at x=-1), got {hit.Distance}");
        Assert.True(Math.Abs(hit.Normal.x - (-1f)) < 1e-3f, "Wall normal must point -X");
    }

    [Fact]
    public void AxialRay_HitsCap_AtExpectedDistance()
    {
        var cap = MakeVertical();
        var ray = new CRay(new UVector3(0, 10, 0), new UVector3(0, -1, 0), 100f);
        Assert.True(cap.IntersectRay(ray, out CHitInfo hit, 100f));
        // Top of the top cap: cap center y=2, radius 1 -> surface at y=3 -> distance 7.
        Assert.True(Math.Abs(hit.Distance - 7f) < 1e-3f, $"Expected 7 (cap top at y=3), got {hit.Distance}");
        Assert.True(hit.Normal.y > 0.99f, "Cap normal must point +Y");
    }

    [Fact]
    public void RayPastTheCap_Misses()
    {
        var cap = MakeVertical();
        // Passes at x=1.5 > radius 1, above the cylinder: must miss.
        var ray = new CRay(new UVector3(1.5f, 10, 0), new UVector3(0, -1, 0), 100f);
        Assert.False(cap.IntersectRay(ray, out _, 100f));
    }

    [Fact]
    public void RotatedCapsule_BehavesLikeAxisAlignedOne()
    {
        // Rotate the vertical capsule 90 degrees about Z: its axis now lies along X.
        var rot = TestSceneBuilder.AxisAngle(new UVector3(0, 0, 1), 90f);
        var cap = new CapsulePrimitive(2, UVector3.zero, rot, new UVector3(1f, 6f, 1f));

        // A ray along -Y toward the (now horizontal) body must hit the wall at y=1 -> dist 9.
        var ray = new CRay(new UVector3(1.5f, 10, 0), new UVector3(0, -1, 0), 100f);
        Assert.True(cap.IntersectRay(ray, out CHitInfo hit, 100f), "Body lies along X; x=1.5 is inside the segment span");
        Assert.True(Math.Abs(hit.Distance - 9f) < 1e-3f, $"Expected 9, got {hit.Distance}");

        // Along the old axis (+Y at x=0) the capsule is now only radius-thick: surface at y=1.
        var ray2 = new CRay(new UVector3(0, 10, 0), new UVector3(0, -1, 0), 100f);
        Assert.True(cap.IntersectRay(ray2, out CHitInfo hit2, 100f));
        Assert.True(Math.Abs(hit2.Distance - 9f) < 1e-3f, $"Expected 9, got {hit2.Distance}");
    }

    [Fact]
    public void DegenerateHeight_ClampsToSphere()
    {
        // Height <= 2*radius -> cylinder half-length clamps to 0: behaves like a sphere r=1.
        var cap = new CapsulePrimitive(3, new UVector3(0, 0, 5), UQuaternion.identity, new UVector3(1f, 1f, 1f));
        var ray = new CRay(UVector3.zero, new UVector3(0, 0, 1), 100f);
        Assert.True(cap.IntersectRay(ray, out CHitInfo hit, 100f));
        Assert.True(Math.Abs(hit.Distance - 4f) < 1e-3f, $"Expected 4 (sphere surface at z=4), got {hit.Distance}");
    }

    [Fact]
    public void AABB_EnclosesTheWholeCapsule()
    {
        var rot = TestSceneBuilder.AxisAngle(new UVector3(0, 0, 1), 90f);
        var cap = new CapsulePrimitive(4, new UVector3(5, 5, 5), rot, new UVector3(1f, 6f, 1f));
        // Axis along X, segment half 2 -> AABB x: [5-3, 5+3], y/z: [5-1, 5+1].
        Assert.True(Math.Abs(cap.AABBMin.x - 2f) < 1e-3f && Math.Abs(cap.AABBMax.x - 8f) < 1e-3f, "X extent");
        Assert.True(Math.Abs(cap.AABBMin.y - 4f) < 1e-3f && Math.Abs(cap.AABBMax.y - 6f) < 1e-3f, "Y extent");
        Assert.True(Math.Abs(cap.AABBMin.z - 4f) < 1e-3f && Math.Abs(cap.AABBMax.z - 6f) < 1e-3f, "Z extent");
    }
}
