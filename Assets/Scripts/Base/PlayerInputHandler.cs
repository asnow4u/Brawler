using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SceneObj.Router;

public class PlayerInputHandler : MonoBehaviour
{
    private ActionRouter actionHandler;
    private PlayerButtonMap input;

    public void SetUpHandler(ActionRouter actionHandler)
    {
        this.actionHandler = actionHandler;

        input = new PlayerButtonMap();
        input.Enable();

        //Movement
        input.PlayerActions.Movement.performed += HorizontalMovement;
        input.PlayerActions.Movement.canceled += MovementCanceled;

        input.PlayerActions.Jump.performed += JumpPerformed;
        input.PlayerActions.Jump.canceled += JumpCanceled;

        //input.PlayerActions.Couch.performed += Couch_performed;
        //input.PlayerActions.Couch.canceled += Couch_canceled;

        //Attack
        input.PlayerActions.RightAttack.performed += AttackToRight;
        input.PlayerActions.LeftAttack.performed += AttackToLeft;
        input.PlayerActions.DownAttack.performed += AttackDownward;
        input.PlayerActions.UpAttack.performed += AttackUpward;
    }

    public void DisableInputEvents()
    {
        input.Disable();
    }

    #region Movement Input Events

    private void HorizontalMovement(InputAction.CallbackContext obj)
    {
        actionHandler.OnHorizontalMovement(obj.ReadValue<Vector2>());
    }

    private void MovementCanceled(InputAction.CallbackContext obj)
    {
        actionHandler.OnHorizontalMovementStopped();
    }

    private void JumpPerformed(InputAction.CallbackContext obj)
    {
        actionHandler.OnJump();
    }

    private void JumpCanceled(InputAction.CallbackContext obj)
    {
        actionHandler.OnJumpCanceled();
    }
    #endregion

    #region Attack Input Events

    private void AttackUpward(InputAction.CallbackContext obj)
    {
        actionHandler.OnUpAttack();
    }

    private void AttackDownward(InputAction.CallbackContext obj)
    {
        actionHandler.OnDownAttack();
    }

    private void AttackToLeft(InputAction.CallbackContext obj)
    {
        actionHandler.OnLeftAttack();
    }

    private void AttackToRight(InputAction.CallbackContext obj)
    {
        actionHandler.OnRightAttack();
    }
    #endregion
}
