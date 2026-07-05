// Deterministic scene helpers -- fixed seeds only.
using System;

public static class TestSceneBuilder
{
    public const float World = 40f;

    public static UVector3 RandVec(Random r, float range)
        => new UVector3((float)(r.NextDouble() * 2 - 1) * range,
                        (float)(r.NextDouble() * 2 - 1) * range,
                        (float)(r.NextDouble() * 2 - 1) * range);

    public static UVector3 RandDir(Random r)
    {
        UVector3 d;
        do { d = RandVec(r, 1f); } while (d.magnitude < 1e-3f);
        return d;
    }

    // q = (n*sin(a/2), cos(a/2)); the headless stub has no Euler/AngleAxis.
    public static UQuaternion AxisAngle(UVector3 axis, float degrees)
    {
        UVector3 n = axis.normalized;
        double half = degrees * Math.PI / 360.0;
        float s = (float)Math.Sin(half);
        float c = (float)Math.Cos(half);
        return new UQuaternion(n.x * s, n.y * s, n.z * s, c);
    }

    public static CustomRaycastSystemCore NewSystem(bool useQuadTree, int capacity = 4)
        => new CustomRaycastSystemCore(useQuadTree, UVector3.zero,
            new UVector3(World * 4, World * 4, World * 4), capacity);

    // Same seed => same scene. Every primitive uses placeholder id 0 (system auto-assigns).
    public static void BuildScene(CustomRaycastSystemCore sys, int seed, int n, float worldRange, bool withCapsules)
    {
        var r = new Random(seed);
        for (int i = 0; i < n; i++)
        {
            var pos = RandVec(r, worldRange);
            double kind = r.NextDouble();
            if (withCapsules && kind < 1.0 / 3.0)
            {
                float radius = (float)(0.4 + r.NextDouble() * 1.0);
                float height = 2f * radius + (float)(r.NextDouble() * 3.0);
                var rot = AxisAngle(RandDir(r), (float)(r.NextDouble() * 360.0));
                sys.AddPrimitive(new CapsulePrimitive(0, pos, rot, new UVector3(radius, height, radius)));
            }
            else if (kind < (withCapsules ? 2.0 / 3.0 : 0.5))
            {
                float radius = (float)(0.5 + r.NextDouble() * 2.0);
                sys.AddPrimitive(new SpherePrimitive(0, pos, radius));
            }
            else
            {
                var size = new UVector3((float)(0.5 + r.NextDouble() * 2.0),
                                        (float)(0.5 + r.NextDouble() * 2.0),
                                        (float)(0.5 + r.NextDouble() * 2.0));
                var rot = AxisAngle(RandDir(r), (float)(r.NextDouble() * 360.0));
                sys.AddPrimitive(new BoxPrimitive(0, pos, rot, size));
            }
        }
    }
}
