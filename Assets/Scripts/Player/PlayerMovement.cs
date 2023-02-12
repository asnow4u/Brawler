using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class Player : MonoBehaviour
{
    [Header("Movement Speed")]
    [SerializeField] private float speedX;
    [SerializeField] private float speedY;
    [SerializeField] private float airDrag;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityX;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityY;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] int jumpsAvailable;
    public bool isGrounded;

    private Rigidbody rb;

    private float xAxis;
    private float yAxis;

    private int additionalJumpsPerformed;
 
    // Update is called once per frame
    void FixedUpdate()
    {
        //TODO: make two rays on each side of the player to prevent landing just on the edge and not getting jump reset
        if (Physics.Raycast(transform.position, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
        {
            Debug.DrawLine(transform.position, transform.position - new Vector3(0, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, 0), Color.red);

            if (rb.velocity.y < 0)
            {
                isGrounded = true;
                additionalJumpsPerformed = 0;
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
        //Horizontal Movement
        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            rb.AddForce(transform.right * xAxis * speedX);
        }


        //TODO: note, depending on sign of xAxis and direction character is facing might need to flip around
    }

    private void UpdateAirMovement()
    {
        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            rb.AddForce(transform.right * xAxis * speedX * airDrag);
        }
    }

    private void SetUpMovementEvents(PlayerButtonMap input)
    {
        input.PlayerActions.Movement.performed += Movement_performed;
        input.PlayerActions.Movement.canceled += Movement_canceled;
        input.PlayerActions.Jump.performed += Jump_performed;
        input.PlayerActions.Couch.performed += Couch_performed;
    }



    private void Couch_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        //TODO: throw new NotImplementedException();
    }

    private void Jump_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (isGrounded)
        {
            rb.AddForce(new Vector3(0, 1, 0) * jumpForce, ForceMode.Impulse);
        }

        else if (additionalJumpsPerformed < jumpsAvailable)
        {
            rb.AddForce(new Vector3(0, 1, 0) * (jumpForce + Math.Abs(Physics.gravity.y)), ForceMode.Impulse);
            additionalJumpsPerformed++;
        }
    }

    private void Movement_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {        
        Vector2 movement = obj.ReadValue<Vector2>();
        
        xAxis = movement.x;
        yAxis = movement.y;        
    }

    private void Movement_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {        
        xAxis = 0;
        yAxis = 0;
    }
}
