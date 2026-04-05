using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


/*NOTE: 
*   The playable api bypasses the animator controller when playing animations
    Methods like animator.GetCurrentAnimatorClipInfo() will not work while using the playable api
*/

[RequireComponent(typeof(IActionState))]
[RequireComponent(typeof(IStats))]
public class AnimationHandler : MonoBehaviour, IAnimation, IAnimationEditor
{
    //Dependencies
    IActionState actionState;
    IStats stats;
    
    private Animator animator;
    private AnimationEventHandler eventHandler;    
    private AnimationGraph animationGraph;

    private AnimationClip curPlayingAnimation;

    //Events
    public event Action<AnimationClip> AnimationStartedEvent;
    public event Action<AnimationClip> AnimationEndedEvent;
    public event Action<AnimationEventState> AnimationEventFiredEvent;

    #region Getters

    public int GetFrameOfCurrentAnimation()
    {
        AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();

        double wrappedTime = clipPlayable.GetTime() % clipPlayable.GetAnimationClip().length;

        float frameRate = clipPlayable.GetAnimationClip().frameRate;
        int currentFrame = Mathf.FloorToInt((float)wrappedTime * frameRate);

        return currentFrame;
    }

    #endregion


    #region Initialize / Destroy

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("AnimationHandler Animator is null", gameObject);
        
        eventHandler = animator.GetComponentInChildren<AnimationEventHandler>();
        if (eventHandler == null)
            Debug.LogError("AnimationHandler AnimatorEventHandler is null", gameObject);

        actionState = GetComponent<IActionState>();
        if (actionState == null)
            Debug.LogError("AnimationHandler IActionState is null", gameObject);

        stats = GetComponent<IStats>();
        if (stats == null)
            Debug.LogError("AnimationHandler IEquipment is null", gameObject);

        animationGraph = new AnimationGraph(animator);
        
        RegisterToEvents();
    }

    public void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        actionState.IdleStateChangedEvent += OnIdleStateChanged;
        actionState.MovementStateChangedEvent += OnMovementStateChanged;
        actionState.AttackStateChangedEvent += OnAttackStateChanged;

        stats.AnimationStatsChangedEvent += OnAnimationStatsChanged;

        eventHandler.OnEventFired += HandleAnimationEvent;
    }

    private void OnDestroy()
    {
        UnregisterToEvents();

        animationGraph.Dispose();
    }

    private void UnregisterToEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        actionState.IdleStateChangedEvent -= OnIdleStateChanged;
        actionState.MovementStateChangedEvent -= OnMovementStateChanged;
        actionState.AttackStateChangedEvent -= OnAttackStateChanged;

        stats.AnimationStatsChangedEvent -= OnAnimationStatsChanged;

        eventHandler.OnEventFired += HandleAnimationEvent;
    }

    #endregion 


    #region Events       
    private void OnActionStateChanged(ActionState actionState)
    {
        animationGraph.ChangeActionStateInput(actionState);
    }

    private void OnIdleStateChanged(IdleState idleState)
    {
        if (idleState == IdleState.Null)
            return;

        animationGraph.ChangeIdleStateInput(idleState);
    }

    private void OnMovementStateChanged(MovementState moveState)
    {
        if (moveState == MovementState.Null)
            return;

        animationGraph.ChangeMovementStateInput(moveState);        
    }

    private void OnAttackStateChanged(AttackState attackState)
    {
        if (attackState == AttackState.Null)
            return;

        animationGraph.ChangeAttackStateInput(attackState);
    }

    private void OnAnimationStatsChanged(AnimationStatData data)
    {
        animationGraph.SetIdleAnimations(data.GroundedIdleAnimation, data.AirIdleAnimation, data.ClimbIdleAnimation);
        animationGraph.SetMovementAnimations(data.MovementAnimations);
        animationGraph.SetAttackAnimations(data.AttackAnimations);
        animationGraph.SetHitStunAnimations(data.HitStunAnimation);
    }

    private void HandleAnimationEvent(AnimationEventState state)
    {
        AnimationEventFiredEvent?.Invoke(state);
    }

    #endregion


    /// <summary>
    /// Check the current animation playing from graph </br>
    /// Invoke events on changes to the currentPlayingAnimation </br>
    /// Determine when an animation ends   
    /// </summary>
    private void Update()
    {
        if (debugMode)
        {
            DebugUpdate();
            return;
        }

        //Get animationClip from graph
        AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();

        if (!clipPlayable.IsNull() && clipPlayable.GetAnimationClip() != null)
        {
            //Check if animation changed
            if (curPlayingAnimation != clipPlayable.GetAnimationClip())
            {
                if (curPlayingAnimation != null)
                    EndAnimation(curPlayingAnimation);                                               

                clipPlayable.Play();
                curPlayingAnimation = clipPlayable.GetAnimationClip();
                AnimationStartedEvent?.Invoke(curPlayingAnimation);
            }


            //Determine when the clip ends
            if (!curPlayingAnimation.isLooping)
            {
                if (clipPlayable.GetTime() > curPlayingAnimation.length)
                    EndAnimation(curPlayingAnimation);
            }
        }
    }

    /// <summary>
    /// End the animation if currently playing and go to Idle
    /// </summary>
    public void EndAnimation(AnimationClip clip)
    {
        if (curPlayingAnimation != null &&
            curPlayingAnimation == clip)
        {
            AnimationEndedEvent?.Invoke(curPlayingAnimation);
        }
    }


    #region Debug / Editor

    private bool debugMode = false;
    public bool DebugMode => debugMode;
    
    private AttackState debugAttackState = AttackState.Null;
    public AttackState DebugAttackState => debugAttackState;

    private bool isDebugAnimationPlaying;
    public bool IsDebugAnimationPlaying => isDebugAnimationPlaying;

    private float currentDebugAnimationTime;
    public float CurrentDebugAnimationTime => currentDebugAnimationTime;

    private AnimationClip currentDebugAnimationClip;
    public AnimationClip CurrentDebugAnimationClip => currentDebugAnimationClip;

    private AnimationClipPlayable currentDebugClipPlayable;
    private float previousDebugAnimationTime;

    private void DebugUpdate()
    {
        if (currentDebugAnimationClip == null)
            return;

        if (isDebugAnimationPlaying)
        {
            previousDebugAnimationTime = currentDebugAnimationTime;
            currentDebugAnimationTime += Time.deltaTime;

            ClampTime();

            if (currentDebugAnimationTime >= currentDebugAnimationClip.length)
                isDebugAnimationPlaying = false;
        }

        UpdatePlayable();
    }

    private void ClampTime()
    {
        if (currentDebugAnimationClip == null)
            return;

        currentDebugAnimationTime = Mathf.Clamp(currentDebugAnimationTime, 0f, currentDebugAnimationClip.length);
    }

    private void UpdatePlayable()
    {
        if (currentDebugClipPlayable.IsNull())
            return;

        currentDebugClipPlayable.SetTime(currentDebugAnimationTime);
    }

    public void SetDebugMode(bool enabled)
    {
        debugMode = enabled;

        if (!debugMode)
        {
            isDebugAnimationPlaying = false;
            currentDebugAnimationClip = null;
        }
    }

    public void SetDebugAttackState(AttackState state)
    {
        debugAttackState = state;

        if (state == AttackState.Null)
        {
            isDebugAnimationPlaying = false;
            currentDebugAnimationClip = null;
            return;
        }

        OnActionStateChanged(ActionState.Attacking);
        OnAttackStateChanged(state);

        AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();
        if (clipPlayable.IsNull() || clipPlayable.GetAnimationClip() == null)
            return;

        currentDebugClipPlayable = clipPlayable;
        currentDebugAnimationClip = clipPlayable.GetAnimationClip();

        currentDebugAnimationTime = 0f;
        previousDebugAnimationTime = 0f;
    }

    public void PlayDebugAnimation()
    {
        if (currentDebugAnimationTime >= currentDebugAnimationClip.length)
        {
            currentDebugAnimationTime = 0f;
            UpdatePlayable();
        }

        isDebugAnimationPlaying = true;
    }

    public void PauseDebugAnimation()
    {
        isDebugAnimationPlaying = false;
    }

    public void JumpDebugAnimationToStart()
    {
        if (currentDebugAnimationClip == null || isDebugAnimationPlaying) return;

        previousDebugAnimationTime = currentDebugAnimationTime;
        currentDebugAnimationTime = 0f;
        UpdatePlayable();
    }

    public void JumpDebugAnimationToEnd()
    {
        if (currentDebugAnimationClip == null || isDebugAnimationPlaying) return;

        previousDebugAnimationTime = currentDebugAnimationTime;
        currentDebugAnimationTime = currentDebugAnimationClip.length;
        UpdatePlayable();
    }

    public void StepDebugAnimationForward()
    {
        if (currentDebugAnimationClip == null || isDebugAnimationPlaying) return;

        float frameTime = 1f / currentDebugAnimationClip.frameRate;

        previousDebugAnimationTime = currentDebugAnimationTime;
        currentDebugAnimationTime += frameTime;

        ClampTime();
        UpdatePlayable();
    }

    public void StepDebugAnimationBackward()
    {
        if (currentDebugAnimationClip == null || isDebugAnimationPlaying) return;

        float frameTime = 1f / currentDebugAnimationClip.frameRate;

        previousDebugAnimationTime = currentDebugAnimationTime;
        currentDebugAnimationTime -= frameTime;

        ClampTime();
        UpdatePlayable();
    }    
    
    #endregion
}
