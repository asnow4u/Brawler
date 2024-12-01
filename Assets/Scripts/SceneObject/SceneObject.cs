using System.Collections.Generic;
using UnityEngine;
using System;
using static Unity.Cinemachine.IInputAxisOwner.AxisDescriptor;
using UnityEngine.UI;

public enum SceneObjectType { Player, Enemy, Object }
public enum GroundedState { Airborn, Grounded }

public enum Direction { Right, Left, Up, Down }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MovementInputHandler))]
[RequireComponent(typeof(AttackInputHandler))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(AnimationHandler))]
[RequireComponent(typeof(UIHandler))]
[RequireComponent(typeof(DamageHandler))]
public abstract class SceneObject : MonoBehaviour
{
    [Header("SceneObject")]
    public string UniqueId;
    public SceneObjectType ObjectType;

    [Header("Ground Status")]
    [SerializeField] private GroundedState curGroundedState;
    [SerializeField] private float maxSlopeAngle; //Move to moveData? or MovementInputHandler?

    //Logger
    private SceneObjectLogger logger;

    //Handlers
    private ActionStateHandler actionStateHandler;
    private MovementInputHandler movementInputHandler;
    private AttackInputHandler attackInputHandler;
    private AnimationHandler animationHandler;
    private UIHandler uiHandler;
    private DamageHandler damageHandler;

    public IEquipment EquipmentHandler;
    public IInteraction InteractionHandler;

    //Getters
    public SceneObjectLogger Logger => logger;
    public ActionStateHandler ActionStateHandler => actionStateHandler;
    public MovementInputHandler MovementInputHandler => movementInputHandler;
    public AttackInputHandler AttackInputHandler => attackInputHandler;
    public AnimationHandler AnimationHandler => animationHandler;
    public UIHandler UIHandler => uiHandler;
    public DamageHandler DamageHandler => damageHandler;

    
    public GroundedState CurGroundedState => curGroundedState;   
    public Rigidbody Rb => GetComponent<Rigidbody>();
    private Collider collider => GetComponent<Collider>();


    //Events
    public event Action<GroundedState> GroundedStateChangeEvent;


    #region Initialize

    private void Start()
    {
        InspectorCheck();
        Initialize();        
    }


    /// <summary>
    /// Make sure everything has been set within the inspector
    /// </summary>
    private void InspectorCheck()
    {        
        Debug.Assert(maxSlopeAngle > 0, "MaxSlopeAngle needs to be > 0." ,gameObject);
    }
    

    /// <summary>
    /// Create Unique ID
    /// Setup handlers
    /// </summary>
    protected virtual void Initialize()
    {               
        UniqueId = Guid.NewGuid().ToString();
        curGroundedState = GroundedState.Grounded;

        GetHandlers();
        SetUpHandlers();
        InitializeHandlers(); 
        
        logger = new SceneObjectLogger(this);
    }


    /// <summary>
    /// Grab all handlers from gameobject
    /// </summary>
    private void GetHandlers()
    {
        InteractionHandler = new InteractionHandler();

        actionStateHandler = GetComponent<ActionStateHandler>();
        animationHandler = GetComponent<AnimationHandler>();
        uiHandler = GetComponent<UIHandler>();
        movementInputHandler = GetComponent<MovementInputHandler>();
        attackInputHandler = GetComponent<AttackInputHandler>();
        damageHandler = GetComponent<DamageHandler>();
    }


    /// <summary>
    /// Set up all handlers <br/>
    /// Set up events before performing actions
    /// </summary>
    private void SetUpHandlers()
    {
        actionStateHandler.SetUp();    
        animationHandler.Setup();                
        uiHandler.Setup();
        movementInputHandler.Setup();        
        attackInputHandler.Setup();
        damageHandler.Setup();
    }


    /// <summary>
    /// Initialize all handlers
    /// </summary>
    private void InitializeHandlers()
    {
        actionStateHandler.Initialize();
        uiHandler.Initialize();
        movementInputHandler.Initialize();
        attackInputHandler.Initialize();        
        animationHandler.Initialize(); //NOTE: needs to happen after move and attack handlers
        damageHandler.Initialize();
    }

  
    #endregion


    #region Update

    protected virtual void Update()
    {        
        attackInputHandler.HandleUpdate();
    }


    protected virtual void FixedUpdate()
    {  
        MovementInputHandler.UpdateMovement();

        DamageHandler.HandleUpdate();

        //Grounded Status
        CheckGroundedStatus();
    }

    #endregion


    #region Ground Status

    /// <summary>
    /// Use raycasts to determine current status of the ground
    /// </summary>
    private void CheckGroundedStatus()
    {
        if (TryGetSlopeAngle(out Vector3 slopeAngle))
        {
            if (curGroundedState != GroundedState.Grounded)
            {
                curGroundedState = GroundedState.Grounded;
                GroundedStateChangeEvent?.Invoke(curGroundedState);
            }
        }

        else
        {
            if (curGroundedState != GroundedState.Airborn)
            {
                curGroundedState = GroundedState.Airborn;
                GroundedStateChangeEvent?.Invoke(curGroundedState);
            }
        }
    }


    /// <summary>
    /// Attempt to get the current slope of the environment under the sceneObject
    /// Casts 10 rays based on the left/right most point of the collider
    /// </summary>
    /// <param name="slopeAngle"></param>
    /// <returns></returns>
    public bool TryGetSlopeAngle(out Vector3 slopeAngle)
    {
        List<RaycastHit> hits = new List<RaycastHit>();

        //Create raycasts
        Vector3 leftSidePoint = collider.bounds.center + Vector3.left * collider.bounds.extents.x;
        Vector3 rightSidePoint = collider.bounds.center + Vector3.right * collider.bounds.extents.x;
        float spaceBetweenRays = (rightSidePoint.x - leftSidePoint.x) / 10;

        //Raycast
        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = leftSidePoint + Vector3.right * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + 0.3f, LayerMask.GetMask("Environment")))
            {
                hits.Add(hit);
            }
        }

        if (hits.Count > 0)
        {
            //Average normals
            Vector3 avgNormal = Vector3.zero;

            foreach (RaycastHit hit in hits)
            {
                avgNormal += hit.normal;
            }

            avgNormal /= 10;

            //Determine slope angle
            slopeAngle = Vector3.Cross(avgNormal, transform.forward).normalized;
            
            Debug.DrawRay(collider.bounds.center + Vector3.down * collider.bounds.extents.y, avgNormal, Color.green);
            Debug.DrawRay(collider.bounds.center + Vector3.down * collider.bounds.extents.y, slopeAngle, Color.red);

            return true;
        }

        slopeAngle = Vector3.zero;
        return false;
    }

    #endregion


    #region RigidBody

    public Rigidbody CoreRigidBody 
    { 
        get { return GetComponent<Rigidbody>(); }            
    }


    /// <summary>
    /// Returns a list of active rigidbody where the associated collider is not trigger
    /// </summary>
    public List<Rigidbody> ActiveRigidbodies 
    { 
        get 
        {
            List<Rigidbody> activeRbs = new List<Rigidbody>();

            foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>())
            {
                if (rb.TryGetComponent(out Collider collider))
                {
                    if (collider.enabled == true && collider.isTrigger == false)
                        activeRbs.Add(rb);
                }
            }

            return activeRbs;
        } 
    }

    #endregion


    #region Direction

    public void TurnAround()
    {
        transform.Rotate(transform.up, 180f);

        UIHandler.RotateDisplayText();
    }


    public bool IsFacingRightDirection()
    {
        float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
        float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

        if (angleRightDiff < angleLeftDiff)
        {
            return true;
        }

        return false;
    }

    #endregion



    #region Collision Detection

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
                collidingCollider= DownSideCollisionDetection(dist, mask);
                break;        
        }

        return collidingCollider != null;
    }  


    /// <summary>
    /// Check right side for any collisions
    /// </summary>
    private Collider RightSideCollisionDetection(float dist, LayerMask mask)
    {
        Vector3 point1 = collider.bounds.center + Vector3.up * collider.bounds.extents.y;
        Vector3 point2 = collider.bounds.center + Vector3.down * collider.bounds.extents.y;
        float spaceBetweenRays = (point1.y - point2.y) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.right, out RaycastHit hit, collider.bounds.extents.x + dist, mask))
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
        Vector3 point1 = collider.bounds.center + Vector3.up * collider.bounds.extents.y;
        Vector3 point2 = collider.bounds.center + Vector3.down * collider.bounds.extents.y;
        float spaceBetweenRays = (point1.y - point2.y) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.left, out RaycastHit hit, collider.bounds.extents.x + dist, mask))
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
        Vector3 point1 = collider.bounds.center + Vector3.right * collider.bounds.extents.x;
        Vector3 point2 = collider.bounds.center + Vector3.left * collider.bounds.extents.x;
        float spaceBetweenRays = (point1.x - point2.x) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, collider.bounds.extents.y + dist, mask))
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
        Vector3 point1 = collider.bounds.center + Vector3.right * collider.bounds.extents.x;
        Vector3 point2 = collider.bounds.center + Vector3.left * collider.bounds.extents.x;
        float spaceBetweenRays = (point1.x - point2.x) / 10;

        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + dist, mask))
            {
                return hit.collider;
            }
        }

        return null;
    }

    #endregion
}

