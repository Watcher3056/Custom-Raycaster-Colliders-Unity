#if UNITY_5_3_OR_NEWER
using UnityEngine;

[AddComponentMenu("Custom Colliders/Custom Box Collider")]
public class CustomBoxCollider : MonoBehaviour
{
    [SerializeField] private Vector3 _size = Vector3.one;

    private BoxPrimitive _primitive;

    public Vector3 Size
    {
        get => _size;
        set
        {
            _size = value;
            if (_primitive != null) UpdatePrimitiveInSystem();
        }
    }

    private void OnEnable()
    {
        _primitive = new BoxPrimitive(-1, transform.position, transform.rotation, Vector3.Scale(_size, transform.lossyScale));
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
            CustomRaycastSystem.Instance.UpdatePrimitive(_primitive.ID, transform.position, transform.rotation, Vector3.Scale(_size, transform.lossyScale));
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_primitive != null)
        {
            Gizmos.color = Color.blue;
            // Apply the object's full transform (position, rotation, scale) to the Gizmos matrix
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.DrawWireCube(Vector3.zero, _size); // Draw a unit cube at local origin
            Gizmos.matrix = Matrix4x4.identity; // Reset Gizmos matrix
        }
    }
}
#endif
