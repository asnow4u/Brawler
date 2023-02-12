using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player : MonoBehaviour
{
    

    private void SetUpActionEvents(PlayerButtonMap input)
    {
        input.PlayerActions.RightAttack.performed += RightAttack_performed;
        input.PlayerActions.LeftAttack.performed += LeftAttack_performed;
        input.PlayerActions.DownAttack.performed += DownAttack_performed;
        input.PlayerActions.UpAttack.performed += UpAttack_performed;
    }

    private void UpAttack_performed(InputAction.CallbackContext obj)
    {
        if (isGrounded)
        {
            animator.ChangeAnimationState("UpTilt", 2, false);
        }
        else
        {
            animator.ChangeAnimationState("UpAir", 2, false);
        }
    }

    private void DownAttack_performed(InputAction.CallbackContext obj)
    {
        if (isGrounded)
        {
            animator.ChangeAnimationState("PickUp", 2, false);
        }
        else
        {
            animator.ChangeAnimationState("DownAir", 2, false);
        }
    }

    private void LeftAttack_performed(InputAction.CallbackContext obj)
    {
        if(isGrounded)
        {
            animator.ChangeAnimationState("LeftTilt", 2, false);
        }
        else
        {
            animator.ChangeAnimationState("LeftAir", 2, false);
        }
    }

    private void RightAttack_performed(InputAction.CallbackContext obj)
    {
        if (isGrounded)
        {
            animator.ChangeAnimationState("RightTilt", 2, false);
        }
        else
        {
            animator.ChangeAnimationState("RightAir", 2, false);
        }
    }
}
