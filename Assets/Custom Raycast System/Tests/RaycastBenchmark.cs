#if UNITY_5_3_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Head-to-head performance harness: Unity's built-in Physics.Raycast vs this project's
/// CustomRaycastSystemCore, over the SAME deterministic scenes and ray batches the headless
/// Tests.Core suite uses (shared TestSceneBuilder). Both engines get byte-identical geometry
/// (built from one PrimitiveSpec list) and the identical ray batch.
///
/// Usage: drop this on an empty GameObject in a scene and enter Play mode (runs on Start by
/// default), or press "Run" in the on-screen panel / right-click component > Run Benchmark.
/// Results print to the Console and render as a table via OnGUI.
///
/// This is a dev/profiling tool. It spawns/destroys collider GameObjects at runtime, so run it
/// in an empty scene, not your gameplay scene.
/// </summary>
[AddComponentMenu("Custom Raycast System/Raycast Benchmark (Unity vs Custom)")]
public class RaycastBenchmark : MonoBehaviour
{
    [Header("Workload (same recipe as Tests.Core / TestSceneBuilder)")]
    [Tooltip("Primitive counts to sweep. Each is a fresh seeded scene of mixed primitives.")]
    public int[] primitiveCounts = { 100, 1000, 10000 };

    [Tooltip("Include capsules (Box+Sphere+Capsule). Off = Box+Sphere only, like the README 'mixed' row.")]
    public bool includeCapsules = false;

    [Tooltip("Use the QuadTree acceleration structure for the custom core (matches the shipped perf numbers).")]
    public bool useQuadTree = true;

    [Header("Rays")]
    public int rayCount = 20000;
    public float maxRayDistance = 1000f;

    [Tooltip("Timed repetitions per engine; the fastest run is reported (least noise).")]
    public int repetitions = 3;

    [Tooltip("Untimed warmup rays per engine before measuring (JIT + lazy-structure warmup).")]
    public int warmupRays = 4000;

    [Header("Determinism")]
    public int sceneSeed = 20260705;
    public int raySeed = 6060842;

    [Tooltip("World half-size for the reference count; other counts scale as n^(1/3) to hold density ~constant.")]
    public float referenceWorldHalfSize = 40f;
    public int referenceCount = 1000;

    [Header("Run")]
    public bool runOnStart = true;

    public struct Row
    {
        public int count;
        public double customRaysPerSec;
        public double unityRaysPerSec;
        public int customHits;
        public int unityHits;
        public float agreementPct;
        public float meanDistDelta;
    }

    private readonly List<Row> _rows = new List<Row>();
    private bool _running;
    private string _status = "Idle - press Run.";

    private void Start()
    {
        if (runOnStart) RunBenchmark();
    }

    [ContextMenu("Run Benchmark")]
    public void RunBenchmark()
    {
        if (_running) return;
        StartCoroutine(RunAll());
    }

    // Scale the world so primitive density stays roughly constant across the sweep, otherwise
    // large scenes get pathologically dense and every ray hits immediately.
    private float WorldHalfSizeFor(int count)
        => referenceWorldHalfSize * Mathf.Pow((float)count / Mathf.Max(1, referenceCount), 1f / 3f);

    private IEnumerator RunAll()
    {
        _running = true;
        _rows.Clear();
        Debug.Log($"[RaycastBenchmark] start: {rayCount} rays/batch, {repetitions} reps, " +
                  $"capsules={includeCapsules}, quadTree={useQuadTree}");

        foreach (int count in primitiveCounts)
        {
            _status = $"Building {count} primitives...";
            yield return null;

            float range = WorldHalfSizeFor(count);
            List<PrimitiveSpec> specs = TestSceneBuilder.BuildSpecs(sceneSeed, count, range, includeCapsules);

            // --- Custom core: same primitives, same order/ids as Tests.Core ---
            CustomRaycastSystemCore core = TestSceneBuilder.NewSystem(useQuadTree);
            for (int i = 0; i < specs.Count; i++) TestSceneBuilder.AddSpec(core, specs[i]);

            // --- Matching Unity Physics scene: one static collider per spec ---
            var root = new GameObject($"__bench_{count}") { hideFlags = HideFlags.DontSave };
            BuildUnityColliders(root.transform, specs);
            Physics.SyncTransforms();
            yield return null; // let PhysX register the static colliders

            // --- One ray batch, shared by both engines (RandVec/RandDir, like Tests.Core) ---
            var origins = new Vector3[rayCount];
            var dirs = new Vector3[rayCount];
            var rr = new System.Random(raySeed);
            for (int i = 0; i < rayCount; i++)
            {
                origins[i] = TestSceneBuilder.RandVec(rr, range);
                dirs[i] = ((Vector3)TestSceneBuilder.RandDir(rr)).normalized;
            }

            _status = $"Measuring {count} primitives...";
            yield return null;

            // Warmup (untimed) - triggers any lazy structure build + JIT on both paths.
            int warm = Mathf.Min(warmupRays, rayCount);
            double sink = 0;
            for (int i = 0; i < warm; i++)
            {
                if (core.Raycast(new CRay(origins[i], dirs[i], maxRayDistance), out CHitInfo hc, maxRayDistance)) sink += hc.Distance;
                if (Physics.Raycast(origins[i], dirs[i], out RaycastHit hu, maxRayDistance)) sink += hu.distance;
            }

            var sw = new System.Diagnostics.Stopwatch();

            // --- Timed: custom engine ---
            double bestCustomMs = double.MaxValue;
            int customHits = 0;
            for (int rep = 0; rep < Mathf.Max(1, repetitions); rep++)
            {
                int hits = 0; double cs = 0;
                sw.Restart();
                for (int i = 0; i < rayCount; i++)
                    if (core.Raycast(new CRay(origins[i], dirs[i], maxRayDistance), out CHitInfo h, maxRayDistance)) { hits++; cs += h.Distance; }
                sw.Stop();
                bestCustomMs = System.Math.Min(bestCustomMs, sw.Elapsed.TotalMilliseconds);
                customHits = hits; sink += cs;
            }

            // --- Timed: Unity Physics ---
            double bestUnityMs = double.MaxValue;
            int unityHits = 0;
            for (int rep = 0; rep < Mathf.Max(1, repetitions); rep++)
            {
                int hits = 0; double cs = 0;
                sw.Restart();
                for (int i = 0; i < rayCount; i++)
                    if (Physics.Raycast(origins[i], dirs[i], out RaycastHit h, maxRayDistance)) { hits++; cs += h.distance; }
                sw.Stop();
                bestUnityMs = System.Math.Min(bestUnityMs, sw.Elapsed.TotalMilliseconds);
                unityHits = hits; sink += cs;
            }

            // --- Correctness cross-check (untimed): hit/miss agreement + distance delta ---
            // Not a strict oracle: PhysX and the custom engine differ on grazing rays and on rays
            // that START inside a collider (Unity reports miss, the custom engine may report a hit).
            int agree = 0, mutual = 0; double distErr = 0;
            for (int i = 0; i < rayCount; i++)
            {
                bool c = core.Raycast(new CRay(origins[i], dirs[i], maxRayDistance), out CHitInfo hc, maxRayDistance);
                bool u = Physics.Raycast(origins[i], dirs[i], out RaycastHit hu, maxRayDistance);
                if (c == u) agree++;
                if (c && u) { distErr += Mathf.Abs(hc.Distance - hu.distance); mutual++; }
            }

            var row = new Row
            {
                count = count,
                customRaysPerSec = rayCount / (bestCustomMs / 1000.0),
                unityRaysPerSec = rayCount / (bestUnityMs / 1000.0),
                customHits = customHits,
                unityHits = unityHits,
                agreementPct = 100f * agree / rayCount,
                meanDistDelta = mutual > 0 ? (float)(distErr / mutual) : 0f
            };
            _rows.Add(row);

            double speedup = row.customRaysPerSec / System.Math.Max(1.0, row.unityRaysPerSec);
            Debug.Log($"[RaycastBenchmark] n={count,6} | custom {row.customRaysPerSec,12:N0} rays/s | " +
                      $"unity {row.unityRaysPerSec,12:N0} rays/s | {speedup,5:0.00}x | " +
                      $"hits c/u {customHits}/{unityHits} | agree {row.agreementPct:0.0}% | " +
                      $"dDelta {row.meanDistDelta:0.0000} | (sink {sink:0.0})");

            Destroy(root);
            yield return null;
        }

        _status = "Done.";
        _running = false;
        Debug.Log("[RaycastBenchmark] done.");
    }

    private static void BuildUnityColliders(Transform parent, List<PrimitiveSpec> specs)
    {
        for (int i = 0; i < specs.Count; i++)
        {
            PrimitiveSpec s = specs[i];
            var go = new GameObject("p" + i) { hideFlags = HideFlags.DontSave };
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.position = s.Position;
            t.rotation = s.Rotation;
            t.localScale = Vector3.one; // custom core is scale-free: collider dims == world dims

            switch (s.Kind)
            {
                case PrimKind.Sphere:
                    go.AddComponent<SphereCollider>().radius = s.Radius;
                    break;
                case PrimKind.Capsule:
                    var cc = go.AddComponent<CapsuleCollider>();
                    cc.radius = s.Radius;
                    cc.height = s.Height; // full height incl. caps == CapsulePrimitive.Size.y
                    cc.direction = 1;     // local +Y, matching CapsulePrimitive
                    break;
                default:
                    go.AddComponent<BoxCollider>().size = s.Size; // BoxPrimitive.Size is full world dims
                    break;
            }
        }
    }

    private void OnGUI()
    {
        float h = 40f + 20f * (_rows.Count + 3);
        GUILayout.BeginArea(new Rect(10, 10, 640, h), GUI.skin.box);
        GUILayout.Label($"Raycast Benchmark - Unity Physics vs Custom   ({_status})");
        GUILayout.Label($"{"n",7} | {"custom rays/s",14} | {"unity rays/s",14} | {"speedup",8} | {"agree",6}");
        foreach (var r in _rows)
        {
            double x = r.customRaysPerSec / System.Math.Max(1.0, r.unityRaysPerSec);
            GUILayout.Label($"{r.count,7} | {r.customRaysPerSec,14:N0} | {r.unityRaysPerSec,14:N0} | {x,6:0.00}x | {r.agreementPct,5:0.0}%");
        }
        if (!_running && GUILayout.Button("Run")) RunBenchmark();
        GUILayout.EndArea();
    }
}
#endif
