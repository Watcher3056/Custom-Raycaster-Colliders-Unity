// Deterministic scene helpers -- fixed seeds only.
//
// SINGLE SHARED SOURCE. This file is compiled twice, from one copy:
//   * by Unity, in the CustomRaycastSystem.Tests assembly (RaycastBenchmark uses it), and
//   * by the headless Tests.Core xUnit project, via an explicit <Compile Include> in
//     Tests.Core.csproj (the same trick the project already uses to compile the Core).
// Because both the Unity performance benchmark and the headless differential tests call the
// exact same BuildSpecs()/BuildScene(), they exercise byte-identical scenes.
using System;
using System.Collections.Generic;

#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
#endif

public enum PrimKind { Box, Sphere, Capsule }

// One generated primitive, engine-agnostic: carries enough to build either a
// CustomRaycastSystemCore primitive or a matching UnityEngine collider.
public struct PrimitiveSpec
{
    public PrimKind Kind;
    public UVector3 Position;
    public UQuaternion Rotation;   // identity for spheres
    public UVector3 Size;          // Box: full dimensions; Capsule: (radius, totalHeight, radius); Sphere: (r,r,r)
    public float Radius;           // Sphere / Capsule radius
    public float Height;           // Capsule total height (incl. caps); 0 otherwise
}

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

    // Same seed => same list of specs. The RNG call order in this method is the single source
    // of truth for what a scene *is*. BuildScene() (custom engine) and RaycastBenchmark (custom
    // engine + Unity Physics) both consume these specs, so every engine sees identical geometry.
    public static List<PrimitiveSpec> BuildSpecs(int seed, int n, float worldRange, bool withCapsules)
    {
        var r = new Random(seed);
        var specs = new List<PrimitiveSpec>(n);
        for (int i = 0; i < n; i++)
        {
            var pos = RandVec(r, worldRange);
            double kind = r.NextDouble();
            if (withCapsules && kind < 1.0 / 3.0)
            {
                float radius = (float)(0.4 + r.NextDouble() * 1.0);
                float height = 2f * radius + (float)(r.NextDouble() * 3.0);
                var rot = AxisAngle(RandDir(r), (float)(r.NextDouble() * 360.0));
                specs.Add(new PrimitiveSpec
                {
                    Kind = PrimKind.Capsule,
                    Position = pos,
                    Rotation = rot,
                    Size = new UVector3(radius, height, radius),
                    Radius = radius,
                    Height = height
                });
            }
            else if (kind < (withCapsules ? 2.0 / 3.0 : 0.5))
            {
                float radius = (float)(0.5 + r.NextDouble() * 2.0);
                specs.Add(new PrimitiveSpec
                {
                    Kind = PrimKind.Sphere,
                    Position = pos,
                    Rotation = UQuaternion.identity,
                    Size = new UVector3(radius, radius, radius),
                    Radius = radius
                });
            }
            else
            {
                var size = new UVector3((float)(0.5 + r.NextDouble() * 2.0),
                                        (float)(0.5 + r.NextDouble() * 2.0),
                                        (float)(0.5 + r.NextDouble() * 2.0));
                var rot = AxisAngle(RandDir(r), (float)(r.NextDouble() * 360.0));
                specs.Add(new PrimitiveSpec
                {
                    Kind = PrimKind.Box,
                    Position = pos,
                    Rotation = rot,
                    Size = size
                });
            }
        }
        return specs;
    }

    // Register one spec into a custom core (system auto-assigns the id from placeholder 0).
    public static IPrimitive AddSpec(CustomRaycastSystemCore sys, PrimitiveSpec s)
    {
        switch (s.Kind)
        {
            case PrimKind.Capsule: return sys.AddPrimitive(new CapsulePrimitive(0, s.Position, s.Rotation, s.Size));
            case PrimKind.Sphere:  return sys.AddPrimitive(new SpherePrimitive(0, s.Position, s.Radius));
            default:               return sys.AddPrimitive(new BoxPrimitive(0, s.Position, s.Rotation, s.Size));
        }
    }

    // Same seed => same scene. Every primitive uses placeholder id 0 (system auto-assigns).
    // Defined in terms of BuildSpecs so the recipe lives in exactly one place; the RNG sequence
    // is unchanged versus the original, so existing seeded tests see identical scenes and ids.
    public static void BuildScene(CustomRaycastSystemCore sys, int seed, int n, float worldRange, bool withCapsules)
    {
        var specs = BuildSpecs(seed, n, worldRange, withCapsules);
        for (int i = 0; i < specs.Count; i++) AddSpec(sys, specs[i]);
    }
}
