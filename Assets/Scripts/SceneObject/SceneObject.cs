using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem.Utilities;
using System.Threading.Tasks;

public enum SceneObjectType { Player, Enemy, Object }
public enum GroundedState { Airborn, Grounded, Sliding }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MovementInputHandler))]
[RequireComponent(typeof(AttackInputHandler))]
[RequireComponent(typeof(AnimationStateHandler))]
[RequireComponent(typeof(DamageHandler))]
public abstract class SceneObject : MonoBehaviour
{
    [Header("SceneObject")]
    public string UniqueId;
    public SceneObjectType ObjectType;

    [Header("Ground Status")]
    [SerializeField] private GroundedState curGroundedState;
    [SerializeField] private float maxSlopeAngle;      

    //Handlers
    public IEquipment EquipmentHandler;
    public IInteraction InteractionHandler;

    //Getters
    public IAnimator AnimationStateHandler => GetComponentInChildren<IAnimator>();
    public MovementInputHandler MovementInputHandler => GetComponent<MovementInputHandler>();
    public AttackInputHandler AttackInputHandler => GetComponent<AttackInputHandler>();
    public DamageHandler DamageHandler => GetComponent<DamageHandler>();
    
    public GroundedState GroundedState => curGroundedState;   
    public Rigidbody Rb => GetComponent<Rigidbody>();
    private Collider collider => GetComponent<Collider>();


    //Events
    public event Action<GroundedState> GroundedStateChangeEvent;

    #region Initialize

    private void Start()
    {
        Initialize();
    }
    

    /// <summary>
    /// Create Unique ID
    /// Setup handlers
    /// </summary>
    protected virtual void Initialize()
    {               
        UniqueId = Guid.NewGuid().ToString();        

        InitializeInteractionHandler();
        InitializeEquipmentHandler();
        InitializeMovementHandler();
        InitializeAttackHandler();
        InitializeAnimationStateHandler();
        InitializeDamageHandler();
    }

    private void InitializeInteractionHandler()
    {
        InteractionHandler = new InteractionHandler();
    }

    private void InitializeEquipmentHandler()
    {
        //TODO: Rework
        //EquipmentHandler = new EquipmentHandler(this);
    }

    private void InitializeAnimationStateHandler()
    {
        AnimationStateHandler.SetUp();
    }

    private void InitializeMovementHandler()
    {         
        MovementInputHandler.Setup();        
    }

    private void InitializeAttackHandler()
    {        
        AttackInputHandler.Setup();        
    }

    private void InitializeDamageHandler()
    {
        DamageHandler.Initialize();
    }

    #endregion


    #region Fixed Update

    protected virtual void FixedUpdate()
    {  
        MovementInputHandler.UpdateMovement();
        
        DamageHandler.PredictHitStunBounce();

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
            float angle = Vector3.Angle(transform.right, slopeAngle);

            switch (curGroundedState)
            {
                case GroundedState.Grounded:

                    if (angle > maxSlopeAngle)
                    {
                        //Face direction of downward slope
                        //if (slopeAngle.y > 0)
                        //    TurnAround();

                        curGroundedState = GroundedState.Sliding;

                        GroundedStateChangeEvent?.Invoke(curGroundedState);
                    }
                    break;

                case GroundedState.Sliding:

                    if (angle < maxSlopeAngle)
                    {                     
                        curGroundedState = GroundedState.Grounded;

                        GroundedStateChangeEvent?.Invoke(curGroundedState);
                    }
                    break;

                case GroundedState.Airborn:

                    if (angle > maxSlopeAngle)
                        curGroundedState = GroundedState.Sliding;
                    else
                        curGroundedState = GroundedState.Grounded;

                    //End animation at or below attacking state
                    AnimationStateHandler.EndCurrentAnimation(ActionState.Attacking);

                    GroundedStateChangeEvent?.Invoke(curGroundedState);
                    
                    break;
            }
        }

        else
        {
            if (curGroundedState != GroundedState.Airborn)
            {
                curGroundedState = GroundedState.Airborn;

                //End animation at or below attacking state
                AnimationStateHandler.EndCurrentAnimation(ActionState.Attacking);

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


    #region Direction

    public void TurnAround()
    {
        transform.Rotate(transform.up, 180f);
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

}

