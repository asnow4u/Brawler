using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
internal class HitBox : MonoBehaviour, IHitBox
{
    private Collider col;

    // Used to prevent colliding against your own hurtboxs, ignored if null
    private Guid ownerID = default;

    private LayerMask collisionMask;

    private const int OverlapBufferSize = 16;
    private readonly Collider[] overlapBuffer = new Collider[OverlapBufferSize];
    private readonly RaycastHit[] castBuffer = new RaycastHit[OverlapBufferSize];

    private Vector3 prevPosition;
    private bool hasPrevPosition;

    // Track all hit sceneObjects while active
    private readonly HashSet<Guid> hurtBoxesHit = new HashSet<Guid>();

    public event Action<IHitBox, IHurtBox, Vector3> OnCollisionEntered;


    private void Awake()
    {
        gameObject.layer = LayerMask.NameToLayer("HitBox");
        collisionMask = LayerMask.GetMask("HurtBox");

        col = GetComponent<Collider>();
        col.isTrigger = true;

        DeactivateHitBox();
    }

    public void ActivateHitBox()
    {
        col.enabled = true;
        hurtBoxesHit.Clear();
        hasPrevPosition = false;
    }

    public void DeactivateHitBox()
    {
        col.enabled = false;
        hurtBoxesHit.Clear();
        hasPrevPosition = false;
    }

    public void SetOwner(Guid ownerID)
    {
        this.ownerID = ownerID;
    }

    private bool MatchOwnership(Guid id)
    {
        if (ownerID == null)
            return false;
        return id == ownerID;
    }

    private void FixedUpdate()
    {
        if (!col.enabled)
            return;

        CheckCurrentOverlap();

        Vector3 currentCenter = col.bounds.center;        
        if (hasPrevPosition)
            CheckSweep(prevPosition, currentCenter);

        prevPosition = currentCenter;
        hasPrevPosition = true;
    }
    

    #region Detection

    // Runs OverlapBox/Sphere/Capsule based on the actual collider type. Any IHurtBox
    // found that hasn't been reported this active window fires OnCollisionEntered.
    private void CheckCurrentOverlap()
    {
        int count = 0;

        switch (col)
        {
            case BoxCollider box:
            {
                Vector3 worldCenter = box.transform.TransformPoint(box.center);
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, AbsScale(box.transform.lossyScale));
                count = Physics.OverlapBoxNonAlloc(worldCenter, halfExtents, overlapBuffer, box.transform.rotation, collisionMask, QueryTriggerInteraction.Collide);
                break;
            }
            case SphereCollider sphere:
            {
                Vector3 worldCenter = sphere.transform.TransformPoint(sphere.center);
                float radius = sphere.radius * MaxAbsScale(sphere.transform.lossyScale);
                count = Physics.OverlapSphereNonAlloc(worldCenter, radius, overlapBuffer, collisionMask, QueryTriggerInteraction.Collide);
                break;
            }
            case CapsuleCollider capsule:
            {
                GetCapsuleEndpoints(capsule, out Vector3 p0, out Vector3 p1, out float radius);
                count = Physics.OverlapCapsuleNonAlloc(p0, p1, radius, overlapBuffer, collisionMask, QueryTriggerInteraction.Collide);
                break;
            }
            default:
                Debug.LogWarning($"HitBox: unsupported collider type {col.GetType().Name}. Only Box, Sphere, and Capsule are supported.", this);
                return;
        }

        for (int i = 0; i < count; i++)
        {
            Collider other = overlapBuffer[i];            

            if (other.TryGetComponent(out IHurtBox hurtBox) && !hurtBoxesHit.Contains(hurtBox.OwnerID) && !MatchOwnership(hurtBox.OwnerID))
            {
                hurtBoxesHit.Add(hurtBox.OwnerID);
                Vector3 hitPoint = other.ClosestPoint(col.bounds.center);

                OnCollisionEntered?.Invoke(this, hurtBox, hitPoint);
            }
        }
    }

    // Runs BoxCast/SphereCast/CapsuleCast from prev center to current center.
    // Direction/distance derived from the delta; if there's no movement, skip the sweep.
    private void CheckSweep(Vector3 fromCenter, Vector3 toCenter)
    {
        Vector3 delta = toCenter - fromCenter;
        float distance = delta.magnitude;
        if (distance < 1e-5f)
            return;

        Vector3 direction = delta / distance;
        int count = 0;

        switch (col)
        {
            case BoxCollider box:
            {
                // Cast originates at the previous center, not the current one.
                Vector3 prevWorldCenter = fromCenter + (box.transform.TransformPoint(box.center) - col.bounds.center);
                Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, AbsScale(box.transform.lossyScale));
                count = Physics.BoxCastNonAlloc(prevWorldCenter, halfExtents, direction, castBuffer, box.transform.rotation, distance, collisionMask, QueryTriggerInteraction.Collide);
                break;
            }
            case SphereCollider sphere:
            {
                float radius = sphere.radius * MaxAbsScale(sphere.transform.lossyScale);
                count = Physics.SphereCastNonAlloc(fromCenter, radius, direction, castBuffer, distance, collisionMask, QueryTriggerInteraction.Collide);
                break;
            }
            case CapsuleCollider capsule:
            {
                GetCapsuleEndpoints(capsule, out Vector3 p0, out Vector3 p1, out float radius);
                // Endpoints are at the current position; offset them back to the previous position.
                Vector3 offset = fromCenter - toCenter;
                count = Physics.CapsuleCastNonAlloc(p0 + offset, p1 + offset, radius, direction, castBuffer, distance, collisionMask, QueryTriggerInteraction.Collide);
                break;
            }
            default:
                // Already warned in CheckCurrentOverlap; stay silent here.
                return;
        }

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = castBuffer[i];
            Collider other = hit.collider;
            if (other == null) continue;

            if (other.TryGetComponent(out IHurtBox hurtBox) && !hurtBoxesHit.Contains(hurtBox.OwnerID) && !MatchOwnership(hurtBox.OwnerID))
            {
                hurtBoxesHit.Add(hurtBox.OwnerID);
                // hit.point is zero when the cast starts already overlapping; fall back to ClosestPoint in that case.
                Vector3 hitPoint = hit.point.sqrMagnitude > 1e-8f
                    ? hit.point
                    : other.ClosestPoint(col.bounds.center);

                OnCollisionEntered?.Invoke(this, hurtBox, hitPoint);
            }
        }
    }

    private static void GetCapsuleEndpoints(CapsuleCollider capsule, out Vector3 p0, out Vector3 p1, out float radius)
    {
        Transform t = capsule.transform;
        Vector3 worldCenter = t.TransformPoint(capsule.center);
        Vector3 lossyScale = AbsScale(t.lossyScale);

        Vector3 axis;
        float radiusScale;
        float heightScale;
        switch (capsule.direction)
        {
            case 0: // X
                axis = t.right;
                radiusScale = Mathf.Max(lossyScale.y, lossyScale.z);
                heightScale = lossyScale.x;
                break;
            case 2: // Z
                axis = t.forward;
                radiusScale = Mathf.Max(lossyScale.x, lossyScale.y);
                heightScale = lossyScale.z;
                break;
            default: // Y
                axis = t.up;
                radiusScale = Mathf.Max(lossyScale.x, lossyScale.z);
                heightScale = lossyScale.y;
                break;
        }

        radius = capsule.radius * radiusScale;
        float height = Mathf.Max(capsule.height * heightScale, radius * 2f);
            float halfCylinder = Mathf.Max(0f, (height - radius * 2f) * 0.5f);

        p0 = worldCenter + axis * halfCylinder;
        p1 = worldCenter - axis * halfCylinder;
    }

    private static Vector3 AbsScale(Vector3 s) => new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

    private static float MaxAbsScale(Vector3 s) => Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));

    #endregion


    #region Gizmos

    private void OnDrawGizmos()
    {
        var hitboxCollider = col ? col : GetComponent<Collider>();
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
