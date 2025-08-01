#if UNITY_5_3_OR_NEWER
using UVector3 = UnityEngine.Vector3;
using UQuaternion = UnityEngine.Quaternion;
using UMatrix4x4 = UnityEngine.Matrix4x4;
using UMathf = UnityEngine.Mathf;
using UDebug = UnityEngine.Debug;
using System.Collections.Generic;
using UnityEngine;

// Unity MonoBehaviour Singleton
public class CustomRaycastSystem : MonoBehaviour
{
    private static CustomRaycastSystem _instance;
    public static CustomRaycastSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<CustomRaycastSystem>();
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("CustomRaycastSystemSingleton");
                    _instance = singletonObject.AddComponent<CustomRaycastSystem>();
                    DontDestroyOnLoad(singletonObject);
                }
            }
            return _instance;
        }
    }

    [Header("Quad-Tree Settings")]
    [SerializeField] private bool _useQuadTree = true;
    [SerializeField] private Vector3 _quadTreeCenter = Vector3.zero;
    [SerializeField] private Vector3 _quadTreeSize = new Vector3(1000, 1000, 1000);
    [SerializeField] private int _quadTreeCapacity = 4;

    private CustomRaycastSystemCore _coreSystem;
    private Dictionary<int, GameObject> _primitiveIDToGameObjectMap = new Dictionary<int, GameObject>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _coreSystem = new CustomRaycastSystemCore(_useQuadTree, _quadTreeCenter, _quadTreeSize, _quadTreeCapacity);
    }

    public IPrimitive RegisterPrimitive(IPrimitive primitive, GameObject gameObject)
    {
        IPrimitive registeredPrimitive = _coreSystem.AddPrimitive(primitive);
        _primitiveIDToGameObjectMap[registeredPrimitive.ID] = gameObject;
        return registeredPrimitive;
    }

    public void UnregisterPrimitive(int primitiveId)
    {
        _coreSystem.RemovePrimitive(primitiveId);
        _primitiveIDToGameObjectMap.Remove(primitiveId);
    }

    public void UpdatePrimitive(int primitiveId, UVector3 newPosition, UQuaternion newRotation, UVector3 newSize)
    {
        _coreSystem.UpdatePrimitive(primitiveId, newPosition, newRotation, newSize);
    }

    public bool Raycast(Ray unityRay, out CustomHitInfo hitInfo, float maxDistance = Mathf.Infinity)
    {
        CRay cRay = new CRay(unityRay.origin, unityRay.direction, maxDistance);
        if (_coreSystem.Raycast(cRay, out CHitInfo cHitInfo, maxDistance))
        {
            hitInfo = new CustomHitInfo(cHitInfo, _primitiveIDToGameObjectMap.TryGetValue(cHitInfo.PrimitiveID, out GameObject go) ? go : null);
            return true;
        }
        hitInfo = default;
        return false;
    }

    public List<CustomHitInfo> RaycastAll(Ray unityRay, float maxDistance = Mathf.Infinity, bool sortByDistance = true)
    {
        CRay cRay = new CRay(unityRay.origin, unityRay.direction, maxDistance);
        List<CHitInfo> cHitInfos = _coreSystem.RaycastAll(cRay, maxDistance, sortByDistance);
        List<CustomHitInfo> customHitInfos = new List<CustomHitInfo>();

        foreach (var cHitInfo in cHitInfos)
        {
            customHitInfos.Add(new CustomHitInfo(cHitInfo, _primitiveIDToGameObjectMap.TryGetValue(cHitInfo.PrimitiveID, out GameObject go) ? go : null));
        }
        return customHitInfos;
    }
}
#endif
