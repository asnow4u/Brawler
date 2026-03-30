using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
internal abstract class SceneObject : MonoBehaviour, ISceneObject
{
    [Header("SceneObject")]
    [SerializeField] private Guid uniqueID;
    public SceneObjectType ObjectType;

    [Header("Ground Status")]
    [SerializeField] private GroundedState curGroundedState;

    [Header("Climb Status")]
    [SerializeField] private ClimbState curClimbState;

    private Collider col;
    private Rigidbody rb;    

    #region Getters        
    
    public Guid UniqueID => uniqueID;   
    public Bounds Bounds => col.bounds; //TODO: This will represent all colliders, this is the volume of the sceneObject

    public GroundedState CurGroundedState => curGroundedState;
    public ClimbState CurClimbState => curClimbState;             

    #endregion

    //Events
    public event Action<GroundedState> GroundedStateChangedEvent;
    public event Action<ClimbState, ClimbState> ClimbStateChangedEvent;


    #region Initialize

    protected virtual void Awake()
    {        
        uniqueID = Guid.NewGuid();
        curGroundedState = GroundedState.Grounded;
        curClimbState = ClimbState.Unavailable;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }    

    #endregion


    #region Update

    private void FixedUpdate()
    {
        CheckClimbingState();
        CheckGroundedState();
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


    #region Ground State

    /// <summary>
    /// Use raycasts to determine current status of the ground
    /// </summary>
    private void CheckGroundedState()
    {            
        switch (curGroundedState)
        {
            case GroundedState.Grounded:
                UpdateGroundedState();
                break;

            case GroundedState.Airborn:
                UpdateAirbornState();
                break;

            case GroundedState.Climbing:
                UpdateClimbingState();
                break;
        }
    }

    private void UpdateGroundedState()
    {
        //Switch to climbing
        if (curClimbState == ClimbState.Climbing)            
        {
            curGroundedState = GroundedState.Climbing;
            GroundedStateChangedEvent?.Invoke(curGroundedState);
        }

        //Switch to airborn
        else if (!GroundCheck())
        {
            curGroundedState = GroundedState.Airborn;
            GroundedStateChangedEvent?.Invoke(curGroundedState);
        }
    }

    private void UpdateAirbornState()
    {
        //Switch to climbing
        if (curClimbState == ClimbState.Climbing)
        {
            curGroundedState = GroundedState.Climbing;
            GroundedStateChangedEvent?.Invoke(curGroundedState);
        }

        //Switch to grounded
        else if (GroundCheck())
        {
            curGroundedState = GroundedState.Grounded;
            GroundedStateChangedEvent?.Invoke(curGroundedState);
        }
    }
        
    private void UpdateClimbingState()
    {
        if (curClimbState != ClimbState.Climbing)
        {
            curGroundedState = GroundCheck() ? GroundedState.Grounded : GroundedState.Airborn;
            GroundedStateChangedEvent?.Invoke(curGroundedState);
        }
    }

    private bool GroundCheck()
    {
        return true; //TODO: Temp
        //return CheckForEnvironmentCollision(Vector3.down, col.bounds.extents.y + 0.01f, out RaycastHit hitInfo);
    }

    #endregion


    #region Climb State

    private void CheckClimbingState()
    {
        switch (curClimbState)
        {
            case ClimbState.Unavailable:
                UpdateUnavailableClimbState();
                break;
            case ClimbState.Available:
                UpdateAvailableClimbState();
                break;
            case ClimbState.Climbing:
                UpdateClimbingClimbState();
                break;
        }
    }

    private void UpdateUnavailableClimbState()
    {
        if (ClimbSurfaceCheck())
            SetClimbState(ClimbState.Available);
    }

    private void UpdateAvailableClimbState()
    {
        if (!ClimbSurfaceCheck())
            SetClimbState(ClimbState.Unavailable);

        //else if (MovementInputHandler.VerticalInfluence != 0)
        //{
        //    // NOTE: Need to climb upwards while on the ground
        //    if (CurGroundedState == GroundedState.Grounded && MovementInputHandler.VerticalInfluence < 0)
        //        return;                

        //    SetClimbState(ClimbState.Climbing);
        //}
    }

    private void UpdateClimbingClimbState()
    {
        if (!ClimbSurfaceCheck())
            SetClimbState(ClimbState.Unavailable);

        //// Hit ground while climbing (NOTE: Dont check based on groundedState since climbState is updated first)
        //else if (GroundCheck() && !(MovementInputHandler.VerticalInfluence > 0))
        //    SetClimbState(ClimbState.Available);

        //// Jump while climbing
        //else if (MovementInputHandler.CurMovementInputData != null && MovementInputHandler.CurMovementInputData.Type == MovementType.Jump)
        //    SetClimbState(ClimbState.Available);

        //// Attack action performed
        //else if (ActionStateHandler.CurActionState == ActionState.Attacking)
        //    SetClimbState(ClimbState.Available);
    }

    private bool ClimbSurfaceCheck()
    {
        return true; //TODO: Temp
        //return CheckForClimbableSurface();
    }

    public void UpdateClimbState(ClimbState climbState)
    {
        SetClimbState(climbState);
    }

    /// <summary>
    /// Set the climb state of the sceneObject to <paramref name="state"/>
    /// </summary>
    private void SetClimbState(ClimbState state)
    {
        if (curClimbState == state)
            return;

        ClimbState prevClimbState = curClimbState;
        curClimbState = state;

        if (curClimbState == ClimbState.Climbing)
            rb.useGravity = false;
        else
            rb.useGravity = true;

        ClimbStateChangedEvent?.Invoke(prevClimbState, curClimbState);
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

    public bool CheckForClimbableSurface()
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




