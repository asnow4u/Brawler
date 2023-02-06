using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerButtonMap buttonMap;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        buttonMap = new PlayerButtonMap();
        playerMovement = GetComponent<PlayerMovement>();
    }

    private void OnEnable()
    {
        buttonMap.Enable();
        buttonMap.PlayerActions.Movement.performed += MovementAction;
        buttonMap.PlayerActions.Movement.canceled += MovementAction;
        buttonMap.PlayerActions.Jump.performed += JumpAction;
        buttonMap.PlayerActions.Couch.performed += CouchAction;
        
        buttonMap.PlayerActions.UpTilt.performed += UpTiltAction;
        buttonMap.PlayerActions.DownTilt.performed += DownTiltAction;
        buttonMap.PlayerActions.RightTilt.performed += RightTiltAction;
        buttonMap.PlayerActions.LeftTilt.performed += LeftTiltAction;
    }

    private void OnDisable()
    {
        buttonMap.Disable();

        buttonMap.PlayerActions.Movement.performed -= MovementAction;
        buttonMap.PlayerActions.Jump.performed -= JumpAction;
        buttonMap.PlayerActions.Couch.performed -= CouchAction;

        buttonMap.PlayerActions.UpTilt.performed -= UpTiltAction;
        buttonMap.PlayerActions.DownTilt.performed -= DownTiltAction;
        buttonMap.PlayerActions.RightTilt.performed -= RightTiltAction;
        buttonMap.PlayerActions.LeftTilt.performed -= LeftTiltAction;
    }



    private void LeftTiltAction(InputAction.CallbackContext obj)
    {
        throw new NotImplementedException();
    }

    private void RightTiltAction(InputAction.CallbackContext obj)
    {
        throw new NotImplementedException();
    }

    private void DownTiltAction(InputAction.CallbackContext obj)
    {
        throw new NotImplementedException();
    }

    private void UpTiltAction(InputAction.CallbackContext obj)
    {
        GetComponent<Animator>().SetTrigger("UpTilt");
    }

    private void CouchAction(InputAction.CallbackContext obj)
    {
        throw new NotImplementedException();
    }

    private void JumpAction(InputAction.CallbackContext obj)
    {
        playerMovement.Jump();
    }

    private void MovementAction(InputAction.CallbackContext obj)
    {
        Vector2 movement = obj.ReadValue<Vector2>();

        playerMovement.UpdateHorizontalMovementSpeed(movement.x);
        playerMovement.UpdateVerticalMovementSpeed(movement.y);
    }
}
