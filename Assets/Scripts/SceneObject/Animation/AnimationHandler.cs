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

    public event Action<string> OnAnimationStateStartedEvent;
    public event Action<string> OnAnimationStateEndedEvent;
    public event Action<AnimationClip, string> OnAnimationTriggerEvent;

    public void SetUp(SceneObject obj)
    {
        sceneObj = obj;
        animator = GetComponent<Animator>();
    }


    public void PlayIdleAnimation()
    {
        if (sceneObj.isGrounded)
            PlayAnimation("BaseIdle");
        else
            PlayAnimation("BaseAirIdle");
    }


    public void PlayAnimation(string animationState)
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
                    OnAnimationStateEndedEvent?.Invoke(curAnimatorState);

                    animator.Play("Base Layer." + animationState);
                    StartCoroutine(WaitForAnimationStart(animationState));

                }
            }

            else
            {
                Debug.Log("start clip: " + animationState);

                animator.Play("Base Layer." + animationState);
                StartCoroutine(WaitForAnimationStart(animationState));
            }
        }
    }


    private AnimationClip GetCurrentPlayingAnimation()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
            return clipInfo[0].clip;

        return null;
    }


    private IEnumerator WaitForAnimationStart(string waitingState)
    {
        int waitingHashID = Animator.StringToHash(waitingState);        

        while (waitingHashID != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
        {
            yield return null;
        }

        Debug.Log("updated clip: " + waitingState);
        curAnimatorState = waitingState;

        OnAnimationStateStartedEvent?.Invoke(waitingState);
    }



    #region Animation Perameter Setting

    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    #endregion


    #region AnimationEvents

    public void OnAnimationEnded()
    {
        OnAnimationStateEndedEvent?.Invoke(curAnimatorState);
    }

    public void OnAntimationTrigger(string trigger)
    {
        AnimationClip clip = GetCurrentPlayingAnimation();
        OnAnimationTriggerEvent?.Invoke(clip, trigger);
    }

    #endregion



    //private IEnumerator WaitForAnimationFinished(string animation)
    //{
    //    int animationHash = Animator.StringToHash(animation);

    //    while (animationHash != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
    //    {
    //        yield return null;
    //    }

    //    if (animator.GetCurrentAnimatorStateInfo(0).loop) yield break;

    //    while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
    //    {
    //        yield return null;
    //    }

    //    Debug.Log("Animation " + animation + " finished");
    //}
}