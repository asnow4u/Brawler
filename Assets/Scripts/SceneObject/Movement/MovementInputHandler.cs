using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;


public class MovementInputHandler : MonoBehaviour
{
    const ActionState.State moveState = ActionState.State.Moving;

    //Movement State Data
    private MovementType curMoveState = MovementType.Move;
    private string curMoveAnimationState;

    //Movement Data
    private float horizontalInfluence;
    private float verticalInfluence;
    private MoveData moveData;
    private int numJumpsPerformed;

    //Movement Collection
    public MovementCollection BaseMovementCollection;
    public MovementCollection CurMovementCollection;


    #region SceneObject Getters

    private SceneObject sceneObj;
    private Rigidbody rb { get { return sceneObj.rb; } }
    private bool isGrounded { get { return sceneObj.IsGrounded; } }
    private bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    private IAnimator animator { get { return sceneObj.Animator; } }
    private IActionState stateHandler { get { return sceneObj.StateHandler; } }

    #endregion

    public void Setup(SceneObject obj)
    {
        sceneObj = obj;

        sceneObj.EquipmentHandler.Weapons.WeaponChangedEvent += OnWeaponChanged;
        animator.OnAnimationUpdateEvent += OnMovementAnimationUpdated;

        OnWeaponChanged(null);        
    }

    #region Events

    private void OnWeaponChanged(Weapon weapon)
    {
        if (weapon == null)        
            CurMovementCollection = BaseMovementCollection;       

        else
            CurMovementCollection = weapon.MovementCollection;             
    }

    private void OnMovementAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (CurMovementCollection.TryGetMovementFromAnimationClip(animationState, out MovementData move))
        {
            switch (triggerType)
            {
                case AnimationTrigger.Type.Start:
                    OnMovementAnimationStarted(animationState, move.Type);
                    break;

                case AnimationTrigger.Type.End:
                    OnMovementAnimationEnded(animationState, move.Type);
                    break;
            }
        }
    }

    private void OnMovementAnimationStarted(string animationState, MovementType type)
    {
        curMoveAnimationState = animationState;        
    }

    private void OnMovementAnimationEnded(string animationState, MovementType type) 
    {        
        if (curMoveAnimationState == animationState)
        {
            curMoveAnimationState = null;

            switch(type)
            {
                case MovementType.Move:
                    break;

                case MovementType.Jump:
                case MovementType.AirJump:
                    //curMoveState = MovementType.Type.Fall;                    
                    break;

                case MovementType.Fall:
                    curMoveState = MovementType.Land;
                    break;

                case MovementType.Roll:
                case MovementType.Land:
                    curMoveState = MovementType.Move;
                    stateHandler.ResetState();
                    break;
            }                                                    
        }
    }

    #endregion


    private void ChangeMoveState(MovementType moveType)
    {
        curMoveState = moveType;
    }


    /// <summary>
    /// Horizontal movement based on input value
    /// Input value ranges between (-1, 1) 
    /// Input Value determines how much of the curCollection moveSpeed should be applied
    /// </summary>
    /// <param name="moveInfluence"></param>
    public void PerformMovement(Vector2 moveInfluence)
    {       
        if (CurMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData movement))
        {
            if (stateHandler.ChangeState(moveState))
            {
                moveInfluence.Normalize();
                horizontalInfluence = Mathf.Clamp(moveInfluence.x, -1, 1);
                verticalInfluence = Mathf.Clamp(moveInfluence.y, -1, 1);
                moveData = (MoveData)movement;                   
            }
        }                           
    }


    /// <summary>
    /// Vertical jump movement based on input value
    /// Input value ranges between (0, 1) 
    /// </summary>
    /// <param name="jumpInfluence"></param>
    public void PerformJump(float jumpInfluence)
    {
        Debug.Log("Perform Jump\n Influence " + jumpInfluence + "\nPos: " + transform.position.x + "\nVelocity: " + rb.velocity.x + "\nTime: " + Time.time);
        jumpInfluence = Mathf.Clamp01(jumpInfluence);

        if (isGrounded)
        {
            if (CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jump))
            {
                if (stateHandler.ChangeState(moveState))
                {
                    JumpAction((JumpData)jump, jumpInfluence);
                }
            }
        }
        
        else
        {
            if (CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJump))
            {
                if (stateHandler.ChangeState(moveState))
                {
                    AirJumpAction((AirJumpData)airJump, jumpInfluence);
                }
            }
        }        
    }


    private void JumpAction(JumpData jumpData, float jumpInfluence)
    {
        ChangeMoveState(jumpData.Type);
        
        rb.velocity = new Vector3(rb.velocity.x, jumpData.JumpVelocity * jumpInfluence, rb.velocity.z);
        numJumpsPerformed++;

        PlayMoveAnimation(jumpData.Type);        
    }


    private void AirJumpAction(AirJumpData airJumpData, float jumpInfluence)
    {
        if (numJumpsPerformed < airJumpData.JumpsAvailable)
        {
            ChangeMoveState(airJumpData.Type);
         
            rb.velocity = new Vector3(rb.velocity.x, airJumpData.AirJumpVelocity * jumpInfluence, rb.velocity.z);
            numJumpsPerformed++;

            CheckTurnAround();

            PlayMoveAnimation(airJumpData.Type);
        }
    }


    //TODO: Will work out later
    private void PerformLand()
    {
        Debug.Log("Land: \nPos: " + transform.position.x + "\nVelocity: " + rb.velocity.x + "\nTime: " + Time.time);

        curMoveState = MovementType.Move;
        numJumpsPerformed = 0;

        stateHandler.ResetState();

        //if (curMovementCollection.GetMovementByType(MovementType.Type.Land, out MovementCollection.Movement movement))
        //{
        //    ChangeMoveState(MovementType.Type.Land);
        //    PlayMoveAnimation(movement.type);
        //}
    }


    private void CheckTurnAround()
    {
        if ((isFacingRightDir && horizontalInfluence < 0) || (!isFacingRightDir && horizontalInfluence > 0))
        {
            sceneObj.TurnAround();
            rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
        }
    }


    private void FixedUpdate()
    {
        //Check for landing
        if (isGrounded && rb.velocity.y < 0 && (curMoveState == MovementType.Jump || curMoveState == MovementType.AirJump))
        {
            PerformLand();
        }
        
        
        //Check MoveData and Action State
        if (moveData != null && stateHandler.ChangeState(moveState))
        {
            if (isGrounded && (curMoveState == MovementType.Move || curMoveState == MovementType.Land))
            {
                UpdateGroundMovement();
            }

            else
            {
                UpdateAirMovement();
            }
        }         
    }


    private void UpdateGroundMovement()
    {
        //Turn around
        CheckTurnAround();

        //Accelerate
        if (Mathf.Abs(horizontalInfluence) > 0)
        {
            PlayMoveAnimation(moveData.Type);
            
            rb.velocity += transform.right * Mathf.Abs(horizontalInfluence) * moveData.XVelocityAcceleration * Time.fixedDeltaTime;

            if (rb.velocity.x > moveData.XVelocityLimit)
                rb.velocity = new Vector3(moveData.XVelocityLimit, rb.velocity.y);
            
            if (rb.velocity.x < -moveData.XVelocityLimit)
                rb.velocity = new Vector3(-moveData.XVelocityLimit, rb.velocity.y);
        }

        //Decelerate
        else if (Mathf.Abs(rb.velocity.x) > 0)
        {
            rb.velocity -= transform.right * moveData.XVelocityDeceleration * Time.fixedDeltaTime;

            //Check for stopping 
            if (isFacingRightDir && rb.velocity.x < 0f)
            {
                rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
                moveData = null;
                stateHandler.ResetState();                                    
            }

            else if (!isFacingRightDir && rb.velocity.x > 0f) 
            {
                rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
                moveData = null;
                stateHandler.ResetState();                
            }
        }

        animator.SetFloatPerameter("Velocity", moveData != null ? Mathf.Abs(rb.velocity.x) / moveData.XVelocityLimit : 0);                
    }


    private void UpdateAirMovement()
    {        
        //Horizontal Movement
        if (isFacingRightDir)
            rb.velocity += transform.right * horizontalInfluence * moveData.XAirVelocityAcceleration * Time.fixedDeltaTime;

        else
            rb.velocity -= transform.right * horizontalInfluence * moveData.XAirVelocityAcceleration * Time.fixedDeltaTime;


        if (rb.velocity.x > moveData.XVelocityLimit)
            rb.velocity = new Vector3(moveData.XVelocityLimit, rb.velocity.y);

        if (rb.velocity.x < -moveData.XVelocityLimit)
            rb.velocity = new Vector3(-moveData.XVelocityLimit, rb.velocity.y);


        //Vertical Movement
        if (verticalInfluence < 0f)
        {
            rb.velocity += transform.up * verticalInfluence * moveData.FastFallVelocity * Time.fixedDeltaTime;
        }
    }


    //TODO: Look at how animations are played. 
    //Move blendtree makes this not work so great...
    //This is the only place that uses the GetCurWeapon(), would like to remove
    /// <summary>
    /// Play move animation based on moveType
    /// </summary>
    /// <param name="moveType"></param>
    private void PlayMoveAnimation(MovementType moveType)
    {
        if (CurMovementCollection.TryGetMovementByType(moveType, out MovementData move))
        {
            string animationName = move.Animation.name;

            //Need to call name for blend tree
            if (move.Type == moveType)
            {
                string userName = gameObject.name;
                string clipName = moveType.ToString();
                string weaponName;

                if (sceneObj.EquipmentHandler.Weapons.GetCurWeapon() != null)
                    weaponName = sceneObj.EquipmentHandler.Weapons.GetCurWeapon().name;
                else
                    weaponName = "Base";

                animationName = userName + weaponName + clipName;
            }           

            sceneObj.Animator.PlayAnimation(animationName);            
        }
    }
}
