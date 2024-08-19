using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;


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

    //Movement Collection
    [Header("Collection")]
    public MovementCollection BaseMovementCollection;
    public MovementCollection CurMovementCollection;

    //Infulence
    private float horizontalInfluence;
    private float verticalInfluence;
    private float jumpInfluence;
   
    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();

    //Events
    public event Action<MovementCollection> MovementCollectionChangedEvent;
    public event Action<MovementType> MoveStateChangedEvent;

    #region Getters

    public MovementType CurMoveState => curMoveState;    

    #endregion


    #region Initialize

    public void Setup()
    {
        SetupEvents();
        OnWeaponChanged(null);        
    }

    #endregion


    #region Events

    private void SetupEvents()
    {
        sceneObj.GroundedStateChangeEvent += OnGroundedStateChanged;
        sceneObj.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
        sceneObj.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
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
                break;

        }
    }


    private void OnWeaponChanged(Weapon weapon)
    {
        if (weapon == null)        
            CurMovementCollection = BaseMovementCollection;       

        else
            CurMovementCollection = weapon.MovementCollection;

        MovementCollectionChangedEvent?.Invoke(CurMovementCollection);
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
                if (sceneObj.ActionStateHandler.TryChangeState(MOVESTATE))
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

                sceneObj.ActionStateHandler.ChangeState(MOVESTATE);
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
        if (sceneObj.IsFacingRightDirection() && horizontalInfluence < 0)
        {
            sceneObj.TurnAround();

            if (sceneObj.CoreRigidBody.velocity.x > 0)
            {
                switch (sceneObj.GroundedState)
                {
                    case GroundedState.Grounded:
                    case GroundedState.Sliding:
                        if (sceneObj.TryGetSlopeAngle(out Vector3 slope))
                        {
                            sceneObj.CoreRigidBody.velocity = slope * sceneObj.CoreRigidBody.velocity.magnitude;
                        }
                        break;

                    case GroundedState.Airborn:
                        sceneObj.CoreRigidBody.velocity = new Vector3(sceneObj.CoreRigidBody.velocity.x * -1, sceneObj.CoreRigidBody.velocity.y, sceneObj.CoreRigidBody.velocity.z);
                        break;
                }
            }
        }

        else if (!sceneObj.IsFacingRightDirection() && horizontalInfluence > 0)
        {
            sceneObj.TurnAround();

            switch (sceneObj.GroundedState)
            {
                case GroundedState.Grounded:
                case GroundedState.Sliding:
                    if (sceneObj.TryGetSlopeAngle(out Vector3 slope))
                    {
                        sceneObj.CoreRigidBody.velocity = slope * sceneObj.CoreRigidBody.velocity.magnitude;
                    }
                    break;

                case GroundedState.Airborn:
                    sceneObj.CoreRigidBody.velocity = new Vector3(sceneObj.CoreRigidBody.velocity.x * -1, sceneObj.CoreRigidBody.velocity.y, sceneObj.CoreRigidBody.velocity.z);
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
        if (CurMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData movement))
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
        switch (sceneObj.GroundedState)
        {
            case GroundedState.Grounded:
                UpdateGroundMovement(sceneObj.CoreRigidBody);
                break;

            case GroundedState.Sliding:
                UpdateSlidingMovement(sceneObj.CoreRigidBody);
                break;

            case GroundedState.Airborn:
                UpdateAirMovement(sceneObj.CoreRigidBody);
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
            if (CurMovementCollection.ContainsMovementType(MovementType.Move))
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
        if (sceneObj.TryGetSlopeAngle(out Vector3 slope))
        {            
            //Drag
            sceneObj.CoreRigidBody.drag = 0;

            //Cap Velocity based on horizontal influence
            float targetVelocity = CurMovementCollection.GetMaxXVelocity() * Mathf.Abs(horizontalInfluence);

            //Update velocity based on slope
            sceneObj.CoreRigidBody.velocity += slope * Mathf.Abs(horizontalInfluence) * CurMovementCollection.GetGroundedXAcceleration() * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if (sceneObj.CoreRigidBody.velocity.magnitude > targetVelocity)
            {
                sceneObj.CoreRigidBody.velocity = slope * targetVelocity;
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
        if (sceneObj.CoreRigidBody.velocity.x != 0)
        {
            Vector3 dragForce = rb.velocity.normalized * CurMovementCollection.GetGroundedXDeceleration();
            rb.velocity -= dragForce * Time.fixedDeltaTime;

            if ((sceneObj.IsFacingRightDirection() && rb.velocity.x <= 0) ||
                (!sceneObj.IsFacingRightDirection() && rb.velocity.x >= 0))
            {
                rb.velocity = Vector3.zero;
                sceneObj.AnimationHandler.EndAnimation(curMoveData.Animation);
            }

            //TODO:
            //sceneObj.AnimationStateHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());
        }
        
    }


    private void UpdateSlidingMovement(Rigidbody rb)
    {
        //Ground slope
        if (sceneObj.TryGetSlopeAngle(out Vector3 slope))
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
        if (sceneObj.ActionStateHandler.CurActionState < ActionState.HitStun)
        {
            if (horizontalInfluence > 0)
            {
                if (CurMovementCollection.ContainsMovementType(MovementType.Move) &&
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
            float targetXVelocity = CurMovementCollection.GetMaxXVelocity() * horizontalInfluence;

            if ((horizontalInfluence > 0 && rb.velocity.x < targetXVelocity) ||
                (horizontalInfluence < 0 && rb.velocity.x > targetXVelocity))
            {
                rb.velocity += Vector3.right * horizontalInfluence * CurMovementCollection.GetArialXAcceleration() * Time.fixedDeltaTime;
            }
        }
    }


    /// <summary>
    /// Update velocity in the air to slow down
    /// </summary>
    /// <param name="rb"></param>
    private void UpdateAirDeceleration(Rigidbody rb)
    {
        float targetXVelocity = CurMovementCollection.GetMaxXVelocity();

        if (horizontalInfluence > 0)
            targetXVelocity *= horizontalInfluence;

        if (rb.velocity.x > targetXVelocity)
            rb.velocity -= Vector3.right * CurMovementCollection.GetArialXDeceleration() * Time.fixedDeltaTime;

        else if (rb.velocity.x < -targetXVelocity)
            rb.velocity += Vector3.right * CurMovementCollection.GetArialXDeceleration() * Time.fixedDeltaTime;
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

            switch (sceneObj.GroundedState)
            {
                case GroundedState.Grounded:
                    if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jump))
                    {
                        SetCurrentMoveState(MovementType.Jump);
                    }
                    break;

                case GroundedState.Sliding:
                    if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData slideJump))
                    {
                        SetCurrentMoveState(MovementType.Jump);
                    }
                    break;

                case GroundedState.Airborn:
                    if (CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
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
        sceneObj.CoreRigidBody.velocity = new Vector3(sceneObj.CoreRigidBody.velocity.x, jumpData.JumpVelocity * jumpInfluence, sceneObj.CoreRigidBody.velocity.z);
        numJumpsPerformed++;
    }


    /// <summary>
    /// Velocity applied to rb based on jumpInfluence
    /// </summary>
    /// <param name="airJumpData"></param>
    private void ApplyAirJumpInfluence(AirJumpData airJumpData)
    {

        sceneObj.CoreRigidBody.velocity = new Vector3(sceneObj.CoreRigidBody.velocity.x, airJumpData.AirJumpVelocity * jumpInfluence, sceneObj.CoreRigidBody.velocity.z);
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
        if (CurMovementCollection.TryGetMovementFromAnimation(clip.name, out MovementData moveData))
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
        if (CurMovementCollection.TryGetMovementFromAnimation(clip.name, out MovementData moveData))
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
