#if UNITY_5_3_OR_NEWER
using UnityEngine;

[AddComponentMenu("Custom Colliders/Custom Sphere Collider")]
public class CustomSphereCollider : MonoBehaviour
{
    [SerializeField] private float _radius = 0.5f;

    private SpherePrimitive _primitive;

    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            if (_primitive != null) UpdatePrimitiveInSystem();
        }
    }

    private void OnEnable()
    {
        _primitive = new SpherePrimitive(-1, transform.position, _radius * transform.lossyScale.x);
        CustomRaycastSystem.Instance.RegisterPrimitive(_primitive, gameObject);
    }

    private void OnDisable()
    {
        if (_primitive != null)
        {
            CustomRaycastSystem.Instance.UnregisterPrimitive(_primitive.ID);
        }
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            UpdatePrimitiveInSystem();
            transform.hasChanged = false;
        }
    }

    private void UpdatePrimitiveInSystem()
    {
        if (_primitive != null)
        {
            // Pass current transform position, rotation, and scale to the core system
            CustomRaycastSystem.Instance.UpdatePrimitive(_primitive.ID, transform.position, transform.rotation, _radius * transform.lossyScale);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_primitive != null)
        {
            Gizmos.color = Color.green;
            // Apply the object's full transform (position, rotation, scale) to the Gizmos matrix
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.DrawWireSphere(Vector3.zero, _radius); // Draw a unit sphere at local origin
            Gizmos.matrix = Matrix4x4.identity; // Reset Gizmos matrix
        }
    }
}
#endif
