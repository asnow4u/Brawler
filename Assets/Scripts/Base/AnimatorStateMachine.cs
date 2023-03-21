using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Action))]
[RequireComponent(typeof(Movement))]
public abstract class AnimatorStateMachine : MonoBehaviour
{
    public enum ActionState { Idle, Moveing, Jumping, Landing, Attacking, Damaged, Dead };
    protected ActionState actionState;

    protected Animator animator;
    protected string curAnimationPlaying;

    protected Action actionManager;

    protected Movement movementManager;

    private void Start()
    {
        animator = GetComponent<Animator>();
        actionManager = GetComponent<Action>();
        movementManager = GetComponent<Movement>();

        actionState = ActionState.Idle;
    }


    private bool CheckAnimationHierachy(ActionState newState)
    {
        if ((int)newState > (int)actionState)
        {
            return true;
        }

        return false;
    }



    public void ChangeAnimationState(string animation, ActionState newState)
    {
        if (!CheckAnimationHierachy(newState)) return;

        actionState = newState;

        animator.Play(animation);
        curAnimationPlaying = animation;
        
        Debug.Log(gameObject.name + " Anmation started => " + curAnimationPlaying);
    }


    public abstract void OnAnimationStarted();

    public abstract void OnAnimationEnableColliders();

    public abstract void OnAnimationDisableColliders();

    public virtual void OnAnimationEnd()
    {
        Debug.Log("Animation Ended");

        actionState = ActionState.Idle;

        if (movementManager.isGrounded)
        {
            animator.Play("Idle");
        }
        else
        {
            animator.Play("AirIdle");
        }
    }
}

