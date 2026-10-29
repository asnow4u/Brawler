using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public abstract class SceneObject : MonoBehaviour, ISceneObject
{
    [Header("SceneObject")]
    private Guid uniqueID;
    [SerializeField] private string uniqueIDString; //NOTE: Guid does not show up in inspector
    [SerializeField] private SceneObjectType objectType;
    public SceneObjectType ObjectType => objectType;

    private Collider col;
    protected Rigidbody rb;

    [Header("Environment Collider")]
    [Tooltip("Points the root CapsuleCollider or BoxCollider is fitted to each physics step. Leave empty to keep the authored collider.")]
    [SerializeField] private Transform[] colliderPoints;
    [Tooltip("Distance added around the points on every side except a ground-locked bottom.")]
    [SerializeField] private float colliderPadding = 0.05f;
    [Tooltip("Minimum width and height of the fitted collider.")]
    [SerializeField] private float minColliderSize = 0.2f;
    private CapsuleCollider fittedCapsule;
    private BoxCollider fittedBox;
    private bool isColliderGroundLocked = false;
    private float lastColliderFitTime = -1f;

    private bool beingDestroyed = false;

    #region Getters        
    
    public Guid UniqueID => uniqueID;   

    //TODO: This will represent all colliders, this is the volume of the sceneObject 
    public Bounds Bounds
    {
        get
        {
            // Looks up the collider when it has not been cached.
            if (col == null)
                col = GetComponent<Collider>();

            FitCollider();

            return col.bounds;
        }
    }

    #endregion


    #region Initialize

    protected virtual void Awake()
    {        
        uniqueID = Guid.NewGuid();
        uniqueIDString = uniqueID.ToString();

        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        col = GetComponent<Collider>();

        if (colliderPoints != null && colliderPoints.Length > 0)
        {
            fittedCapsule = col as CapsuleCollider;
            fittedBox = col as BoxCollider;
            if (fittedCapsule == null && fittedBox == null)
                Debug.LogError("Collider points require a CapsuleCollider or BoxCollider on the SceneObject root.", this);
        }
    }    

    protected virtual void OnDestroy()
    {
        beingDestroyed = true;
    }

    #endregion


    #region Direction

    /// <returns>
    /// Whether the sceneObject is facing the right direction
    /// </returns>
    public bool IsFacingRightDirection
    {
        get
        {
            float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
            float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

            if (angleRightDiff < angleLeftDiff)
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Turn the sceneObject around 
    /// </summary>
    public void TurnAround()
    {
        if (IsFacingRightDirection)
            transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        else
            transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        //sceneObject.UIHandler.RotateDisplayText(); //TODO: UI should handle this
    }

    #endregion


    #region Collision

    /// <summary>
    /// Try to detect if a collider exists
    /// </summary>
    public bool TryDetectCollision(Direction direction, float dist, LayerMask mask, out Collider collidingCollider)
    {
        collidingCollider = null;

        switch (direction)
        {
            case Direction.Left:
                collidingCollider = LeftSideCollisionDetection(dist, mask);
                break;

            case Direction.Right:
                collidingCollider = RightSideCollisionDetection(dist, mask);
                break;

            case Direction.Up:
                collidingCollider = UpSideCollisionDetection(dist, mask);
                break;

            case Direction.Down:
                collidingCollider = DownSideCollisionDetection(dist, mask);
                break;
        }

        return collidingCollider != null;
    }

    /// <summary>
    /// Check right side for any collisions
    /// </summary>
    private Collider RightSideCollisionDetection(float dist, LayerMask mask)
    {
        Vector3 point1 = Bounds.center + Vector3.up * Bounds.extents.y;
        Vector3 point2 = Bounds.center + Vector3.down * Bounds.extents.y;
        float spaceBetweenRays = (point1.y - point2.y) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.right, out RaycastHit hit, Bounds.extents.x + dist, mask))
            {
                return hit.collider;
            }
        }

        return null;
    }

    /// <summary>
    /// Check left side for any collisions
    /// </summary>
    private Collider LeftSideCollisionDetection(float dist, LayerMask mask)
    {
        Vector3 point1 = Bounds.center + Vector3.up * Bounds.extents.y;
        Vector3 point2 = Bounds.center + Vector3.down * Bounds.extents.y;
        float spaceBetweenRays = (point1.y - point2.y) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.left, out RaycastHit hit, Bounds.extents.x + dist, mask))
            {
                return hit.collider;
            }
        }

        return null;
    }

    /// <summary>
    /// Check up for any collisions
    /// </summary>
    private Collider UpSideCollisionDetection(float dist, LayerMask mask)
    {
        Vector3 point1 = Bounds.center + Vector3.right * Bounds.extents.x;
        Vector3 point2 = Bounds.center + Vector3.left * Bounds.extents.x;
        float spaceBetweenRays = (point1.x - point2.x) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, Bounds.extents.y + dist, mask))
            {
                return hit.collider;
            }
        }

        return null;
    }

    /// <summary>
    /// Check down for any collisions
    /// </summary>
    private Collider DownSideCollisionDetection(float dist, LayerMask mask)
    {
        Vector3 point1 = Bounds.center + Vector3.right * Bounds.extents.x;
        Vector3 point2 = Bounds.center + Vector3.left * Bounds.extents.x;
        float spaceBetweenRays = (point1.x - point2.x) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Bounds.extents.y + dist, mask))
            {
                return hit.collider;
            }
        }

        return null;
    }

    public bool ClimbableSurfaceAvailable()
    {        
        Vector3 center = Bounds.center;
        Vector3 halfExtents = Bounds.extents;

        Collider[] hits = Physics.OverlapBox(
            center,
            halfExtents,
            Quaternion.identity,
            LayerMask.GetMask("Climbable"),
            QueryTriggerInteraction.Collide
        );

        return hits.Length > 0;
    }

    #endregion


    #region Environment Collider

    /// <summary>Locks the bottom of the fitted collider to the root's Y.</summary>
    public void SetColliderGroundLock(bool locked)
    {
        isColliderGroundLocked = locked;
    }

    /// <summary>Fits the CapsuleCollider or BoxCollider to the collider points in root space, at most once per physics step.</summary>
    private void FitCollider()
    {
        if ((fittedCapsule == null && fittedBox == null) || lastColliderFitTime == Time.fixedTime)
            return;

        lastColliderFitTime = Time.fixedTime;

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        int pointCount = 0;

        foreach (Transform point in colliderPoints)
        {
            if (point == null)
                continue;

            Vector2 local = transform.InverseTransformPoint(point.position);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
            pointCount++;
        }

        if (pointCount == 0)
            return;

        min -= Vector2.one * colliderPadding;
        max += Vector2.one * colliderPadding;

        if (isColliderGroundLocked)
            min.y = 0f;

        Vector2 size = Vector2.Max(max - min, Vector2.one * minColliderSize);
        Vector2 center = (min + max) * 0.5f;

        if (isColliderGroundLocked)
            center.y = size.y * 0.5f;

        if (fittedBox != null)
        {
            fittedBox.size = new Vector3(size.x, size.y, fittedBox.size.z);
            fittedBox.center = new Vector3(center.x, center.y, fittedBox.center.z);
            return;
        }

        bool isHorizontal = size.x >= size.y;

        fittedCapsule.direction = isHorizontal ? 0 : 1;
        fittedCapsule.radius = (isHorizontal ? size.y : size.x) * 0.5f;
        fittedCapsule.height = isHorizontal ? size.x : size.y;
        fittedCapsule.center = new Vector3(center.x, center.y, fittedCapsule.center.z);
    }

    #endregion


    #region Debug

    public void Log(string log)
    {
        if (!beingDestroyed)
            Debug.Log($"({ObjectType}) {gameObject.name}: {log}", gameObject);
    }

    #endregion
}




