using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class AnimationHandler : MonoBehaviour, IAnimator
{
    protected SceneObject sceneObj;
    protected Animator animator;

    private Coroutine finishAnimationCheck;

    public event Action<AnimationClip> OnAnimationStartedEvent;
    public event Action<AnimationClip> OnAnimationEndedEvent;
    public event Action<AnimationClip, string> OnAnimationTriggerEvent;

    public void SetUp(SceneObject obj)
    {
        sceneObj = obj;
        animator = GetComponent<Animator>();
    }


    public void PlayAnimation(string animation)
    {
        animator.Play("Base Layer." + animation);

        if (finishAnimationCheck != null)
            StopCoroutine(finishAnimationCheck);

        finishAnimationCheck = StartCoroutine(CheckIfAnimationFinished(animation));
    }


    private IEnumerator CheckIfAnimationFinished(string animation)
    {
        int animationHash = Animator.StringToHash(animation);

        while (animationHash != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
        {
            yield return null;
        }

        if (animator.GetCurrentAnimatorStateInfo(0).loop) yield break;

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
        {
            yield return null;
        }

        Debug.Log("Animation " + animation + " finished");
    }


    public void PlayIdleAnimation()
    {        
        if (sceneObj.isGrounded)
            PlayAnimation("BaseIdle");
        else
            PlayAnimation("BaseAirIdle");      
    }


    private AnimationClip GetCurrentPlaingAnimation()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        return clipInfo[0].clip;
    }


    #region Animation Perameter Setting

    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    #endregion

    #region AnimationEvents


    public void OnAnimationStarted()
    {
        AnimationClip clip = GetCurrentPlaingAnimation();

        OnAnimationStartedEvent?.Invoke(clip);
    }

    public void OnAnimationEnded()
    {
        AnimationClip clip = GetCurrentPlaingAnimation();

        OnAnimationEndedEvent?.Invoke(clip);
    }


    //TODO: Figure out how to do this
    public void OnAntimationTrigger(string trigger)
    {
        AnimationClip clip = GetCurrentPlaingAnimation();
        OnAnimationTriggerEvent?.Invoke(clip, trigger);
    }

    #endregion

}