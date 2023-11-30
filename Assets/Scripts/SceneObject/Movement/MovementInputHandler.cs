using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;


public class MovementInputHandler : MonoBehaviour, IMovement
{
    const ActionState.State moveState = ActionState.State.Moving;

    //Movement State Data
    private MovementType curMoveState = MovementType.Move;
    private string curMoveAnimationState;

    //Movement Data
    private float horizontalInputValue;
    private float verticalInputValue;
    private MoveData moveData;
    private int numJumpsPerformed;

    #region SceneObject Getters

    private SceneObject sceneObj;
    private Rigidbody rb { get { return sceneObj.rb; } }
    private bool isGrounded { get { return sceneObj.isGrounded; } }
    private float maxVelocityX { get { return sceneObj.maxVelocity; } }
    private bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    private IAnimator animator { get { return sceneObj.animator; } }
    private IActionState stateHandler { get { return sceneObj.stateHandler; } }
    private IWeaponCollection weaponCollection { get { return sceneObj.equipmentHandler.Weapons; } }
    private Weapon curWeapon { get { return weaponCollection.GetCurWeapon(); } }
    private MovementCollection curMovementCollection { get { return curWeapon.MovementCollection; } }

    #endregion

    public void Setup(SceneObject obj)
    {
        sceneObj = obj;

        animator.OnAnimationUpdateEvent += OnMovementAnimationUpdated;
    }

    #region Events

    private void OnMovementAnimationUpdated(string animationState, AnimationTrigger.Type triggerType)
    {
        if (GetMovementTypeFromAnimationState(animationState, out MovementType type))
        {
            switch (triggerType)
            {
                case AnimationTrigger.Type.Start:
                    OnMovementAnimationStarted(animationState, type);
                    break;

                case AnimationTrigger.Type.End:
                    OnMovementAnimationEnded(animationState, type);
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


    public void PerformMovement(Vector2 inputValue)
    {
        if (curMovementCollection.GetMovementByType(MovementType.Move, out MovementData movement))
        {
            if (stateHandler.ChangeState(moveState))
            {
                inputValue.Normalize();
                horizontalInputValue = inputValue.x;
                verticalInputValue = inputValue.y;
                moveData = (MoveData)movement;                   
            }
        }                           
    }


    public void PerformJump()
    {
        if (isGrounded)
        {
            if (curMovementCollection.GetMovementByType(MovementType.Jump, out MovementData jump))
            {
                if (stateHandler.ChangeState(moveState))
                {
                    JumpAction((JumpData)jump);
                }
            }
        }
        
        else
        {
            if (curMovementCollection.GetMovementByType(MovementType.AirJump, out MovementData airJump))
            {
                if (stateHandler.ChangeState(moveState))
                {
                    AirJumpAction((AirJumpData)airJump);
                }
            }
        }        
    }


    private void JumpAction(JumpData jumpData)
    {
        ChangeMoveState(jumpData.Type);
        
        rb.velocity = new Vector3(rb.velocity.x, jumpData.JumpVelocity, rb.velocity.z);
        numJumpsPerformed++;

        PlayMoveAnimation(jumpData.Type);        
    }


    private void AirJumpAction(AirJumpData airJumpData)
    {
        if (numJumpsPerformed < airJumpData.JumpsAvailable)
        {
            ChangeMoveState(airJumpData.Type);
         
            rb.velocity = new Vector3(rb.velocity.x, airJumpData.AirJumpVelocity, rb.velocity.z);
            numJumpsPerformed++;

            CheckTurnAround();

            PlayMoveAnimation(airJumpData.Type);
        }
    }


    //TODO: Will work out later
    private void PerformLand()
    {
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
        if ((isFacingRightDir && horizontalInputValue < 0) || (!isFacingRightDir && horizontalInputValue > 0))
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
        if (Mathf.Abs(horizontalInputValue) > 0)
        {
            PlayMoveAnimation(moveData.Type);
            
            if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
            {
                rb.velocity += transform.right * Mathf.Abs(horizontalInputValue) * moveData.AccelerationX * Time.fixedDeltaTime;
            }
        }

        //Decelerate
        else if (Mathf.Abs(rb.velocity.x) > 0)
        {
            rb.velocity -= transform.right * moveData.DecelerationX * Time.fixedDeltaTime;

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

        animator.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / maxVelocityX);
        
    }


    private void UpdateAirMovement()
    {        
        //Horizontal Movement
        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            if (isFacingRightDir)
                rb.velocity += transform.right * horizontalInputValue * moveData.AirAccelerationX * Time.fixedDeltaTime;

            else
                rb.velocity -= transform.right * horizontalInputValue * moveData.AirAccelerationX * Time.fixedDeltaTime;
        }

        //Vertical Movement
        if (verticalInputValue < 0f)
        {
            rb.velocity += transform.up * verticalInputValue * moveData.FastFallVelocity * Time.fixedDeltaTime;
        }
    }


    #region Animation

    private bool GetMovementTypeFromAnimationState(string animationState, out MovementType type)
    {
        string str = animationState;
        str = str.Replace(gameObject.name, "");
        str = str.Replace(curWeapon.weaponType.ToString(), "");

        if (Enum.TryParse(str, out type))
        {
            return true;
        }

        return false;
    }


    private void PlayMoveAnimation(MovementType moveType)
    {
        string name = gameObject.name;
        string weaponName = curWeapon.weaponType.ToString();
        string moveName = moveType.ToString();

        sceneObj.animator.PlayAnimation(name + weaponName + moveName);
    }

    #endregion
}
