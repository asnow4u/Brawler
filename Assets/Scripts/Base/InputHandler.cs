using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Attack;
using Movement;

public class InputHandler : MonoBehaviour
{
    private PlayerButtonMap input;
    private AttackHandler attackHandler;
    private MovementHandler movementHandler;

    public void SetUpHandler()
    {
        input = new PlayerButtonMap();
        input.Enable();

        input.PlayerActions.RightAttack.performed += AttackToRight;
        input.PlayerActions.LeftAttack.performed += AttackToLeft;
        input.PlayerActions.DownAttack.performed += AttackDownward;
        input.PlayerActions.UpAttack.performed += AttackUpward;

        attackHandler = GetComponent<AttackHandler>();
        movementHandler = GetComponent<MovementHandler>();
    }

    public void DisableInputEvents()
    {
        input.Disable();
    }

    private void AttackUpward(InputAction.CallbackContext obj)
    {        
        if (movementHandler.isGrounded)
        {
            attackHandler.PerformUpTiltAttack();       
        }
        else
        {
            attackHandler.PerformUpAirAttack();
        }        
    }

    private void AttackDownward(InputAction.CallbackContext obj)
    {
        if (movementHandler.isGrounded)
        {
            attackHandler.PerformDownTiltAttack();        
        }
        else
        {
            attackHandler.PerformDownAirAttack();
        }
    }

    private void AttackToLeft(InputAction.CallbackContext obj)
    {        
        if (movementHandler.isGrounded)
        {
            if (movementHandler.IsFacingRightDirection())
            {
                movementHandler.TurnAround();
                attackHandler.PerformForwardTiltAttack();
            }

            else
            {
                attackHandler.PerformForwardTiltAttack();
            }
        }
        else
        {
            if (movementHandler.IsFacingRightDirection())
            {
                attackHandler.PerformBackAirAttack();
            }

            else
            {
                attackHandler.PerformForwardAirAttack();
            }
        }
    }

    private void AttackToRight(InputAction.CallbackContext obj)
    {
        if (movementHandler.isGrounded)
        {
            if (movementHandler.IsFacingRightDirection())
            {
                attackHandler.PerformForwardTiltAttack();
            }        
            
            else
            {
                movementHandler.TurnAround();
                attackHandler.PerformForwardTiltAttack();
            }
        }
        else
        {
            if (movementHandler.IsFacingRightDirection())
            {
                attackHandler.PerformForwardAirAttack();
            }

            else
            {
                attackHandler.PerformBackAirAttack();
            }
        }
    }    
}
