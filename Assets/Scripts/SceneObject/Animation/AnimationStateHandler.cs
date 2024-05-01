using System;
using System.Collections;
using UnityEditor.Animations;
using UnityEngine;

public enum ActionState
{
    Null, Idle, Moving, Attacking, HitStun, Dead
};


public class AnimationStateHandler : MonoBehaviour, IAnimator
{
    [SerializeField] private ActionState curActionState;
    [SerializeField] private string curPlayingAnimation;

    private Coroutine animationEventCorutine;

    //Getter
    private Animator animator => GetComponentInChildren<Animator>();
    private MovementInputHandler movementHandler => GetComponent<MovementInputHandler>();
    private AttackInputHandler attackHandler => GetComponent<AttackInputHandler>();

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
    private float GetCurrentFrameOfCurAnimation()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return Mathf.RoundToInt(animationInfo.normalizedTime * GetCurrentPlayingAnimation().frameRate);            
    }


    //TODO: Figure out a better flow for determining action state from animation
    private ActionState DetermineActionStateFromAnimation(string animationName)
    {
        if (animationName.Contains("Idle"))
            return ActionState.Idle;

        else if (animationName.Contains("Hit"))
            return ActionState.HitStun;

        else if (animationName.Contains("Move"))
            return ActionState.Moving;

        else
        {
            if (movementHandler.CurMovementCollection.TryGetMovementFromAnimation(animationName, out MovementData moveData))
                return ActionState.Moving;

            if (attackHandler.CurAttackCollection.TryGetAttackByAnimation(animationName, out AttackData attackData))
                return ActionState.Attacking;
        }

        return ActionState.Null;
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


    private bool TryChangeState(ActionState newState)
    {
        if (IsStatePossible(newState))
        {
            Debug.Log("STATE: " + newState);
            curActionState = newState;
            return true;
        }

        return false;
    }


    private void ResetState()
    {
        curActionState = ActionState.Idle;
    }

    #endregion


    #region Play Animation
    
    private void PlayIdleAnimation()
    {
        if (movementHandler.GroundedState == GroundedState.Airborn)
            PlayAnimation("BaseAirIdle");
        else
            PlayAnimation("BaseIdle");
    }
    
    /// <summary>
    /// Update the current ActionState <br/>
    /// Start playing animation.
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="animationTriggers"></param>
    public void PlayAnimation(string animationName, AnimationTrigger[] animationTriggers = null)
    {        
        if (animationName != null)
        {
            ActionState animationActionState = DetermineActionStateFromAnimation(animationName);

            if (TryChangeState(animationActionState))
            {
                if (animationName != curPlayingAnimation)
                {
                    Debug.Log("ANIMATION: " + animationName);
                    animator.Play("Base Layer." + animationName);
                    StartCoroutine(WaitForAnimationStart(animationName, animationTriggers));
                }                
            }
        }
    }


    /// <summary>
    /// Reset action state and play idle animation
    /// </summary>
    public void EndCurrentAnimation()
    {
        ResetState();
        PlayIdleAnimation();
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
            OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, AnimationTrigger.Type.End);

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
        EndCurrentAnimation();
    }


    private void CheckForAnimationTrigger(AnimationTrigger[] animationTriggers)
    {
        if (animationTriggers != null)
        {
            float curFrame = GetCurrentFrameOfCurAnimation();

            foreach (var trigger in animationTriggers)
            {
                if (!trigger.WasTriggered && curFrame >= trigger.TriggerFrame)
                {
                    OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, trigger.TriggerType);
                    trigger.WasTriggered = true;
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