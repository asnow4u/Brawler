using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
internal class HitBox : MonoBehaviour, IHitBox
{
    private Collider collider;

    public event Action<IHurtBox, Vector3> OnCollisionEntered;

    private void Awake()
    {
        collider = GetComponent<Collider>();
        DeactivateHitBox();
    }

    public void ActivateHitBox()
    {
        collider.enabled = true;
    }

    public void DeactivateHitBox()
    {
        collider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out IHurtBox hurtBox))
        {
            Vector3 hitPoint = other.ClosestPoint(transform.position);

            OnCollisionEntered?.Invoke(hurtBox, hitPoint);
        }
    }

    #region Gizmos

    private void OnDrawGizmos()
    {
        var hitboxCollider = collider ? collider : GetComponent<Collider>();
        if (!hitboxCollider)
        {
            return;
        }

        bool isActive = enabled && hitboxCollider.enabled && hitboxCollider.gameObject.activeInHierarchy;
        Gizmos.color = isActive ? Color.red : Color.white;
#if UNITY_EDITOR
        Handles.color = Gizmos.color;
#endif

        Matrix4x4 originalMatrix = Gizmos.matrix;
        try
        {
            switch (hitboxCollider)
            {
                case BoxCollider boxCollider:
                    DrawBoxColliderGizmo(boxCollider);
                    break;
                case SphereCollider sphereCollider:
                    DrawSphereColliderGizmo(sphereCollider);
                    break;
                case CapsuleCollider capsuleCollider:
#if UNITY_EDITOR
                    DrawCapsuleColliderGizmo(capsuleCollider);
#else
                    DrawBoundsFallback(hitboxCollider);
#endif
                    break;
                default:
                    DrawBoundsFallback(hitboxCollider);
                    break;
            }
        }
        finally
        {
            Gizmos.matrix = originalMatrix;
        }
    }

    private static void DrawBoxColliderGizmo(BoxCollider boxCollider)
    {
        Transform t = boxCollider.transform;
        Vector3 centerWorld = t.TransformPoint(boxCollider.center);

        Gizmos.matrix = Matrix4x4.TRS(centerWorld, t.rotation, t.lossyScale);
        Gizmos.DrawWireCube(Vector3.zero, boxCollider.size);
    }

    private static void DrawSphereColliderGizmo(SphereCollider sphereCollider)
    {
        Transform t = sphereCollider.transform;
        Vector3 centerWorld = t.TransformPoint(sphereCollider.center);

        Vector3 lossyScale = t.lossyScale;
        float uniformScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));

        Gizmos.matrix = Matrix4x4.TRS(centerWorld, t.rotation, Vector3.one * uniformScale);
        Gizmos.DrawWireSphere(Vector3.zero, sphereCollider.radius);
    }

#if UNITY_EDITOR
    private static void DrawCapsuleColliderGizmo(CapsuleCollider capsuleCollider)
    {
        Transform t = capsuleCollider.transform;
        Vector3 centerWorld = t.TransformPoint(capsuleCollider.center);

        Vector3 lossyScale = t.lossyScale;
        float scaleX = Mathf.Abs(lossyScale.x);
        float scaleY = Mathf.Abs(lossyScale.y);
        float scaleZ = Mathf.Abs(lossyScale.z);

        Vector3 axis;
        float radiusScale;
        float heightScale;
        switch (capsuleCollider.direction)
        {
            case 0:
                axis = t.right;
                radiusScale = Mathf.Max(scaleY, scaleZ);
                heightScale = scaleX;
                break;
            case 2:
                axis = t.forward;
                radiusScale = Mathf.Max(scaleX, scaleY);
                heightScale = scaleZ;
                break;
            default:
                axis = t.up;
                radiusScale = Mathf.Max(scaleX, scaleZ);
                heightScale = scaleY;
                break;
        }

        float radius = capsuleCollider.radius * radiusScale;
        float height = Mathf.Max(capsuleCollider.height * heightScale, radius * 2f);
        float cylinderLength = Mathf.Max(0f, height - radius * 2f);

        Vector3 up = axis.normalized;
        if (up.sqrMagnitude < 1e-6f)
        {
            up = Vector3.up;
        }

        Vector3 tangent = Mathf.Abs(Vector3.Dot(up, Vector3.up)) > 0.99f
            ? Vector3.Cross(up, Vector3.right)
            : Vector3.Cross(up, Vector3.up);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(up, tangent).normalized;

        if (cylinderLength <= 1e-4f)
        {
            Handles.DrawWireDisc(centerWorld, up, radius);
            Handles.DrawWireDisc(centerWorld, tangent, radius);
            Handles.DrawWireDisc(centerWorld, bitangent, radius);
            return;
        }

        Vector3 top = centerWorld + up * (cylinderLength * 0.5f);
        Vector3 bottom = centerWorld - up * (cylinderLength * 0.5f);

        Handles.DrawWireDisc(top, up, radius);
        Handles.DrawWireDisc(bottom, up, radius);

        Handles.DrawLine(top + tangent * radius, bottom + tangent * radius);
        Handles.DrawLine(top - tangent * radius, bottom - tangent * radius);
        Handles.DrawLine(top + bitangent * radius, bottom + bitangent * radius);
        Handles.DrawLine(top - bitangent * radius, bottom - bitangent * radius);

        Handles.DrawWireArc(top, tangent, bitangent, 180f, radius);
        Handles.DrawWireArc(top, bitangent, -tangent, 180f, radius);
        Handles.DrawWireArc(bottom, tangent, -bitangent, 180f, radius);
        Handles.DrawWireArc(bottom, bitangent, tangent, 180f, radius);
    }
#endif

    private static void DrawBoundsFallback(Collider c)
    {
        Bounds b = c.bounds;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.DrawWireCube(b.center, b.size);
    }

    #endregion
}
