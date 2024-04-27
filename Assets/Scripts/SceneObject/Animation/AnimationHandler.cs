using System;
using System.Collections;
using UnityEditor.Animations;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationHandler : MonoBehaviour, IAnimator
{
    protected SceneObject sceneObj;
    protected Animator animator => GetComponent<Animator>();

    protected string curAnimatorState = string.Empty;

    protected Coroutine animationEventCorutine;

    public event Action<string, AnimationTrigger.Type> OnAnimationUpdateEvent;

    public void SetUp(SceneObject obj)
    {
        sceneObj = obj;
    }


    private AnimationClip GetCurrentPlayingAnimation()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
            return clipInfo[0].clip;

        return null;
    }


    private float GetCurAnimationNormalizedTime()
    {        
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.normalizedTime;        
    }


    private float GetCurrentFrameOfCurAnimation()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return Mathf.RoundToInt(animationInfo.normalizedTime * GetCurrentPlayingAnimation().frameRate);            
    }


    public void PlayIdleAnimation()
    {
        if (sceneObj.MovementInputHandler.GroundedState == GroundedState.Grounded)
            PlayAnimation("BaseIdle");
        else
            PlayAnimation("BaseAirIdle");
    }


    #region Play Animation

    public void PlayAnimation(string animationState, AnimationTrigger[] animationTriggers = null)
    {        
        if (animationState != null)
        {
            Debug.Log(animationState);

            //Check for cur animation playing
            if (curAnimatorState != string.Empty)
            {
                //Check for difference
                if (curAnimatorState != animationState)
                {
                    EndCurAnimation();

                    animator.Play("Base Layer." + animationState);
                    StartCoroutine(WaitForAnimationStart(animationState, animationTriggers));
                }
            }

            else
            {
                animator.Play("Base Layer." + animationState);
                StartCoroutine(WaitForAnimationStart(animationState, animationTriggers));
            }
        }
    }


    private void EndCurAnimation()
    {
        OnAnimationUpdateEvent?.Invoke(curAnimatorState, AnimationTrigger.Type.End);
        curAnimatorState = null;
    }

    /// <summary>
    /// Wait till the animation begins playing
    /// Update curAnimationState
    /// Send Start AnimationTrigger event
    /// </summary>
    /// <param name="waitingState"></param>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator WaitForAnimationStart(string waitingState, AnimationTrigger[] animationTriggers)
    {
        int waitingHashID = Animator.StringToHash(waitingState);        

        while (waitingHashID != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
        {
            yield return null;
        }

        curAnimatorState = waitingState;
        SetUpAnimationEvents(animationTriggers);        
    }


    /// <summary>
    /// Set up animation for trigger events
    /// </summary>
    /// <param name="animationTriggers"></param>
    private void SetUpAnimationEvents(AnimationTrigger[] animationTriggers)
    {       
        //Reset Triggers
        if (animationTriggers != null)
        {
            foreach (AnimationTrigger trigger in animationTriggers)
                trigger.Reset();
        }

        //Stop previous animation events
        if (animationEventCorutine != null)
            StopCoroutine(animationEventCorutine);

        //Start animation events
        animationEventCorutine = StartCoroutine(CheckAnimationEvents(animationTriggers));

        OnAnimationUpdateEvent?.Invoke(curAnimatorState, AnimationTrigger.Type.Start);
    }


    /// <summary>
    /// Check each frame for any animationTrigger events that need to fire
    /// </summary>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator CheckAnimationEvents(AnimationTrigger[] animationTriggers)
    {
        //Loop while animation is running
        while (GetCurAnimationNormalizedTime() < 1)
        {
            if (animationTriggers != null)
            {
                float curFrame = GetCurrentFrameOfCurAnimation();

                foreach (var trigger in animationTriggers)
                {
                    if (!trigger.WasTriggered && curFrame >= trigger.TriggerFrame)
                    {
                        OnAnimationUpdateEvent?.Invoke(curAnimatorState, trigger.TriggerType);
                        trigger.WasTriggered = true;
                    }
                }
            }

            yield return null;
        }

        //End Animation
        EndCurAnimation();
    }

    #endregion



    #region Animation Perameter Setting

    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    #endregion

}