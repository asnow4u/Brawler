using System;
using System.Collections;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationHandler : MonoBehaviour, IAnimator
{
    protected SceneObject sceneObj;
    protected Animator animator;

    protected string curAnimatorState = string.Empty;

    protected Coroutine animationEventCorutine;

    public event Action<string, AnimationTrigger.Type> OnAnimationUpdateEvent;

    public void SetUp(SceneObject obj)
    {
        sceneObj = obj;
        animator = GetComponent<Animator>();
    }


    private AnimationClip GetCurrentPlayingAnimation()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
            return clipInfo[0].clip;

        return null;
    }


    public bool TryGetCurrentFrameOfAnimation(string animationState, out float curFrame)
    {
        if (animationState == curAnimatorState)
        {
            AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);

            //Current animation frame
            curFrame = Mathf.RoundToInt(animationInfo.normalizedTime * GetCurrentPlayingAnimation().frameRate);
            return true;
        }

        curFrame = 0f;
        return false;
    }


    public void PlayIdleAnimation()
    {
        if (sceneObj.IsGrounded)
            PlayAnimation("BaseIdle");
        else
            PlayAnimation("BaseAirIdle");
    }


    #region Play Animation

    public void PlayAnimation(string animationState, AnimationTrigger[] animationTriggers = null)
    {        
        if (animationState != null)
        {
            //Check for cur animation playing
            if (curAnimatorState != string.Empty)
            {
                //Check for difference
                if (curAnimatorState != animationState)
                {
                    OnAnimationUpdateEvent?.Invoke(curAnimatorState, AnimationTrigger.Type.End);

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

        if (animationEventCorutine != null)
            StopCoroutine(animationEventCorutine);

        animationEventCorutine = StartCoroutine(CheckAnimationEvents(animationTriggers));

        OnAnimationUpdateEvent?.Invoke(waitingState, AnimationTrigger.Type.Start);
    }


    /// <summary>
    /// Check each frame for any animationTrigger events that need to fire
    /// </summary>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator CheckAnimationEvents(AnimationTrigger[] animationTriggers)
    {
        //Get Animation info
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        float length = animationInfo.length;
        float curNormalizedTime = animationInfo.normalizedTime;

        //Reset Triggers
        if (animationTriggers != null)
        {
            foreach(AnimationTrigger trigger in animationTriggers)
            {
                trigger.Reset();
            }
        }

        //Fire Triggers when needed
        while (curNormalizedTime < 1)
        {
            if (animationTriggers != null && TryGetCurrentFrameOfAnimation(curAnimatorState, out float curFrame))
            {
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
        OnAnimationUpdateEvent?.Invoke(curAnimatorState, AnimationTrigger.Type.End);
    }

    #endregion



    #region Animation Perameter Setting

    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    #endregion

}