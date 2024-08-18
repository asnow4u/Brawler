using System;
using System.Collections.Generic;
using UnityEngine;


public class MovementInputHandler : MonoBehaviour
{
    const ActionState MOVESTATE = ActionState.Moving;

    //Movement State Data
    [Header("State")]
    [SerializeField] private MovementType curMoveState = MovementType.Move;
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
   
    //SceneObject
    private SceneObject sceneObj => GetComponent<SceneObject>();
    private Collider collider => GetComponent<Collider>();


    //Getters
    public MovementType CurMoveState => curMoveState;    


    //Events
    public event Action<MovementCollection> MovementCollectionChangedEvent;
    public event Action<MovementType> MoveStateChangedEvent;

    #region Initialize

    public void Setup()
    {
        sceneObj.GroundedStateChangeEvent += OnGroundedStateChanged;
        sceneObj.AnimationHandler.OnAnimationUpdateEvent += OnAnimationUpdate;

        OnWeaponChanged(null);        
    }

    #endregion

    #region Events

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


    /// <summary>
    /// Animation trigger event
    /// </summary>
    /// <param name="animationState"></param>
    /// <param name="triggerType"></param>
    private void OnAnimationUpdate(string animationState, AnimationTrigger.Type triggerType)
    {
        if (CurMovementCollection.TryGetMovementFromAnimation(animationState, out MovementData move))
        {
            switch (triggerType)
            {
                case AnimationTrigger.Type.Start:                    
                    OnMovementAnimationStarted(animationState, move);
                    break;

                case AnimationTrigger.Type.End:
                    OnMovementAnimationEnded(animationState, move.Type);
                    break;
            }
        }
    }


    /// <summary>
    /// Movement animation started
    /// </summary>
    /// <param name="animationState"></param>
    /// <param name="type"></param>
    private void OnMovementAnimationStarted(string animationState, MovementData moveData)
    {
        curMoveData = moveData;

        foreach (AnimationTrigger trigger in moveData.Triggers)
            trigger.Reset();
    }



    private void OnMovementAnimationEnded(string animationState, MovementType type) 
    {        
        Debug.Log("MOVEMENT: Animation finished for " + type);

        switch (type)
        {
            case MovementType.Jump:
            case MovementType.AirJump:
                SetCurrentMoveState(MovementType.FreeFall);
                sceneObj.AttackInputHandler.ExecuteBufferedAttack();
                break;

            case MovementType.Landing:
                SetCurrentMoveState(MovementType.Move);
                sceneObj.AttackInputHandler.ExecuteBufferedAttack();
                break;
        }
    }

    #endregion


    private void SetCurrentMoveState(MovementType moveState)
    {
        Debug.Log("MOVEMENT: CurMoveState => " + moveState);
        curMoveState = moveState;

        MoveStateChangedEvent?.Invoke(moveState);
    }


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


    #region Perform Movement

    /// <summary>
    /// Horizontal movement based on input value
    /// Input value ranges between (-1, 1) 
    /// Input Value determines how much of the curCollection moveSpeed should be applied
    /// </summary>
    /// <param name="moveInfluence"></param>
    public void PerformMovement(Vector2 moveInfluence)
    {       
        //Check that moveData exists
        if (CurMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData movement))
        {          
            horizontalInfluence = Mathf.Clamp(moveInfluence.x, -1, 1);
            verticalInfluence = Mathf.Clamp(moveInfluence.y, -1, 1);                             
        }                           
    }


    /// <summary>
    /// Jump action based on input value <br/>
    /// Cant jump while jumping or landing <br/>
    /// Cant jump while attacking
    /// Input value ranges between (0, 1) 
    /// </summary>
    /// <param name="jumpInfluence"></param>
    public void PerformJump(float jumpInfluence)
    {
        if (curMoveState != MovementType.Jump && curMoveState != MovementType.Landing)
        {
            if (sceneObj.AnimationStateHandler.IsStatePossible(MOVESTATE))
            {            
                jumpInfluence = Mathf.Clamp01(jumpInfluence);

                switch (sceneObj.GroundedState)
                {
                    case GroundedState.Grounded:
                        if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jump))
                        {                        
                            VerticalJumpAction((JumpData)jump, jumpInfluence);
                        }
                        break;

                    case GroundedState.Sliding:
                        if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData slideJump))
                        {
                            SlidingJumpAction((JumpData)slideJump, jumpInfluence);
                        }
                        break;

                    case GroundedState.Airborn:
                        if (CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
                        {
                            if (numJumpsPerformed < ((AirJumpData)airJump).JumpsAvailable)
                            {
                                AirJumpAction((AirJumpData)airJump, jumpInfluence);
                            }
                        }
                        break;
                }
            }
        }
    }


    /// <summary>
    /// Performs any actions associated with landing
    /// </summary>
    private void PerformLanding()
    {
        //Reset jumps
        numJumpsPerformed = 0;

        SetCurrentMoveState(MovementType.Landing);
        PlayTransitionAnimation(MovementType.Landing);
    }

    #endregion


    #region Movement Actions

    /// <summary>
    /// Velocity applied to rb based on jumpInfluence and current jumpVelocity
    /// </summary>
    /// <param name="jumpData"></param>
    /// <param name="jumpInfluence"></param>
    private void VerticalJumpAction(JumpData jumpData, float jumpInfluence)
    {
        SetCurrentMoveState(MovementType.Jump);

        sceneObj.CoreRigidBody.velocity = new Vector3(sceneObj.CoreRigidBody.velocity.x, jumpData.JumpVelocity * jumpInfluence, sceneObj.CoreRigidBody.velocity.z);
        numJumpsPerformed++;

        PlayTransitionAnimation(jumpData.Type);        
    }


    /// <summary>
    /// Velocity applied based on the normal of the environment the sceneobject is slideing on.<br/>
    /// Velocity is based on the jumpInfluence and current jump velocity.
    /// </summary>
    /// <param name="jumpData"></param>
    /// <param name="jumpInfluence"></param>
    private void SlidingJumpAction(JumpData jumpData, float jumpInfluence)
    {
        if (sceneObj.TryGetSlopeAngle(out Vector3 slopeAngle))
        {
            SetCurrentMoveState(MovementType.Jump);

            Vector3 normal = Vector3.Cross(slopeAngle, -transform.forward).normalized;
            sceneObj.CoreRigidBody.velocity = normal * (jumpData.JumpVelocity * jumpInfluence);
            numJumpsPerformed++;

            CheckTurnAround();

            PlayTransitionAnimation(jumpData.Type);
        }
    }


    private void AirJumpAction(AirJumpData airJumpData, float jumpInfluence)
    {
        SetCurrentMoveState(MovementType.AirJump);

        sceneObj.CoreRigidBody.velocity = new Vector3(sceneObj.CoreRigidBody.velocity.x, airJumpData.AirJumpVelocity * jumpInfluence, sceneObj.CoreRigidBody.velocity.z);
        numJumpsPerformed++;

        CheckTurnAround();

        PlayTransitionAnimation(airJumpData.Type);                   
    }

    #endregion


    public void UpdateMovement()
    {             
        if (sceneObj.AnimationStateHandler.CurActionState < ActionState.HitStun)
        {
            //Check Movement Action
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
    }


    #region Ground Movement

    private void UpdateGroundMovement(Rigidbody rb)
    {
        if (curMoveState == MovementType.Move)
        {
            if (sceneObj.AnimationStateHandler.IsStatePossible(MOVESTATE) && 
                curMoveState == MovementType.Move)
            {
                CheckTurnAround();

                if (CurMovementCollection.ContainsMovementType(MovementType.Move))
                    UpdateGroundAcceleration();
            }

            UpdateGroundDecceleration(rb);        
        }
    }


    /// <summary>
    /// Update velocity while on the ground
    /// Based on current movement collection and horizontal influence try to speed up, slow down or stop
    /// </summary>
    private void UpdateGroundAcceleration()
    {      
        //Ground slope
        if (sceneObj.TryGetSlopeAngle(out Vector3 slope))
        {
            //Apply Movement based on influence
            if (horizontalInfluence != 0)
            {
                //Animation
                PlayMoveAnimation(MovementType.Move);

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

                sceneObj.AnimationStateHandler.SetFloatPerameter("Velocity", Mathf.Abs(sceneObj.CoreRigidBody.velocity.x) / CurMovementCollection.GetMaxXVelocity());
            }                        
        }
    }


    private void UpdateGroundDecceleration(Rigidbody rb)
    {
        if (horizontalInfluence == 0)
        {
            if (sceneObj.CoreRigidBody.velocity.x != 0)
            {
                Vector3 dragForce = rb.velocity.normalized * CurMovementCollection.GetGroundedXDeceleration();
                rb.velocity -= dragForce * Time.fixedDeltaTime;

                if ((sceneObj.IsFacingRightDirection() && rb.velocity.x <= 0) ||
                    (!sceneObj.IsFacingRightDirection() && rb.velocity.x >= 0))
                {
                    rb.velocity = Vector3.zero;                    
                    sceneObj.AnimationStateHandler.EndCurrentAnimation(MOVESTATE);
                }

                sceneObj.AnimationStateHandler.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / CurMovementCollection.GetMaxXVelocity());
            }
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

    /// <summary>
    /// Update velocity while in the air
    /// </summary>
    private void UpdateAirMovement(Rigidbody rb)
    {
        if (sceneObj.AnimationStateHandler.CurActionState < ActionState.HitStun)
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

            UpdateAirDeceleration(rb);
        }
    }


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


    #region Animation

    //TODO: Look at how animations are played. 
    //Move blendtree makes this not work so great...
    /// <summary>
    /// Play move animation based on moveType
    /// </summary>
    /// <param name="moveType"></param>
    private void PlayMoveAnimation(MovementType moveType)
    {
        if (CurMovementCollection.TryGetMovementByType(moveType, out MovementData move))
        {            
            //NOTE: Need to call name for blend tree
            if (move.Type == MovementType.Move)
            {                
                string userName = gameObject.name;
                string weaponName = "Base"; //TODO: Need to Implement with EquipmentHandler
                string clipName = moveType.ToString();

                string animationName = userName + weaponName + clipName;
                sceneObj.AnimationStateHandler.PlayAnimation(new AnimationStateData(animationName, MOVESTATE, move.Triggers.ToArray()));
            }

            else
            {
                sceneObj.AnimationStateHandler.PlayAnimation(new AnimationStateData(move.Animation.name, MOVESTATE, move.Triggers.ToArray()));
            }
        }
    }


    /// <summary>
    /// Play transition based animation <\br>
    /// Jump and landing 
    /// </summary>
    /// <param name="moveType"></param>
    private void PlayTransitionAnimation(MovementType moveType)
    {
        //if (CurMovementCollection.TryGetMovementByType(moveType, out MovementData move))
        //{
        //    sceneObj.AnimationStateHandler.PlayAnimation(new AnimationStateData(move.Animation.name, ActionState.MoveTransition, move.Triggers.ToArray()));
        //}
    }

    #endregion
}
