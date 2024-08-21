using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;


public enum MovementType { Null, Move, Jump, AirJump, FreeFall, Landing }

public class MovementInputHandler : MonoBehaviour
{
    const ActionState MOVESTATE = ActionState.Moving;

    //Movement State Data
    [Header("State")]
    [SerializeField] private MovementType curMoveState;
    private MovementData curMoveData;

    //Jump Properties
    private int numJumpsPerformed;

    //Fast Fall Properties
    private const float fastFallAcceleration = 20f;
    private const float MaxYVelocity = 15f;

    //Slope Properties
    [Header("Slope")]    
    [SerializeField] private float slidingMaxVelocity;
    [SerializeField] private float slidingAcceleration;

    //Movement Collection (NOTE: BaseMovementCollection is Required for all sceneObjects)    
    [Header("Collection")]
    [SerializeField] private MovementCollection baseMovementCollection;
    private MovementCollection curMovementCollection = null;

    //Infulence
    private float horizontalInfluence;
    private float verticalInfluence;
    private float jumpInfluence;
   
    //SceneObject
    private SceneObject sceneObject;

    //Events
    public event Action<MovementCollection> MovementCollectionChangedEvent;
    public event Action<MovementType> MoveStateChangedEvent;


    #region Getters

    public MovementType CurMoveState => curMoveState;    

    public MovementCollection CurMovementCollection => curMovementCollection;  

    #endregion


    #region Initialize

    public void Setup()
    {
        sceneObject = GetComponent<SceneObject>();
        SetupEvents();

        Debug.Assert(baseMovementCollection != null, "Base Movement Collection is NULL", this);
        Debug.Assert(baseMovementCollection.MoveData != null, "Base Movement Collection Mode Data is Null", this);

        curMovementCollection = baseMovementCollection;
    }


    public void Initialize()
    {
        
    }

    #endregion


    #region Events

    private void SetupEvents()
    {
        sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;
        sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
        sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
    }

    
    /// <summary>
    /// Ground state changed
    /// </summary>
    /// <param name="curGroundState"></param>
    private void OnGroundedStateChanged(GroundedState curGroundState)
    {
        switch (curGroundState)
        {
            case GroundedState.Grounded:
            case GroundedState.Sliding:
                PerformLanding();
                break;

            case GroundedState.Airborn:
                //TODO: Set to free fall
                break;

        }
    }

    #endregion


    #region MoveState

    private void SetCurrentMoveState(MovementType moveState)
    {
        if (moveState != MovementType.Null)
        {
            if (moveState != MovementType.Landing)
            {
                //Change State
                if (sceneObject.ActionStateHandler.TryChangeState(MOVESTATE))
                {              
                    Debug.Log("MOVEMENT: CurMoveState Set To: " + moveState);
                    curMoveState = moveState;
                    MoveStateChangedEvent?.Invoke(moveState);
                }
            }

            //Landing
            else
            {
                //TODO: Check for hitstun?

                sceneObject.ActionStateHandler.ChangeState(MOVESTATE);
                Debug.Log("MOVEMENT: CurMoveState Set To: " + moveState);
                curMoveState = moveState;
                MoveStateChangedEvent?.Invoke(moveState);
            }
        }

        else
        {
            curMoveState = MovementType.Null;
            curMoveData = null;
            MoveStateChangedEvent?.Invoke(moveState);
        }
    }

    #endregion


    #region Turn Around

    private void CheckTurnAround()
    {
        if (sceneObject.IsFacingRightDirection() && horizontalInfluence < 0)
        {
            sceneObject.TurnAround();

            if (sceneObject.CoreRigidBody.velocity.x > 0)
            {
                switch (sceneObject.GroundedState)
                {
                    case GroundedState.Grounded:
                    case GroundedState.Sliding:
                        if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                        {
                            sceneObject.CoreRigidBody.velocity = slope * sceneObject.CoreRigidBody.velocity.magnitude;
                        }
                        break;

                    case GroundedState.Airborn:
                        sceneObject.CoreRigidBody.velocity = new Vector3(sceneObject.CoreRigidBody.velocity.x * -1, sceneObject.CoreRigidBody.velocity.y, sceneObject.CoreRigidBody.velocity.z);
                        break;
                }
            }
        }

        else if (!sceneObject.IsFacingRightDirection() && horizontalInfluence > 0)
        {
            sceneObject.TurnAround();

            switch (sceneObject.GroundedState)
            {
                case GroundedState.Grounded:
                case GroundedState.Sliding:
                    if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                    {
                        sceneObject.CoreRigidBody.velocity = slope * sceneObject.CoreRigidBody.velocity.magnitude;
                    }
                    break;

                case GroundedState.Airborn:
                    sceneObject.CoreRigidBody.velocity = new Vector3(sceneObject.CoreRigidBody.velocity.x * -1, sceneObject.CoreRigidBody.velocity.y, sceneObject.CoreRigidBody.velocity.z);
                    break;
            }
        }
    }

    #endregion


    #region Perform Move

    /// <summary>
    /// Horizontal movement based on input value
    /// Input value ranges between (-1, 1) 
    /// Input Value determines how much of the curCollection moveSpeed should be applied
    /// </summary>
    /// <param name="inputInfluence"></param>
    public void PerformMovement(Vector2 inputInfluence)
    {       
        //Check that moveData exists
        if (curMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData movement))
        {          
            horizontalInfluence = Mathf.Clamp(inputInfluence.x, -1, 1);
            verticalInfluence = Mathf.Clamp(inputInfluence.y, -1, 1);                             
        }                           
    }


    /// <summary>
    /// Update movement based on grounded status
    /// </summary>
    public void UpdateMovement()
    {
        switch (sceneObject.GroundedState)
        {
            case GroundedState.Grounded:
                UpdateGroundMovement(sceneObject.CoreRigidBody);
                break;

            case GroundedState.Sliding:
                UpdateSlidingMovement(sceneObject.CoreRigidBody);
                break;

            case GroundedState.Airborn:
                UpdateAirMovement(sceneObject.CoreRigidBody);
                break;
        }
    }


    #region Ground Movement

    /// <summary>
    /// Update to grounded movement
    /// </summary>
    /// <param name="rb"></param>
    private void UpdateGroundMovement(Rigidbody rb)
    {
        //Apply Movement only if able to move and influence exists
        if (horizontalInfluence != 0)
        {
            if (curMovementCollection.MoveData != null)
            {
                //Update state from null
                if (curMoveState == MovementType.Null)
                    SetCurrentMoveState(MovementType.Move);

                //Check if in move state
                if (curMoveState == MovementType.Move)
                {
                    CheckTurnAround();
                    UpdateGroundAcceleration();
                }
            }
        }

        else
            UpdateGroundDecceleration(rb);        
    }


    /// <summary>
    /// Update velocity while on the ground to speed up
    /// </summary>
    private void UpdateGroundAcceleration()
    {
        //Ground slope
        if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
        {            
            //Drag
            sceneObject.CoreRigidBody.drag = 0;

            //Cap Velocity based on horizontal influence
            float targetVelocity = curMovementCollection.GetMaxXVelocity() * Mathf.Abs(horizontalInfluence);

            //Update velocity based on slope
            sceneObject.CoreRigidBody.velocity += slope * Mathf.Abs(horizontalInfluence) * curMovementCollection.GetGroundedXAcceleration() * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if (sceneObject.CoreRigidBody.velocity.magnitude > targetVelocity)
            {
                sceneObject.CoreRigidBody.velocity = slope * targetVelocity;
            }
            
            //TODO:
            //sceneObj.AnimationStateHandler.SetFloatPerameter("Velocity", Mathf.Abs(sceneObj.CoreRigidBody.velocity.x) / CurMovementCollection.GetMaxXVelocity());
        }
    }


    /// <summary>
    /// Update velocity while on the ground to slow down
    /// </summary>
    private void UpdateGroundDecceleration(Rigidbody rb)
    {
        if (sceneObject.CoreRigidBody.velocity.x != 0)
        {
            Vector3 dragForce = rb.velocity.normalized * curMovementCollection.GetGroundedXDeceleration();
            rb.velocity -= dragForce * Time.fixedDeltaTime;

            if ((sceneObject.IsFacingRightDirection() && rb.velocity.x <= 0) ||
                (!sceneObject.IsFacingRightDirection() && rb.velocity.x >= 0))
            {
                rb.velocity = Vector3.zero;
                sceneObject.AnimationHandler.EndAnimation(curMoveData.Animation);
            }

            //TODO:
            //sceneObj.AnimationStateHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());
        }        
    }    


    private void UpdateSlidingMovement(Rigidbody rb)
    {
        //Ground slope
        if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
        {
            //Reverse slope downward
            if (slope.y > 0)
                slope *= -1;

            rb.velocity += slope * slidingAcceleration;

            if (rb.velocity.y < 0 && rb.velocity.magnitude > slidingMaxVelocity)
            {
                rb.velocity = slope * slidingMaxVelocity;
            }
        }
    }

    #endregion


    #region Air Movement

    /// <summary>
    /// Update velocity while in the air
    /// </summary>
    private void UpdateAirMovement(Rigidbody rb)
    {
        if (sceneObject.ActionStateHandler.CurActionState < ActionState.HitStun)
        {
            if (horizontalInfluence > 0)
            {
                if (curMovementCollection.MoveData != null &&
                   (curMoveState == MovementType.FreeFall || curMoveState == MovementType.AirJump))
                {
                    UpdateAirAcceleration(rb);

                    //Vertical Movement
                    if (verticalInfluence < 0f && rb.velocity.y > -MaxYVelocity)
                    {
                        rb.velocity += transform.up * verticalInfluence * fastFallAcceleration * Time.fixedDeltaTime;
                    }
                }
            }

            else
                UpdateAirDeceleration(rb);
        }
    }


    /// <summary>
    /// Update velocity in the air to speed up
    /// </summary>
    /// <param name="rb"></param>
    private void UpdateAirAcceleration(Rigidbody rb)
    {
        //Apply Movement based on influence
        if (horizontalInfluence != 0)
        {
            //Cap Velocity based on horizontal influence
            float targetXVelocity = curMovementCollection.GetMaxXVelocity() * horizontalInfluence;

            if ((horizontalInfluence > 0 && rb.velocity.x < targetXVelocity) ||
                (horizontalInfluence < 0 && rb.velocity.x > targetXVelocity))
            {
                rb.velocity += Vector3.right * horizontalInfluence * curMovementCollection.GetArialXAcceleration() * Time.fixedDeltaTime;
            }
        }
    }


    /// <summary>
    /// Update velocity in the air to slow down
    /// </summary>
    /// <param name="rb"></param>
    private void UpdateAirDeceleration(Rigidbody rb)
    {
        float targetXVelocity = curMovementCollection.GetMaxXVelocity();

        if (horizontalInfluence > 0)
            targetXVelocity *= horizontalInfluence;

        if (rb.velocity.x > targetXVelocity)
            rb.velocity -= Vector3.right * curMovementCollection.GetArialXDeceleration() * Time.fixedDeltaTime;

        else if (rb.velocity.x < -targetXVelocity)
            rb.velocity += Vector3.right * curMovementCollection.GetArialXDeceleration() * Time.fixedDeltaTime;
    }

    #endregion

    #endregion


    #region Perform Jump

    /// <summary>
    /// Jump action based on input value <br/>
    /// Cant jump while jumping or landing <br/>
    /// Cant jump while attacking / in hitstun
    /// Input value ranges between (0, 1) 
    /// </summary>
    /// <param name="inputInfluence"></param>
    public void PerformJump(float inputInfluence)
    {       
        if (curMoveState != MovementType.Jump && curMoveState != MovementType.Landing)
        {
            jumpInfluence = Mathf.Clamp01(inputInfluence);

            switch (sceneObject.GroundedState)
            {
                case GroundedState.Grounded:
                    if (curMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jump))
                    {
                        SetCurrentMoveState(MovementType.Jump);
                    }
                    break;

                case GroundedState.Sliding:
                    if (curMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData slideJump))
                    {
                        SetCurrentMoveState(MovementType.Jump);
                    }
                    break;

                case GroundedState.Airborn:
                    if (curMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
                    {
                        if (numJumpsPerformed < ((AirJumpData)airJump).JumpsAvailable)
                        {
                            SetCurrentMoveState(MovementType.AirJump);
                        }
                    }
                    break;
            }
        }        
    }


    /// <summary>
    /// Velocity applied to rb based on jumpInfluence
    /// </summary>
    private void ApplyJumpInfluence(JumpData jumpData)
    {        
        sceneObject.CoreRigidBody.velocity = new Vector3(sceneObject.CoreRigidBody.velocity.x, jumpData.JumpVelocity * jumpInfluence, sceneObject.CoreRigidBody.velocity.z);
        numJumpsPerformed++;
    }


    /// <summary>
    /// Velocity applied to rb based on jumpInfluence
    /// </summary>
    /// <param name="airJumpData"></param>
    private void ApplyAirJumpInfluence(AirJumpData airJumpData)
    {

        sceneObject.CoreRigidBody.velocity = new Vector3(sceneObject.CoreRigidBody.velocity.x, airJumpData.AirJumpVelocity * jumpInfluence, sceneObject.CoreRigidBody.velocity.z);
        numJumpsPerformed++;

        CheckTurnAround();
    }


    //TODO: Might remove
    //private void SlidingJumpAction(JumpData jumpData, float jumpInfluence)
    //{
    //    if (sceneObj.TryGetSlopeAngle(out Vector3 slopeAngle))
    //    {

    //        Vector3 normal = Vector3.Cross(slopeAngle, -transform.forward).normalized;
    //        sceneObj.CoreRigidBody.velocity = normal * (jumpData.JumpVelocity * jumpInfluence);
    //        numJumpsPerformed++;

    //        CheckTurnAround();
    //    }
    //}


    #endregion


    #region Perform Landing

    /// <summary>
    /// Performs any actions associated with landing
    /// </summary>
    private void PerformLanding()
    {
        SetCurrentMoveState(MovementType.Landing);

        //Reset jumps
        numJumpsPerformed = 0;
    }

    #endregion


    #region Animation

    /// <summary>
    /// Move animation started
    /// </summary>
    /// <param name="clip"></param>
    private void OnAnimationStarted(AnimationClip clip)
    {
        if (curMovementCollection.TryGetMovementFromAnimation(clip, out MovementData moveData))
        {
            curMoveData = moveData;

            foreach (AnimationTrigger trigger in moveData.Triggers)
                trigger.Reset();
           
            switch (moveData.Type)
            {
                case MovementType.Jump:
                    ApplyJumpInfluence((JumpData)moveData);
                    break;

                case MovementType.AirJump:
                    ApplyAirJumpInfluence((AirJumpData)moveData);
                    break;
            }

        }
    }


    /// <summary>
    /// Move Animation Ended
    /// </summary>
    /// <param name="clip"></param>
    private void OnAnimationEnded(AnimationClip clip)
    {
        if (curMovementCollection.TryGetMovementFromAnimation(clip, out MovementData moveData))
        {
            switch (moveData.Type)
            {
                case MovementType.Jump:
                case MovementType.AirJump:
                    SetCurrentMoveState(MovementType.FreeFall);
                    //sceneObj.AttackInputHandler.ExecuteBufferedAttack();
                    break;

                default:
                    SetCurrentMoveState(MovementType.Null);            
                    break;

            }
        }
    }

    #endregion
}
