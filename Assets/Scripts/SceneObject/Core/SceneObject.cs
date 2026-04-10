using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
internal abstract class SceneObject : MonoBehaviour, ISceneObject
{
    [Header("SceneObject")]
    private Guid uniqueID;
    [SerializeField] private string uniqueIDString; //NOTE: Guid does not show up in inspector
    [SerializeField] private SceneObjectType ObjectType;

    private Collider col;
    private Rigidbody rb;    

    #region Getters        
    
    public Guid UniqueID => uniqueID;   
    public Bounds Bounds => col.bounds; //TODO: This will represent all colliders, this is the volume of the sceneObject 

    #endregion


    #region Initialize

    protected virtual void Awake()
    {        
        uniqueID = Guid.NewGuid();
        uniqueIDString = uniqueID.ToString();

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
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

    public bool CheckForEnvironmentCollision(Vector3 direction, float dist, out RaycastHit hitInfo)
    {
        return Physics.Raycast(Bounds.center, direction.normalized, out hitInfo, dist, LayerMask.GetMask("Environment"));
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


    #region Debug

    public void Log(string log)
    {
        Debug.Log($"({ObjectType}) {gameObject.name}: {log}", gameObject);
    }

    #endregion
}




