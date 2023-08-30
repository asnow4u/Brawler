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


    public void PlayIdleAnimation()
    {
        if (sceneObj.isGrounded)
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
                    Debug.Log("end clip: " + curAnimatorState);
                    OnAnimationUpdateEvent?.Invoke(curAnimatorState, AnimationTrigger.Type.End);

                    animator.Play("Base Layer." + animationState);
                    StartCoroutine(WaitForAnimationStart(animationState, animationTriggers));
                }
            }

            else
            {
                Debug.Log("start clip: " + animationState);

                animator.Play("Base Layer." + animationState);
                StartCoroutine(WaitForAnimationStart(animationState, animationTriggers));
            }
        }
    }


    private IEnumerator WaitForAnimationStart(string waitingState, AnimationTrigger[] animationTriggers)
    {
        int waitingHashID = Animator.StringToHash(waitingState);        

        while (waitingHashID != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
        {
            yield return null;
        }

        Debug.Log("updated clip: " + waitingState);
        curAnimatorState = waitingState;

        if (animationEventCorutine != null)
            StopCoroutine(animationEventCorutine);

        animationEventCorutine = StartCoroutine(CheckAnimationEvents(animationTriggers));

        OnAnimationUpdateEvent?.Invoke(waitingState, AnimationTrigger.Type.Start);
    }


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
                trigger.WasTriggered = false;
            }
        }

        //Fire Triggers when needed
        while (curNormalizedTime < 1)
        {
            animationInfo = animator.GetCurrentAnimatorStateInfo(0);
            curNormalizedTime = animationInfo.normalizedTime;

            if (animationTriggers != null)
            {
                foreach (var trigger in animationTriggers)
                {
                    if (!trigger.WasTriggered && curNormalizedTime >= trigger.NoramlizedTriggerTime)
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