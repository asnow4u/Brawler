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
internal class AnimationHandler : MonoBehaviour, IAnimation
{
    //Dependencies
    IActionState actionState;
    IStats stats;

    //Components
    private Animator animator;
    private AnimationGraph animationGraph;

    private AnimationClip curPlayingAnimation;

    //Events
    public event Action<AnimationClip> AnimationStartedEvent;
    public event Action<AnimationClip> AnimationEndedEvent;


    #region Getters

    public Animator Animator => animator;
    public bool IsAnimationPaused { get; private set; }


    /// <summary>
    /// Get the current frame that the animation is on
    /// </summary>
    /// <returns></returns>
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
        //Animator
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("AnimationHandler Animator is null", gameObject);

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

    #endregion


    /// <summary>
    /// Check the current animation playing from graph </br>
    /// Invoke events on changes to the currentPlayingAnimation </br>
    /// Determine when an animation ends   
    /// </summary>
    private void Update()
    {
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

    //public void PauseAnimation()
    //{
    //    Debug.Log("Pausing Animation");
    //    animationGraph.Graph.Stop();
    //    IsAnimationPaused = true;
    //}

    //public void ResumeAnimation()
    //{
    //    animationGraph.Graph.Play();
    //    IsAnimationPaused = false;
    //    Debug.Log("Resuming Animation");
    //}

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
}
