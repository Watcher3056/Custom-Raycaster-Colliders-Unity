#if UNITY_5_3_OR_NEWER
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class AutoTest : MonoBehaviour
{
    [Header("Object Generation Settings")]
    [SerializeField] private int _numSphereObjects = 50;
    [SerializeField] private bool _testSpheres = true;
    [SerializeField] private int _numBoxObjects = 50;
    [SerializeField] private bool _testCubes = true;
    [SerializeField] private float _spawnRadius = 50f;
    [SerializeField] private float _minPrimitiveSize = 1f;
    [SerializeField] private float _maxPrimitiveSize = 5f;
    [SerializeField] private float _objectMoveSpeed = 5f;
    [SerializeField] private bool _testMovement = true;
    [SerializeField] private bool _testScale = true;
    [SerializeField] private bool _testRotation = true;

    [Header("Raycast Test Settings")]
    [SerializeField] private int _numRaysPerFrame = 100;
    [SerializeField] private float _rayLength = 100f;
    [SerializeField] private bool _testCustomRaycast = true;
    [SerializeField] private bool _testUnityRaycast = true;
    [SerializeField] private bool _compareRaycastSingleHit = true;
    [SerializeField] private bool _compareRaycastAll = false;

    [Header("Comparison Settings")]
    [SerializeField] private float _positionErrorEpsilon = 0.002f;
    [SerializeField] private float _distanceErrorEpsilon = 0.002f;
    [SerializeField] private float _normalErrorEpsilon = 0.01f;
    [SerializeField] private bool _stopOnMismatch = true;

    [Header("Mismatch Visualization")]
    [SerializeField] private Color _colorHitBoth = Color.green;
    [SerializeField] private Color _colorHitCustomOnly = Color.yellow;
    [SerializeField] private Color _colorHitUnityOnly = Color.blue;
    [SerializeField] private Color _colorNoHit = Color.gray; // Color for objects not hit by either, or not part of mismatch

    private List<GameObject> _testObjects = new List<GameObject>();
    private List<Rigidbody> _testObjectRigidbodies = new List<Rigidbody>();
    private List<Vector3> _objectMoveDirections = new List<Vector3>();
    private List<Vector3> _objectRotateAxes = new List<Vector3>();

    private Stopwatch _customSystemStopwatch = new Stopwatch();
    private Stopwatch _unitySystemStopwatch = new Stopwatch();

    private int _frameCounter = 0;
    private int _mismatchCount = 0;

    private GameObject _testObjectsParent; // To hold all generated objects

    private void Awake()
    {
        CustomRaycastSystem.Instance.gameObject.name = "CustomRaycastSystemSingleton";
    }

    private void Start()
    {
        GenerateTestObjects();
    }

    private void OnDisable()
    {
        // Do NOT destroy objects or clear _testObjects collection here.
        // Objects remain in scene for inspection after test stops.
        _testObjectRigidbodies.Clear();
        _objectMoveDirections.Clear();
        _objectRotateAxes.Clear();
    }

    private void GenerateTestObjects()
    {
        // Clean up previous test run's objects if they exist
        if (_testObjectsParent != null)
        {
            Destroy(_testObjectsParent);
        }

        _testObjects.Clear();
        _testObjectRigidbodies.Clear();
        _objectMoveDirections.Clear();
        _objectRotateAxes.Clear();

        _testObjectsParent = new GameObject("TestObjects");
        _testObjectsParent.transform.position = transform.position;

        if (_testSpheres)
        {
            for (int i = 0; i < _numSphereObjects; i++)
            {
                GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphereObj.transform.parent = _testObjectsParent.transform;
                sphereObj.name = $"CustomSphere_{i}";
                sphereObj.transform.position = transform.position + Random.insideUnitSphere * _spawnRadius;

                float radius = Random.Range(_minPrimitiveSize, _maxPrimitiveSize);
                if (_testScale)
                {
                    sphereObj.transform.localScale = Vector3.one * (radius * 2f);
                }
                else
                {
                    sphereObj.transform.localScale = Vector3.one * 2f;
                    radius = 1f;
                }

                if (_testRotation)
                {
                    sphereObj.transform.rotation = Random.rotation;
                }
                else
                {
                    sphereObj.transform.rotation = Quaternion.identity;
                }

                sphereObj.AddComponent<CustomSphereCollider>().Radius = radius / 2f;
                sphereObj.GetComponent<SphereCollider>().radius = radius / 2f;
                AddRigidbodyAndMovement(sphereObj);
            }
        }

        if (_testCubes)
        {
            for (int i = 0; i < _numBoxObjects; i++)
            {
                GameObject boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boxObj.transform.parent = _testObjectsParent.transform;
                boxObj.name = $"CustomBox_{i}";
                boxObj.transform.position = transform.position + Random.insideUnitSphere * _spawnRadius;

                if (_testRotation)
                {
                    boxObj.transform.rotation = Random.rotation;
                }
                else
                {
                    boxObj.transform.rotation = Quaternion.identity;
                }

                Vector3 size;
                if (_testScale)
                {
                    size = new Vector3(
                        Random.Range(_minPrimitiveSize, _maxPrimitiveSize),
                        Random.Range(_minPrimitiveSize, _maxPrimitiveSize),
                        Random.Range(_minPrimitiveSize, _maxPrimitiveSize)
                    );
                }
                else
                {
                    size = Vector3.one;
                }
                boxObj.transform.localScale = size;
                boxObj.AddComponent<CustomBoxCollider>().Size = size;
                boxObj.GetComponent<BoxCollider>().size = size;
                AddRigidbodyAndMovement(boxObj);
            }
        }

        UnityEngine.Debug.Log($"Generated {(_testSpheres ? _numSphereObjects : 0)} spheres and {(_testCubes ? _numBoxObjects : 0)} boxes for testing.");
    }

    private void AddRigidbodyAndMovement(GameObject obj)
    {
        Rigidbody rb = obj.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        _testObjects.Add(obj);
        _testObjectRigidbodies.Add(rb);
        _objectMoveDirections.Add(Random.onUnitSphere);
        _objectRotateAxes.Add(Random.onUnitSphere);
    }

    private void FixedUpdate()
    {
        _frameCounter++;
        if (_testMovement)
        {
            MoveObjects();
        }
        PerformRaycastsAndCompare();
    }

    private void MoveObjects()
    {
        for (int i = 0; i < _testObjects.Count; i++)
        {
            GameObject obj = _testObjects[i];
            if (obj == null) continue; // Skip if object was destroyed (e.g., manually)

            Vector3 currentPos = obj.transform.position;
            Vector3 newPos = currentPos + _objectMoveDirections[i] * _objectMoveSpeed * Time.fixedDeltaTime;

            if (Vector3.Distance(newPos, transform.position) > _spawnRadius)
            {
                _objectMoveDirections[i] = (transform.position - newPos).normalized;
                newPos = currentPos + _objectMoveDirections[i] * _objectMoveSpeed * Time.fixedDeltaTime;
            }
            obj.transform.position = newPos;

            if (_testRotation)
            {
                obj.transform.Rotate(_objectRotateAxes[i] * Time.fixedDeltaTime * 100f);
            }
        }
    }

    private void PerformRaycastsAndCompare()
    {
        List<Ray> rays = GenerateRandomRays(_numRaysPerFrame, transform.position, _spawnRadius + _rayLength);

        List<CustomHitInfo> customHitsSingle = new List<CustomHitInfo>();
        List<CustomHitInfo> customHitsAll = new List<CustomHitInfo>();
        List<RaycastHit> unityHitsSingle = new List<RaycastHit>();
        List<RaycastHit> unityHitsAll = new List<RaycastHit>();

        _customSystemStopwatch.Restart();
        if (_testCustomRaycast)
        {
            foreach (var ray in rays)
            {
                if (_compareRaycastSingleHit)
                {
                    if (CustomRaycastSystem.Instance.Raycast(ray, out CustomHitInfo hit, _rayLength))
                    {
                        customHitsSingle.Add(hit);
                    }
                }
                if (_compareRaycastAll)
                {
                    customHitsAll.AddRange(CustomRaycastSystem.Instance.RaycastAll(ray, _rayLength, false));
                }
            }
        }
        _customSystemStopwatch.Stop();

        _unitySystemStopwatch.Restart();
        if (_testUnityRaycast)
        {
            foreach (var ray in rays)
            {
                if (_compareRaycastSingleHit)
                {
                    if (Physics.Raycast(ray, out RaycastHit hit, _rayLength))
                    {
                        unityHitsSingle.Add(hit);
                    }
                }
                if (_compareRaycastAll)
                {
                    unityHitsAll.AddRange(Physics.RaycastAll(ray, _rayLength));
                }
            }
        }
        _unitySystemStopwatch.Stop();

        // Consolidate all hits for coloring logic
        HashSet<GameObject> allCustomHitGameObjects = new HashSet<GameObject>();
        foreach (var hit in customHitsSingle) allCustomHitGameObjects.Add(hit.HitGameObject);
        foreach (var hit in customHitsAll) allCustomHitGameObjects.Add(hit.HitGameObject);

        HashSet<GameObject> allUnityHitGameObjects = new HashSet<GameObject>();
        foreach (var hit in unityHitsSingle) allUnityHitGameObjects.Add(hit.collider?.gameObject);
        foreach (var hit in unityHitsAll) allUnityHitGameObjects.Add(hit.collider?.gameObject);


        if (_testCustomRaycast && _testUnityRaycast)
        {
            bool mismatchFound = false;
            if (_compareRaycastSingleHit)
            {
                if (!CompareSingleHits(customHitsSingle, unityHitsSingle)) mismatchFound = true;
            }
            if (_compareRaycastAll)
            {
                if (!CompareAllHits(customHitsAll, unityHitsAll)) mismatchFound = true;
            }

            if (mismatchFound)
            {
                LogMismatch($"Mismatch detected in frame {_frameCounter}.", allCustomHitGameObjects, allUnityHitGameObjects);
            }
        }

        UnityEngine.Debug.Log($"Frame {_frameCounter}: Custom Raycast Time: {_customSystemStopwatch.Elapsed.TotalMilliseconds:F4} ms, Unity Raycast Time: {_unitySystemStopwatch.Elapsed.TotalMilliseconds:F4} ms");
    }

    private List<Ray> GenerateRandomRays(int count, Vector3 center, float maxDistance)
    {
        List<Ray> rays = new List<Ray>();
        for (int i = 0; i < count; i++)
        {
            Vector3 origin = center + Random.insideUnitSphere * (_spawnRadius + 5f);
            Vector3 direction = (center + Random.insideUnitSphere * _spawnRadius - origin).normalized;
            rays.Add(new Ray(origin, direction));
        }
        return rays;
    }

    private bool CompareSingleHits(List<CustomHitInfo> customHits, List<RaycastHit> unityHits)
    {
        if (customHits.Count != unityHits.Count)
        {
            UnityEngine.Debug.LogError($"Single Hit Count Mismatch! Custom: {customHits.Count}, Unity: {unityHits.Count}");
            return false;
        }

        for (int i = 0; i < customHits.Count; i++)
        {
            CustomHitInfo customHit = customHits[i];
            RaycastHit unityHit = unityHits[i];

            bool gameObjectsMatch = (customHit.HitGameObject != null && unityHit.collider != null && customHit.HitGameObject.name == unityHit.collider.gameObject.name) ||
                                    (customHit.HitGameObject == null && unityHit.collider == null);

            if (!gameObjectsMatch)
            {
                UnityEngine.Debug.LogError($"GameObject Mismatch! Custom: {customHit.HitGameObject?.name ?? "NULL"}, Unity: {unityHit.collider?.gameObject.name ?? "NULL"}");
                return false;
            }

            if (Vector3.Distance(customHit.HitPoint, unityHit.point) > _positionErrorEpsilon)
            {
                UnityEngine.Debug.LogError($"HitPoint Mismatch for {customHit.HitGameObject?.name}! Custom: {customHit.HitPoint}, Unity: {unityHit.point}");
                return false;
            }

            if (Mathf.Abs(customHit.Distance - unityHit.distance) > _distanceErrorEpsilon)
            {
                UnityEngine.Debug.LogError($"Distance Mismatch for {customHit.HitGameObject?.name}! Custom: {customHit.Distance}, Unity: {unityHit.distance}");
                return false;
            }

            if (Vector3.Distance(customHit.Normal, unityHit.normal) > _normalErrorEpsilon)
            {
                UnityEngine.Debug.LogError($"Normal Mismatch for {customHit.HitGameObject?.name}! Custom: {customHit.Normal}, Unity: {unityHit.normal}");
                return false;
            }
        }
        return true;
    }

    private bool CompareAllHits(List<CustomHitInfo> customHits, List<RaycastHit> unityHits)
    {
        if (Mathf.Abs(customHits.Count - unityHits.Count) > 0)
        {
            UnityEngine.Debug.LogError($"RaycastAll Hit Count Mismatch! Custom: {customHits.Count}, Unity: {unityHits.Count}. Note: This is a simplified comparison for RaycastAll.");
            return false;
        }

        HashSet<string> customHitNames = new HashSet<string>(customHits.Select(h => h.HitGameObject?.name ?? "NULL"));
        HashSet<string> unityHitNames = new HashSet<string>(unityHits.Select(h => h.collider?.gameObject.name ?? "NULL"));

        if (!customHitNames.SetEquals(unityHitNames))
        {
            UnityEngine.Debug.LogError($"RaycastAll Hit Object Set Mismatch! Custom: [{string.Join(", ", customHitNames)}], Unity: [{string.Join(", ", unityHitNames)}]");
            return false;
        }
        return true;
    }

    private void LogMismatch(string message, HashSet<GameObject> customHitObjects, HashSet<GameObject> unityHitObjects)
    {
        _mismatchCount++;
        UnityEngine.Debug.LogError($"Raycast Mismatch Detected (Frame {_frameCounter}, Mismatch Count: {_mismatchCount}): {message}");

        // Apply color highlighting to objects
        foreach (var obj in _testObjects)
        {
            if (obj == null) continue;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) continue;

            bool hitCustom = customHitObjects.Contains(obj);
            bool hitUnity = unityHitObjects.Contains(obj);

            if (hitCustom && hitUnity)
            {
                renderer.material.color = _colorHitBoth;
            }
            else if (hitCustom && !hitUnity)
            {
                renderer.material.color = _colorHitCustomOnly;
            }
            else if (!hitCustom && hitUnity)
            {
                renderer.material.color = _colorHitUnityOnly;
            }
            else
            {
                // For objects that were not hit by either system in this specific mismatching frame,
                // or were not part of the rays that caused the mismatch, keep them gray.
                renderer.material.color = _colorNoHit;
            }
        }

        if (_stopOnMismatch)
        {
            UnityEngine.Debug.LogError("Stopping AutoTest due to mismatch.");
            this.enabled = false;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = true;
#endif
        }
    }

    private void OnDrawGizmos()
    {
        if (!this.enabled) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _spawnRadius);
    }
}
#endif
