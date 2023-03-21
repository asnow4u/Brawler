using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AnimatorStateMachine))]
public abstract class Movement : MonoBehaviour
{
    [Header("Movement Speed")]
    [SerializeField] private float accelerationX;
    [SerializeField] private float decelerationX;
    [SerializeField] private float accelerationY;
    [Range(0,1)]
    [SerializeField] private float drag;
    [Range(0,1)]
    [SerializeField] private float airDrag;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityX;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityY;

    [Header("Jump")]
    [SerializeField] protected float jumpVelocity;
    [SerializeField] protected float fallVelocity;

    public bool isGrounded;
    
    protected float xAxis;
    protected float yAxis;

    protected Rigidbody rb;
    protected AnimatorStateMachine animator;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<AnimatorStateMachine>();
    }



    public virtual void FixedUpdate()
    {

        //TODO: make two rays on each side to prevent landing just on the edge and not getting jump reset

        if (Physics.Raycast(transform.position, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
        {
            Debug.DrawLine(transform.position, transform.position - new Vector3(0, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, 0), Color.red);

            if (rb.velocity.y < 0)
            {
                isGrounded = true;                
            }
        }
        else
        {
            isGrounded = false;
        }


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
        if ((IsFacingRight() && xAxis < 0) || (!IsFacingRight() && xAxis > 0))
        {
            transform.Rotate(transform.up, 180f);
        }
        
        //Accelerate
        if (Mathf.Abs(xAxis) > 0)
        {
            if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
            {
                rb.velocity += transform.right * Mathf.Abs(xAxis) * accelerationX * Time.fixedDeltaTime;                
            }
        }

        //Decelerate
        else
        {
            rb.velocity -= transform.right * decelerationX * Time.fixedDeltaTime;
            if (IsFacingRight())
            {
                if (rb.velocity.x < 0) 
                    rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);            
            } 
            else
            {                
                if (rb.velocity.x > 0) 
                    rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
            }
        }
    }

    private void UpdateAirMovement()
    {
        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            rb.velocity += transform.right * Mathf.Abs(xAxis) * accelerationX * Time.fixedDeltaTime;
        }
    }


    public void ApplyJumpMovement()
    {
        if (isGrounded)
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
            //animator.ChangeAnimationState("Jump", 1, true);
        }
    }

    public void ApplyFallingMovement()
    {
        rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y / fallVelocity, rb.velocity.z);
    }


    private bool IsFacingRight()
    {
        float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
        float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

        if (angleRightDiff < angleLeftDiff)
        {
            return true;
        }

        return false;
    }
}
