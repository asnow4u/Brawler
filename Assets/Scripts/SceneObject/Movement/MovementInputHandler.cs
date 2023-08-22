using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.SceneManagement;
using UnityEngine;
using static AttackCollection;


public class MovementInputHandler : MonoBehaviour, IMovement
{
    const ActionState.State moveState = ActionState.State.Moving;

    protected SceneObject sceneObj;

    protected MovementType.Type curMoveState = MovementType.Type.Move;
    protected string curMoveAnimationState;

    protected Rigidbody rb { get { return sceneObj.rb; } }
    protected bool isGrounded { get { return sceneObj.isGrounded; } }
    protected float maxVelocityX { get { return sceneObj.maxVelocity; } }
    protected bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    protected IAnimator animator { get { return sceneObj.animator; } }
    protected IActionState stateHandler { get { return sceneObj.stateHandler; } }
    protected IWeaponCollection weaponCollection { get { return sceneObj.equipmentHandler.Weapons; } }
    protected Weapon curWeapon { get { return weaponCollection.GetCurWeapon(); } }  
    protected MovementCollection curMovementCollection { get { return curWeapon.MovementCollection; } }

    [Header("Movement Speed")]
    [SerializeField] private float accelerationX = 30f;
    [SerializeField] private float decelerationX = 30f;
    [SerializeField] private float airAccelerationX = 15f;

    [Header("Jump")]
    [SerializeField] protected float jumpVelocity = 7f;
    [SerializeField] protected float fallVelocity = 1f;
    
    protected float horizontalInputValue;
    protected float verticalInputValue;

 
    public void Setup(SceneObject obj)
    {
        sceneObj = obj;

        animator.OnAnimationStateStartedEvent += OnMovementAnimationStarted;
        animator.OnAnimationStateEndedEvent += OnMovementAnimationEnded;
    }

    #region Events

    private void OnMovementAnimationStarted(string animationState)
    {
        if (GetMovementTypeFromAnimationState(animationState, out MovementType.Type type))
        {
            curMoveAnimationState = animationState;
        }
    }

    private void OnMovementAnimationEnded(string animationState) 
    {        
        if (curMoveAnimationState == animationState && GetMovementTypeFromAnimationState(animationState, out MovementType.Type type))
        {
            curMoveAnimationState = null;

            switch(type)
            {
                case MovementType.Type.Move:
                    break;

                case MovementType.Type.Jump:
                case MovementType.Type.AirJump:
                    Debug.Log("MOVE: curMoveState => Fall");
                    curMoveState = MovementType.Type.Fall;
                    break;

                case MovementType.Type.Fall:
                    Debug.Log("MOVE: curMoveState => Land");
                    curMoveState = MovementType.Type.Land;
                    break;

                case MovementType.Type.Roll:
                case MovementType.Type.Land:
                    Debug.Log("MOVE: curMoveState => Move");
                    curMoveState = MovementType.Type.Move;
                    stateHandler.ResetState();
                    break;
            }                                                    
        }
    }

    #endregion


    protected void ChangeMoveState(MovementType.Type moveType)
    {
        curMoveState = moveType;
    }


    public void PerformMovement(Vector2 inputValue)
    {
        horizontalInputValue = inputValue.x;
        verticalInputValue = inputValue.y;                           
    }


    public void PerformJump()
    {
        if (stateHandler.ChangeState(moveState))
        {
            Jump();
        }
    }


    protected virtual void Jump()
    {
        if (curMovementCollection.GetMovementByType(MovementType.Type.Jump, out MovementCollection.Movement movement))
        {
            ChangeMoveState(MovementType.Type.Jump);
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            PlayMoveAnimation(movement.type);
        }
    }


    private void PerformLand()
    {
        if (curMovementCollection.GetMovementByType(MovementType.Type.Land, out MovementCollection.Movement movement))
        {
            ChangeMoveState(MovementType.Type.Land);
            PlayMoveAnimation(movement.type);
        }
    }


    private void PerformRoll()
    {
        if (curMovementCollection.GetMovementByType(MovementType.Type.Roll, out MovementCollection.Movement movement))
        {
            CheckTurnAround();
            ChangeMoveState(MovementType.Type.Roll);
            PlayMoveAnimation(movement.type);
        }
    }


    private void CheckTurnAround()
    {
        if ((isFacingRightDir && horizontalInputValue < 0) || (!isFacingRightDir && horizontalInputValue > 0))
        {
            sceneObj.TurnAround();
            rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
        }
    }


    protected virtual void FixedUpdate()
    {
        if (Mathf.Abs(horizontalInputValue) > 0)
        {
            stateHandler.ChangeState(moveState);
        }

        if (isGrounded && curMoveState == MovementType.Type.Fall)
        {
            if (horizontalInputValue == 0)
                PerformLand();
            else
                PerformRoll();
        }

        if (isGrounded && curMoveState == MovementType.Type.Move)
        {
            Debug.Log("MOVE: Update ground movement");
            UpdateGroundMovement();                
        }

        else
        {
            UpdateAirMovement();
        }        
    }


    protected virtual void UpdateGroundMovement()
    {
        UpdateGroundMovementByInput();   
    }


    private void UpdateGroundMovementByInput()
{
        if (stateHandler.CompairState(moveState))
        {
            //Turn around
            CheckTurnAround();

            //Accelerate
            if (Mathf.Abs(horizontalInputValue) > 0)
            {
                if (curMovementCollection.GetMovementByType(MovementType.Type.Move, out MovementCollection.Movement movement))
                {
                    PlayMoveAnimation(movement.type);                    
                }

                if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
                {
                    rb.velocity += transform.right * Mathf.Abs(horizontalInputValue) * accelerationX * Time.fixedDeltaTime;
                }
            }

            //Decelerate
            else if (Mathf.Abs(rb.velocity.x) > 0)
            {
                rb.velocity -= transform.right * decelerationX * Time.fixedDeltaTime;

                if (isFacingRightDir)
                {
                    if (rb.velocity.x < 0f)
                    {
                        rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);

                        if (stateHandler.CompairState(moveState))
                        {
                            stateHandler.ResetState();
                        }
                    }
                }

                else
                {
                    if (rb.velocity.x > 0f)
                    {
                        rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);

                        if (stateHandler.CompairState(moveState))
                        {
                            stateHandler.ResetState();
                        }
                    }
                }
            }

            animator.SetFloatPerameter("Velocity", Mathf.Abs(rb.velocity.x) / maxVelocityX);
        }
    }


    protected virtual void UpdateAirMovement()
    {
        UpdateAirMovementByInput();

        if (rb.velocity.y < 0)
        {
            rb.velocity += transform.up * -fallVelocity * Time.fixedDeltaTime;
        }
    }


    private void UpdateAirMovementByInput()
    {
        if (stateHandler.CompairState(moveState))
        {
            if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
            {
                if (isFacingRightDir)
                    rb.velocity += transform.right * horizontalInputValue * airAccelerationX * Time.fixedDeltaTime;

                else
                    rb.velocity -= transform.right * horizontalInputValue * airAccelerationX * Time.fixedDeltaTime;
            }
        }
    }


    #region Animation

    private bool GetMovementTypeFromAnimationState(string animationState, out MovementType.Type type)
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


    protected void PlayMoveAnimation(MovementType.Type moveType)
    {
        string name = gameObject.name;
        string weaponName = curWeapon.weaponType.ToString();
        string moveName = moveType.ToString();

        sceneObj.animator.PlayAnimation(name + weaponName + moveName);
    }

    #endregion
}
