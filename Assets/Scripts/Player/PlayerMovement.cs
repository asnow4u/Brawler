using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : Movement
{
    [Header("Jump")]
    [SerializeField] private int jumpsAvailable;
    [SerializeField] private float additionalJumpVelocity;
    private int additionalJumpsPerformed;

    // Update is called once per frame
    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (isGrounded)
        {
            additionalJumpsPerformed = 0;
        }
    }


    public void SetUpMovementEvents(PlayerButtonMap input)
    {
        input.PlayerActions.Movement.performed += Movement_performed;        
        input.PlayerActions.Movement.canceled += Movement_canceled;

        input.PlayerActions.Jump.performed += Jump_performed;
        input.PlayerActions.Jump.canceled += Jump_canceled;

        input.PlayerActions.Couch.performed += Couch_performed;
        input.PlayerActions.Couch.canceled += Couch_canceled;
    }


    private void Couch_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        //TODO: throw new NotImplementedException();
    }

    private void Couch_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        throw new NotImplementedException();
    }

    private void Jump_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        if (isGrounded)
        {
            ApplyJumpMovement();
        }

        else
        {
            if (additionalJumpsPerformed < jumpsAvailable)
            {
                rb.velocity = new Vector3(rb.velocity.x, additionalJumpVelocity, rb.velocity.z);
                additionalJumpsPerformed++;
                //animator.ChangeAnimationState("Jump", 1, true);
            }
        }       
    }

    private void Jump_canceled(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        ApplyFallingMovement();
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
