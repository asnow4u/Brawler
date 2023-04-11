using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public abstract class AnimatorStateMachine : MonoBehaviour
{
    public enum ActionState { Idle, Moveing, Jumping, Landing, Attacking, Damaged, Dead };
    protected ActionState actionState;

    protected Animator animator;
    protected string curAnimationPlaying;


    public UnityEvent<AnimationClip> AnimationStartedEvent;
    public UnityEvent<AnimationClip> AnimationEndedEvent;
    public UnityEvent<AnimationClip> AnimationCollidersEnabledEvent;
    public UnityEvent<AnimationClip> AnimationCollidersDisabledEvent;


    private void Start()
    {
        animator = GetComponent<Animator>();

        actionState = ActionState.Idle;

        SetUpEvents();
    }

    private void SetUpEvents()
    {
        AnimationStartedEvent = new UnityEvent<AnimationClip>();
        AnimationEndedEvent = new UnityEvent<AnimationClip>();
        AnimationCollidersEnabledEvent = new UnityEvent<AnimationClip>();
        AnimationCollidersDisabledEvent = new UnityEvent<AnimationClip>();
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


    public virtual void OnAnimationStarted(AnimationClip clip)
    {        
        AnimationStartedEvent?.Invoke(clip);
    }

    public virtual void OnAnimationEnableColliders(AnimationClip clip)
    {
        AnimationCollidersEnabledEvent?.Invoke(clip);
    }

    public virtual void OnAnimationDisableColliders(AnimationClip clip)
    {
        AnimationCollidersDisabledEvent?.Invoke(clip);
    }

    public virtual void OnAnimationEnd(AnimationClip clip)
    {
        AnimationEndedEvent?.Invoke(clip);

        Debug.Log("Animation Ended");

        actionState = ActionState.Idle;

        //if (movementManager.isGrounded)
        //{
        //    animator.Play("Idle");
        //}
        //else
        //{
        //    animator.Play("AirIdle");
        //}
    }
}

