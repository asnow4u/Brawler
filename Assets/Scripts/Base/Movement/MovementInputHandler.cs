using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MovementInputHandler : MonoBehaviour, IMovement
{
    const ActionState.State moveState = ActionState.State.Moving;

    protected SceneObject sceneObj;

    protected Rigidbody rb { get { return sceneObj.rb; } }
    protected bool isGrounded { get { return sceneObj.isGrounded; } }
    protected bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    protected IAnimator animator { get { return sceneObj.animator; } }
    protected IActionState stateHandler { get { return sceneObj.stateHandler; } }
    protected MovementCollection curMovementCollection { get { return sceneObj.equipmentHandler.GetCurrentWeaponMovementCollection(); } }

    [Header("Movement Speed")]
    [SerializeField] private float accelerationX = 30f;
    [SerializeField] private float decelerationX = 30f;
    [SerializeField] private float airAccelerationX = 15f;
    
    [SerializeField] private float maxVelocityX = 10f;
    [SerializeField] private float maxVelocityY = 5f;

    [Header("Jump")]
    [SerializeField] protected float jumpVelocity = 7f;
    [SerializeField] protected float fastFallVelocity = 1f;
    
    protected float horizontalInputValue;
    protected float verticalInputValue;

 
    public void Setup(SceneObject obj)
    {
        sceneObj = obj;
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
        if (curMovementCollection.GetMovementByType(MovementType.Type.jump, out MovementCollection.Movement movement))
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            sceneObj.animator.PlayAnimation(movement.animationClip.name);
        }
    }


    private void Update()
    {
        if (Mathf.Abs(horizontalInputValue) > 0)
        {
            stateHandler.ChangeState(moveState);
        }
    }


    protected virtual void FixedUpdate()
    {
        if (stateHandler.CompairState(moveState))
        {
            if (isGrounded)
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
        if ((isFacingRightDir && horizontalInputValue < 0) || (!isFacingRightDir && horizontalInputValue > 0))
        {
            sceneObj.TurnAround();
            rb.velocity = new Vector3(rb.velocity.x * -1, rb.velocity.y, rb.velocity.z);
        }
        
        //Accelerate
        if (Mathf.Abs(horizontalInputValue) > 0)
        {
            if (curMovementCollection.GetMovementByType(MovementType.Type.move, out MovementCollection.Movement movement))
            {
                animator.PlayAnimation(movement.animationClip.name);
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


    private void UpdateAirMovement()
    {
        animator.PlayIdleAnimation();

        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            if (isFacingRightDir)
                rb.velocity += transform.right * horizontalInputValue * airAccelerationX * Time.fixedDeltaTime;
            
            else
                rb.velocity -= transform.right * horizontalInputValue * airAccelerationX * Time.fixedDeltaTime;
        }

        
        if (verticalInputValue < 0f)
        {
            rb.velocity += transform.up * verticalInputValue * fastFallVelocity * Time.fixedDeltaTime;
        }
    }
}