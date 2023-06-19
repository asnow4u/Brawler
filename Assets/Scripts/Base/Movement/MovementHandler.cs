using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class MovementHandler : MonoBehaviour, IMovement
{
    protected SceneObject sceneObj;

    protected Rigidbody rb { get { return sceneObj.rb; } }
    protected bool isGrounded { get { return sceneObj.isGrounded; } }
    protected bool isFacingRightDir { get { return sceneObj.IsFacingRightDirection(); } }
    protected IAnimator animator { get { return sceneObj.animator; } }


    protected MovementCollection curMovementCollection;

    [Header("Movement Speed")]
    [SerializeField] private float accelerationX;
    [SerializeField] private float decelerationX;
    [SerializeField] private float airAccelerationX;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityX;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityY;

    [Header("Jump")]
    [SerializeField] protected float jumpVelocity;
    [SerializeField] protected float fastFallVelocity;
    
    protected float horizontalInputValue;
    protected float verticalInputValue;

    public void SetUp(SceneObject obj)
    {
        sceneObj = obj;
    }


    public void SetCollection(MovementCollection collection)
    {
        curMovementCollection = collection;
    }


    public void PerformMovement(Vector2 inputValue)
    {
        horizontalInputValue = inputValue.x;
        verticalInputValue = inputValue.y;                    
    }


    public void PerformJump()
    {
        Jump();
    }


    protected virtual void Jump()
    {
        if (curMovementCollection.GetMovementByType(MovementType.Type.jump, out MovementCollection.Movement movement))
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            sceneObj.animator.PlayAnimation(movement.animationClip.name);
        }
    }


    protected virtual void FixedUpdate()
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
                    animator.PlayIdleAnimation();
                }
            } 

            else
            {                
                if (rb.velocity.x > 0f)
                {
                    rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
                    animator.PlayIdleAnimation();
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
            Debug.Log(rb.velocity.y);
            rb.velocity += transform.up * verticalInputValue * fastFallVelocity * Time.fixedDeltaTime;
        }
    }
}
