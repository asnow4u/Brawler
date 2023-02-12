using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*NOTE: Priority Levels
 * 0 => Idle state
 * 1 => Movement state
 * 2 => Action state
 * 3 => Hurt state
 */

public class AnimatorController : MonoBehaviour
{
    private Animator animator;

    private string currentState;    
    private string bufferedState;
    private int currentPriorityLevel = 0;
    private int bufferedPriorityLevel;

    private Coroutine coroutine;


    private IEnumerator Delay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        if (bufferedState != null)
        {
            animator.Play(bufferedState);
            Debug.Log("Anmation => " + bufferedState);

            currentState = bufferedState;
            currentPriorityLevel = bufferedPriorityLevel;

            coroutine = StartCoroutine(Delay(animator.GetCurrentAnimatorStateInfo(0).length));
        }

        else
        {
            animator.Play("Idle");
            Debug.Log("Anmation => " + "Idle");

            currentState = "Idle";
            currentPriorityLevel = 0;
        }
    }


    private void Start()
    {
        animator = GetComponent<Animator>();
    }


    public void ChangeAnimationState(string newState, int priorityLevel, bool overwrite)
    {
        if (currentState == newState) return;
                
        //Buffer lower priority
        if (priorityLevel < currentPriorityLevel)
        {
            bufferedState = newState;
            bufferedPriorityLevel = priorityLevel;
            return;
        }

        //Buffer same priority that dosent overwrite
        if (priorityLevel == currentPriorityLevel && !overwrite)
        {
            bufferedState = newState;
            bufferedPriorityLevel = priorityLevel;
            return;
        }

        //Stop existing coroutine
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
        }

        animator.Play(newState);
        Debug.Log("Anmation => " + newState);

        currentState = newState;
        currentPriorityLevel = priorityLevel;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == newState)
            {
                coroutine = StartCoroutine(Delay(clip.length));
                break;
            }
        }  
    }
}
