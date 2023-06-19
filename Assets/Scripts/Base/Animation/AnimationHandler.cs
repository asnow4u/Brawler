using System.Collections;
using UnityEngine;

public class AnimationHandler : MonoBehaviour, IAnimator
{
    protected SceneObject sceneObj;
    protected Animator animator;

    private Coroutine finishAnimationCheck;

    public void SetUp(SceneObject obj)
    {
        sceneObj = obj;
        animator = GetComponentInChildren<Animator>();
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
        //TODO: Will be based on equipment
        if (sceneObj.isGrounded)
            PlayAnimation("BaseIdle");
        else
            PlayAnimation("BaseAirIdle");      
    }

    
    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

}

