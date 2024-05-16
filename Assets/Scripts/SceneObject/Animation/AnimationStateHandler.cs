using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public enum ActionState
{
    Null, Idle, Moving, Attacking, HitStun, Dead, Admin
};


public class AnimationStateHandler : MonoBehaviour, IAnimator
{
    [SerializeField] private ActionState curActionState;
    [SerializeField] private string curPlayingAnimation;

    private Coroutine animationEventCorutine;

    //Getter
    private Animator animator => GetComponentInChildren<Animator>();
    private SceneObject sceneObject => GetComponent<SceneObject>();

    //Events
    public event Action<string, AnimationTrigger.Type> OnAnimationUpdateEvent;


    #region Initialize

    public void SetUp()
    {
        PlayIdleAnimation();
    }

    #endregion


    #region Getters

    /// <summary>
    /// Return the current running animation clip info
    /// </summary>
    /// <returns></returns>
    private AnimationClip GetCurrentPlayingAnimation()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
            return clipInfo[0].clip;

        return null;
    }


    /// <summary>
    /// Returns the normalized percentage of the animation playtime (0-1)
    /// </summary>
    /// <returns></returns>
    private float GetCurAnimationNormalizedTime()
    {        
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.normalizedTime;        
    }


    /// <summary>
    /// Return if this current animation loops
    /// </summary>
    /// <returns></returns>
    private bool IsCurAnimationLooping()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.loop;
    }


    /// <summary>
    /// Returns the current frame of the playing animation
    /// </summary>
    /// <returns></returns>
    public int GetCurrentFrameOfCurAnimation()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return Mathf.RoundToInt(animationInfo.normalizedTime * GetCurrentPlayingAnimation().frameRate);            
    }


    #endregion


    #region Action State

    /// <summary>
    /// Determine and return whether the requested state is possible
    /// </summary>
    /// <param name="requestedState"></param>
    /// <returns></returns>
    public bool IsStatePossible(ActionState requestedState)
    {
        if (requestedState != ActionState.Null)
        {
            if (curActionState <= requestedState)
                return true;
        }

        return false;
    }


    private void ChangeState(ActionState newState)
    {        
        Debug.Log("STATE: " + newState);
        curActionState = newState;            
    }


    private void ResetState()
    {
        curActionState = ActionState.Idle;
    }

    #endregion


    #region Play Animation
    
    private void PlayIdleAnimation()
    {
        if (sceneObject.GroundedState == GroundedState.Airborn)
            PlayAnimation(new AnimationStateData("BaseAirIdle", ActionState.Idle, null));

        else
            PlayAnimation(new AnimationStateData("BaseIdle", ActionState.Idle, null));
    }
    

    /// <summary>
    /// Update the current ActionState <br/>
    /// Start playing animation.
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="animationTriggers"></param>
    public void PlayAnimation(AnimationStateData animationData)
    {        
        if (animationData != null)
        {
            ChangeState(animationData.State);

            if (animationData.ClipName != curPlayingAnimation)
            {
                Debug.Log("ANIMATION: Start " + animationData.ClipName);
                animator.Play("Base Layer." + animationData.ClipName);
                StartCoroutine(WaitForAnimationStart(animationData.ClipName, animationData.Triggers));
            }                
        }
    }


    /// <summary>
    /// Check for animation equal to or below the provided state
    /// Reset action state and play idle animation
    /// </summary>
    public void EndCurrentAnimation(ActionState priorityState)
    {
        if (IsStatePossible(priorityState))
        {
            Debug.Log("ANIMATION: End " + curPlayingAnimation);
            ResetState();
            PlayIdleAnimation();
        }
    }


    /// <summary>
    /// Wait till the animation begins playing. 
    /// </summary>
    /// <param name="waitingAnimation"></param>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator WaitForAnimationStart(string waitingAnimation, AnimationTrigger[] animationTriggers)
    {
        int waitingHashID = Animator.StringToHash(waitingAnimation);        

        while (waitingHashID != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
        {
            yield return null;
        }

        InitializeAnimation(waitingAnimation, animationTriggers);      
    }


    /// <summary>
    /// Update the currentPlayingAnimation. <br/> 
    /// Send End AnimationTrigger event if previous animation was active. <br/>
    /// Set up coroutine for trigger events
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="animationTriggers"></param>
    private void InitializeAnimation(string animationName, AnimationTrigger[] animationTriggers)
    {
        //Stop previous animation events
        if (animationEventCorutine != null)
            StopCoroutine(animationEventCorutine);

        //Send end event for previous animation
        if (curPlayingAnimation != null && curPlayingAnimation != animationName)
        {
            Debug.Log("ANIMATION: End " + curPlayingAnimation);
            OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, AnimationTrigger.Type.End);
        }

        curPlayingAnimation = animationName;

        //Reset Animation Triggers
        ResetAnimationTriggers(animationTriggers);

        //Start animation events
        animationEventCorutine = StartCoroutine(CheckAnimationTriggerEvents(animationTriggers));
        OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, AnimationTrigger.Type.Start);
    }


    /// <summary>
    /// Check each frame for any animationTrigger events that need to fire
    /// </summary>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator CheckAnimationTriggerEvents(AnimationTrigger[] animationTriggers)
    {
        //Looping animation
        if (IsCurAnimationLooping())
        {
            while (IsCurAnimationLooping())
            {
                CheckForAnimationTrigger(animationTriggers);
                
                if (GetCurAnimationNormalizedTime() == 1)
                    ResetAnimationTriggers(animationTriggers);                
                
                yield return null;
            }
        }

        //Loop while animation is running
        while (GetCurAnimationNormalizedTime() < 1)
        {
            CheckForAnimationTrigger(animationTriggers);
         
            yield return null;
        }        

        //End Animation
        EndCurrentAnimation(ActionState.Admin);
    }


    private void CheckForAnimationTrigger(AnimationTrigger[] animationTriggers)
    {
        if (animationTriggers != null)
        {
            float curFrame = GetCurrentFrameOfCurAnimation();

            foreach (AnimationTrigger trigger in animationTriggers)
            {
                if (!trigger.WasTriggered && curFrame >= trigger.TriggerFrame)
                {
                    OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, trigger.TriggerType);
                    trigger.WasTriggered = true;

                    //End Animation                    
                    if (trigger.TriggerType == AnimationTrigger.Type.End)
                    {
                        EndCurrentAnimation(ActionState.Admin);
                    }
                }
            }
        }
    }


    private void ResetAnimationTriggers(AnimationTrigger[] animationTriggers)
    {
        if (animationTriggers != null)
        {
            foreach (AnimationTrigger trigger in animationTriggers)
                trigger.Reset();
        }
    }

    #endregion



    #region Perameter Setting

    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    #endregion

}