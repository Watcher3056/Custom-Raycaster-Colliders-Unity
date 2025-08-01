#if UNITY_5_3_OR_NEWER
using UnityEngine;
using System.Collections.Generic;

public class InteractiveRaycastTest : MonoBehaviour
{
    [Header("Object Generation Settings")]
    [SerializeField] private int _numSpheres = 3;
    [SerializeField] private int _numBoxes = 3;
    [SerializeField] private float _spawnAreaSize = 20f; // Objects spawned within a cube of this size
    [SerializeField] private float _primitiveSize = 2f; // Fixed size for all primitives
    [SerializeField] private float _objectMoveSpeed = 3f; // Speed of object movement
    [SerializeField] private float _moveDistance = 5f; // How far objects move back and forth

    [Header("Raycast Visualization Colors")]
    [SerializeField] private Color _hitColor = Color.green;
    [SerializeField] private Color _defaultColor = Color.white;

    private List<GameObject> _spawnedObjects = new List<GameObject>();
    private List<Vector3> _initialPositions = new List<Vector3>();
    private List<Vector3> _moveDirections = new List<Vector3>(); // For back and forth movement

    private GameObject _lastHitObject = null;
    private Renderer _lastHitRenderer = null;

    private void Awake()
    {
        // Ensure CustomRaycastSystem singleton is initialized
        CustomRaycastSystem.Instance.gameObject.name = "CustomRaycastSystemSingleton";
    }

    private void Start()
    {
        GenerateObjects();
    }

    private void OnDisable()
    {
        // Clean up generated objects when the script is disabled
        foreach (var obj in _spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        _spawnedObjects.Clear();
        _initialPositions.Clear();
        _moveDirections.Clear();
    }

    private void GenerateObjects()
    {
        // Clean up previous objects if any
        foreach (var obj in _spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedObjects.Clear();
        _initialPositions.Clear();
        _moveDirections.Clear();

        GameObject parent = new GameObject("InteractiveTestObjects");
        parent.transform.position = transform.position;

        // Generate Spheres
        for (int i = 0; i < _numSpheres; i++)
        {
            GameObject sphereObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereObj.transform.parent = parent.transform;
            sphereObj.name = $"InteractiveSphere_{i}";
            sphereObj.transform.position = transform.position + new Vector3(
                Random.Range(-_spawnAreaSize / 2, _spawnAreaSize / 2),
                Random.Range(-_spawnAreaSize / 2, _spawnAreaSize / 2),
                Random.Range(-_spawnAreaSize / 2, _spawnAreaSize / 2)
            );
            sphereObj.transform.localScale = Vector3.one * _primitiveSize / 2f;

            Renderer sphereRenderer = sphereObj.GetComponent<Renderer>();
            if (sphereRenderer != null)
            {
                sphereRenderer.material = new Material(Shader.Find("Unlit/Color"));
                sphereRenderer.material.color = _defaultColor;
            }

            sphereObj.AddComponent<CustomSphereCollider>().Radius = 0.5f;
            _spawnedObjects.Add(sphereObj);
            _initialPositions.Add(sphereObj.transform.position);
            _moveDirections.Add(Random.onUnitSphere); // Random initial direction for movement
        }

        // Generate Boxes
        for (int i = 0; i < _numBoxes; i++)
        {
            GameObject boxObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObj.transform.parent = parent.transform;
            boxObj.name = $"InteractiveBox_{i}";
            boxObj.transform.position = transform.position + new Vector3(
                Random.Range(-_spawnAreaSize / 2, _spawnAreaSize / 2),
                Random.Range(-_spawnAreaSize / 2, _spawnAreaSize / 2),
                Random.Range(-_spawnAreaSize / 2, _spawnAreaSize / 2)
            );
            boxObj.transform.localScale = Vector3.one * _primitiveSize;
            boxObj.transform.rotation = Random.rotation;

            Renderer boxRenderer = boxObj.GetComponent<Renderer>();
            if (boxRenderer != null)
            {
                boxRenderer.material = new Material(Shader.Find("Unlit/Color"));
                boxRenderer.material.color = _defaultColor;
            }

            boxObj.AddComponent<CustomBoxCollider>().Size = Vector3.one;
            _spawnedObjects.Add(boxObj);
            _initialPositions.Add(boxObj.transform.position);
            _moveDirections.Add(Random.onUnitSphere); // Random initial direction for movement
        }

        UnityEngine.Debug.Log($"Generated {_numSpheres} spheres and {_numBoxes} boxes for interactive test.");
    }

    private void Update()
    {
        MoveObjects();
        HandleMouseRaycast();
    }

    private void MoveObjects()
    {
        for (int i = 0; i < _spawnedObjects.Count; i++)
        {
            GameObject obj = _spawnedObjects[i];
            if (obj == null) continue;

            // Calculate movement based on a sine wave for back-and-forth motion
            float t = (Time.time * _objectMoveSpeed + i * 0.1f) % (2 * Mathf.PI); // Add offset for variety
            float moveFactor = Mathf.Sin(t); // Ranges from -1 to 1

            Vector3 targetPosition = _initialPositions[i] + _moveDirections[i] * moveFactor * _moveDistance;
            obj.transform.position = targetPosition;

            // Optional: add a subtle rotation
            obj.transform.Rotate(_moveDirections[i] * Time.deltaTime * 30f);
        }
    }

    private void HandleMouseRaycast()
    {
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        CustomHitInfo hitInfo;

        // Perform raycast using our custom system
        if (CustomRaycastSystem.Instance.Raycast(mouseRay, out hitInfo, 100f))
        {
            // If a new object is hit, reset the color of the previously hit object
            if (_lastHitObject != hitInfo.HitGameObject && _lastHitObject != null)
            {
                if (_lastHitRenderer != null)
                {
                    _lastHitRenderer.material.color = _defaultColor;
                }
            }

            // Set the current hit object's color to green
            _lastHitObject = hitInfo.HitGameObject;
            if (_lastHitObject != null)
            {
                _lastHitRenderer = _lastHitObject.GetComponent<Renderer>();
                if (_lastHitRenderer != null)
                {
                    _lastHitRenderer.material.color = _hitColor;
                }
            }
        }
        else // No object hit by the ray
        {
            // If there was a previously hit object, reset its color to white
            if (_lastHitObject != null)
            {
                if (_lastHitRenderer != null)
                {
                    _lastHitRenderer.material.color = _defaultColor;
                }
                _lastHitObject = null;
                _lastHitRenderer = null;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * _spawnAreaSize);
    }
}
#endif
