using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;


public enum MovementType { Null, Move, AirMove, Jump, AirJump, Landing }

public class MovementInputHandler : MonoBehaviour
{
    const ActionState MOVESTATE = ActionState.Moving;

    //Movement State Data
    [Header("State")]
    [SerializeField] private MovementType curMoveState;
    private MovementData curMoveData;

    //Jump Properties
    private int airJumpsPerformed;

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

    [Header("Influence")]
    [Range(-1, 1)]
    [SerializeField] private float horizontalInfluence;

    [Range(-1, 1)]
    [SerializeField] private float verticalInfluence;

    [SerializeField] private float jumpInfluence;
   
    //SceneObject
    private SceneObject sceneObject;

    //Events
    public event Action<MovementCollection> MovementCollectionChangedEvent;
    public event Action<MovementType> MoveStateChangedEvent;


    #region Getters

    public MovementType CurMoveState => curMoveState;    
    public MovementCollection CurMovementCollection => curMovementCollection;  
    public float HorizontalInfluence => horizontalInfluence;

    #endregion


    #region Initialize

    public void Setup()
    {
        sceneObject = GetComponent<SceneObject>();
        SetupEventListeners();

        Debug.Assert(baseMovementCollection != null, "BaseMovementCollection is NULL", this);
        Debug.Assert(baseMovementCollection.MoveData != null, "BaseMovementCollection Move Data is Null", this);
        Debug.Assert(baseMovementCollection.AirMoveData != null, "BaseMovementCollection AirMove Data is Null", this);

        curMovementCollection = baseMovementCollection;
    }


    public void Initialize()
    {
        
    }

    #endregion


    #region Events

    private void SetupEventListeners()
    {
        sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;
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
                PerformLanding();
                break;

            case GroundedState.Airborn:
                TrySetCurrentMoveState(MovementType.Null);
                break;

        }
    }


    /// <summary>
    /// Move Animation Ended reset current moveState
    /// </summary>
    /// <param name="clip"></param>
    private void OnAnimationEnded(AnimationClip clip)
    {
        if (curMovementCollection.TryGetMovementFromAnimation(clip, out MovementData moveData))
        {
            if (moveData.Type != MovementType.Move && moveData.Type != MovementType.AirMove)
                TrySetCurrentMoveState(MovementType.Null);
        }

        //End movement after dash attack
        if (sceneObject.AttackInputHandler.CurAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
        {
            if (attackData.Type == AttackType.Dash)
                TrySetCurrentMoveState(MovementType.Null);
        }
    }

    #endregion


    #region MoveState

    private bool TrySetCurrentMoveState(MovementType moveState)
    {
        if (moveState != MovementType.Null)
        {
            if (moveState != MovementType.Landing)
            {
                //Change State
                if (sceneObject.ActionStateHandler.TryChangeState(MOVESTATE))
                {              
                    curMoveState = moveState;
                    MoveStateChangedEvent?.Invoke(moveState);
                    return true;
                }
            }

            //Landing
            else
            {
                //TODO: Check for hitstun?
                sceneObject.ActionStateHandler.ChangeState(MOVESTATE);
                curMoveState = moveState;
                MoveStateChangedEvent?.Invoke(moveState);
                return true;
            }
        }

        else
        {
            curMoveState = MovementType.Null;
            curMoveData = null;
            MoveStateChangedEvent?.Invoke(moveState);
            return true;
        }

        return false;
    }

    #endregion


    #region Turn Around

    private void CheckTurnAround()
    {
        if (sceneObject.IsFacingRightDirection() && horizontalInfluence < 0)
        {
            sceneObject.TurnAround();

            if (sceneObject.CoreRigidBody.linearVelocity.x > 0)
            {
                switch (sceneObject.CurGroundedState)
                {
                    case GroundedState.Grounded:
                    case GroundedState.Sliding:
                        if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                        {
                            sceneObject.CoreRigidBody.linearVelocity = slope * sceneObject.CoreRigidBody.linearVelocity.magnitude;
                        }
                        break;

                    case GroundedState.Airborn:
                        sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x * -1, sceneObject.CoreRigidBody.linearVelocity.y, sceneObject.CoreRigidBody.linearVelocity.z);
                        break;
                }
            }
        }

        else if (!sceneObject.IsFacingRightDirection() && horizontalInfluence > 0)
        {
            sceneObject.TurnAround();

            switch (sceneObject.CurGroundedState)
            {
                case GroundedState.Grounded:
                case GroundedState.Sliding:
                    if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                    {
                        sceneObject.CoreRigidBody.linearVelocity = slope * sceneObject.CoreRigidBody.linearVelocity.magnitude;
                    }
                    break;

                case GroundedState.Airborn:
                    sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x * -1, sceneObject.CoreRigidBody.linearVelocity.y, sceneObject.CoreRigidBody.linearVelocity.z);
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
        horizontalInfluence = Mathf.Clamp(inputInfluence.x, -1, 1);
        verticalInfluence = Mathf.Clamp(inputInfluence.y, -1, 1);                                                  
    }


    /// <summary>
    /// Update movement based on grounded status
    /// </summary>
    public void UpdateMovement()
    {
        //Grounded Movement
        if (sceneObject.CurGroundedState == GroundedState.Grounded)
            UpdateGroundedMovement();

        //Air Movement
        else
            UpdateAirialMovement();
    }

    
    /// <summary>
    /// Update movement on the ground
    /// </summary>
    private void UpdateGroundedMovement()
    {
        if (horizontalInfluence != 0)
        {
            if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking)
                UpdateGroundDecceleration(8); //TODO: Calculate the nessisary decceleration given animation time for attack and current velocity

            else if (curMoveState == MovementType.Null)
                TrySetCurrentMoveState(MovementType.Move);

            else if (curMoveState == MovementType.Move)
            {
                CheckTurnAround();
                UpdateGroundAcceleration(curMovementCollection.GetGroundedXAcceleration());
            }
        }

        else
            UpdateGroundDecceleration(curMovementCollection.GetGroundedXDeceleration());
    }


    /// <summary>
    /// Update movement in the air
    /// </summary>
    private void UpdateAirialMovement()
    {
        if (horizontalInfluence != 0)
        {
            if (curMoveState == MovementType.Null)
                TrySetCurrentMoveState(MovementType.AirMove);

            else if (curMoveState == MovementType.AirMove ||
                curMoveState == MovementType.Jump ||
                curMoveState == MovementType.AirJump)
            {
                UpdateAirAcceleration(curMovementCollection.GetArialXAcceleration());
            }
        }

        else
            UpdateAirDeceleration(curMovementCollection.GetAerialXDeceleration());        
    }


    #region Acceleration   

    /// <summary>
    /// Update velocity while on the ground to speed up
    /// </summary>
    private void UpdateGroundAcceleration(float acceleration)
    {
        //Ground slope
        if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
        {
            //Drag
            sceneObject.CoreRigidBody.linearDamping = 0;

            //Cap Velocity based on horizontal influence
            float targetXVelocity = curMovementCollection.GetGroundedMaxXVelocity() * Mathf.Abs(horizontalInfluence);

            //Update velocity based on slope
            sceneObject.CoreRigidBody.linearVelocity += slope * Mathf.Abs(horizontalInfluence) * acceleration * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if (sceneObject.CoreRigidBody.linearVelocity.magnitude > targetXVelocity)
                sceneObject.CoreRigidBody.linearVelocity = slope * targetXVelocity;
        }
    }


    /// <summary>
    /// Update velocity in the air to speed up
    /// </summary>
    /// <param name="rb"></param>
    private void UpdateAirAcceleration(float acceleration)
    {
        //Cap Velocity based on horizontal influence
        float targetXVelocity = curMovementCollection.GetAerialMaxVelocity() * horizontalInfluence;

        sceneObject.CoreRigidBody.linearVelocity += Vector3.right * horizontalInfluence * acceleration * Time.fixedDeltaTime;

        //Cant exceed target velocity
        if ((horizontalInfluence > 0 && sceneObject.CoreRigidBody.linearVelocity.x > targetXVelocity) ||
            (horizontalInfluence < 0 && sceneObject.CoreRigidBody.linearVelocity.x < targetXVelocity))
        {
            sceneObject.CoreRigidBody.linearVelocity = new Vector3(targetXVelocity, sceneObject.CoreRigidBody.linearVelocity.y, sceneObject.CoreRigidBody.linearVelocity.z);
        }


        //TODO: Want to apply velocity change than check if its over for more consistant values
        //Vertical Movement
        if (verticalInfluence < 0f && sceneObject.CoreRigidBody.linearVelocity.y > -MaxYVelocity)
        {
            sceneObject.CoreRigidBody.linearVelocity += transform.up * verticalInfluence * fastFallAcceleration * Time.fixedDeltaTime;
        }
    }

    #endregion


    #region Decceleration

    /// <summary>
    /// Update velocity while on the ground to slow down
    /// </summary>
    private void UpdateGroundDecceleration(float deccelerationValue)
    {
        //Dont deccelerate when jumping from ground
        if (curMoveState != MovementType.Jump)
        {
            if (sceneObject.CoreRigidBody.linearVelocity.x != 0)
            {
                Vector3 dragForce = sceneObject.CoreRigidBody.linearVelocity.normalized * deccelerationValue;
                sceneObject.CoreRigidBody.linearVelocity -= dragForce * Time.fixedDeltaTime;

                if ((sceneObject.IsFacingRightDirection() && sceneObject.CoreRigidBody.linearVelocity.x <= 0) ||
                    (!sceneObject.IsFacingRightDirection() && sceneObject.CoreRigidBody.linearVelocity.x >= 0))
                {
                    sceneObject.CoreRigidBody.linearVelocity = Vector3.zero;
                
                    if (curMoveData != null)
                        sceneObject.AnimationHandler.EndAnimation(curMoveData.Animation);
                
                    TrySetCurrentMoveState(MovementType.Null);
                }
            }
        }
    }


    /// <summary>
    /// Update velocity in the air to slow down
    /// </summary>
    /// <param name="rb"></param>
    private void UpdateAirDeceleration(float deccelerationValue)
    {
        float targetXVelocity = curMovementCollection.GetAerialMaxVelocity();

        if (horizontalInfluence > 0)
            targetXVelocity *= horizontalInfluence;

        if (sceneObject.CoreRigidBody.linearVelocity.x > targetXVelocity)
            sceneObject.CoreRigidBody.linearVelocity -= Vector3.right * deccelerationValue * Time.fixedDeltaTime;

        else if (sceneObject.CoreRigidBody.linearVelocity.x < -targetXVelocity)
            sceneObject.CoreRigidBody.linearVelocity += Vector3.right * deccelerationValue * Time.fixedDeltaTime;
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

            switch (sceneObject.CurGroundedState)
            {
                case GroundedState.Grounded:
                    if (curMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData groundJumpData))
                        PerformGroundedJump((JumpData)groundJumpData);
                    
                    break;

                case GroundedState.Airborn:
                    if (curMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJumpData))
                    {
                        if (airJumpsPerformed < ((AirJumpData)airJumpData).JumpsAvailable)
                            PerformAirJump((AirJumpData)airJumpData);                        
                    }

                    break;
            }
        }        
    }


    /// <summary>
    /// Velocity applied to rb based on jumpInfluence
    /// </summary>
    private void PerformGroundedJump(JumpData jumpData)
    {
        if (TrySetCurrentMoveState(MovementType.Jump))
        {
            sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x, jumpData.JumpVelocity * jumpInfluence, sceneObject.CoreRigidBody.linearVelocity.z);
        }
    }


    /// <summary>
    /// Velocity applied to rb based on jumpInfluence
    /// </summary>
    /// <param name="airJumpData"></param>
    private void PerformAirJump(AirJumpData airJumpData)
    {
        if (TrySetCurrentMoveState(MovementType.AirJump))
        {
            CheckTurnAround();
            sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x, airJumpData.AirJumpVelocity * jumpInfluence, sceneObject.CoreRigidBody.linearVelocity.z);
            airJumpsPerformed++;
        }
    }

    #endregion


    #region Perform Landing

    /// <summary>
    /// Performs any actions associated with landing
    /// </summary>
    private void PerformLanding()
    {
        TrySetCurrentMoveState(MovementType.Landing);

        //Reset jumps
        airJumpsPerformed = 0;
    }

    #endregion
}
