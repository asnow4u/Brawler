using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAction : Action
{

    public void SetUpActionEvents(PlayerButtonMap input)
    {
        input.PlayerActions.RightAttack.performed += RightAttack_performed;
        input.PlayerActions.LeftAttack.performed += LeftAttack_performed;
        input.PlayerActions.DownAttack.performed += DownAttack_performed;
        input.PlayerActions.UpAttack.performed += UpAttack_performed;
    }

    private void UpAttack_performed(InputAction.CallbackContext obj)
    {
        PerformUpAttack();
    }

    private void DownAttack_performed(InputAction.CallbackContext obj)
    {
        PerformDownAttack();
    }

    private void LeftAttack_performed(InputAction.CallbackContext obj)
    {
        PerformLeftAttack();
    }

    private void RightAttack_performed(InputAction.CallbackContext obj)
    {
        PerformRightAttack();
    }



    protected override void PerformUpAttack()
    {
        if (movement.isGrounded)
        {            
            animator.ChangeAnimationState("UpTilt", AnimatorStateMachine.ActionState.Attacking);
        }
        else
        {
            animator.ChangeAnimationState("UpAir", AnimatorStateMachine.ActionState.Attacking);
        }
    }

    protected override void PerformDownAttack()
    {
        if(movement.isGrounded)
        {
            animator.ChangeAnimationState("PickUp", AnimatorStateMachine.ActionState.Attacking);
        }
        else
        {
            animator.ChangeAnimationState("DownAir", AnimatorStateMachine.ActionState.Attacking);
        }
    }

    protected override void PerformLeftAttack()
    {
        if (movement.isGrounded)
        {
            animator.ChangeAnimationState("LeftTilt", AnimatorStateMachine.ActionState.Attacking);
        }
        else
        {
            animator.ChangeAnimationState("LeftAir", AnimatorStateMachine.ActionState.Attacking);
        }
    }

    protected override void PerformRightAttack()
    {
        if (movement.isGrounded)
        {
            animator.ChangeAnimationState("RightTilt", AnimatorStateMachine.ActionState.Attacking);
        }
        else
        {
            animator.ChangeAnimationState("RightAir", AnimatorStateMachine.ActionState.Attacking);
        }
    }
}
