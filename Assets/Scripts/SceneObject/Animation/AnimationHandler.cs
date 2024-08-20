using RayAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationHandler : MonoBehaviour
{
    private Animator animator;
    private AnimationGraph animationGraph;

    [SerializeField] private AnimationClip groundIdleAnimation;
    [SerializeField] private AnimationClip airIdleAnimation;

    [SerializeField] private AnimationClip curPlayingAnimation;

    //Coroutines
    private Coroutine animationEventCorutine;

    //Events
    public event Action<AnimationClip> AnimationStartedEvent;
    public event Action<AnimationClip> AnimationEndedEvent;


    #region Getters

    public Animator Animator => animator;
    public AnimationClip GroundIdleAnimation => groundIdleAnimation;
    public AnimationClip AirIdleAnimation => airIdleAnimation;

    /// <summary>
    /// Return the current running animation clip info
    /// </summary>
    /// <returns></returns>
    public AnimationClip GetCurrentPlayingAnimation()
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
    public float GetCurrentAnimationNormalizedTime()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.normalizedTime;
    }


    public bool IsCurrentAnimationLooping()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.loop;
    }


    /// <summary>
    /// Returns the current frame of the playing animation
    /// </summary>
    /// <returns></returns>
    public int GetFrameOfCurrentAnimation()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return Mathf.RoundToInt(animationInfo.normalizedTime * GetCurrentPlayingAnimation().frameRate);
    }

    #endregion


    public void Initialize()
    {
        //Animator
        animator = GetComponentInChildren<Animator>();
        Debug.Assert(animator != null, "Animator is Null!", gameObject);
        Debug.Assert(groundIdleAnimation != null, "Ground Idle Animation not set!", gameObject);
        Debug.Assert(airIdleAnimation != null, "Air Idle Animation not set!", gameObject);

        SetupAnimationGraph();       
        SetUpEvents();
    }


    #region AnimationGraph

    /// <summary>
    /// Create animationGraph and set animations
    /// </summary>
    private void SetupAnimationGraph()
    {
        animationGraph = new AnimationGraph(animator);

        SetIdleAnimations();
        SetMovementAnimations();
        SetAttackAnimations();
        SetHitStunAnimations();
    }


    /// <summary>
    /// Set animationGraphs idle animations
    /// </summary>
    private void SetIdleAnimations()
    {
        animationGraph.SetIdleAnimations(groundIdleAnimation, airIdleAnimation);
    }


    /// <summary>
    /// Set animationGraphs movement animations
    /// </summary>
    private void SetMovementAnimations()
    {
        if (TryGetComponent(out MovementInputHandler movementInputHandler))
        {
            if (movementInputHandler.CurMovementCollection != null)
                animationGraph.SetMovementAnimations(movementInputHandler.CurMovementCollection);            
        }
    }


    /// <summary>
    /// Set animationGraphs attack animations
    /// </summary>
    private void SetAttackAnimations()
    {
        if (TryGetComponent(out AttackInputHandler attackInputHandler))
        {
            if (attackInputHandler.CurAttackCollection != null)
                animationGraph.SetAttackAnimations(attackInputHandler.CurAttackCollection);            
        }
    }


    /// <summary>
    /// Set animationGraphs hitstun animations
    /// </summary>
    private void SetHitStunAnimations()
    {
        animationGraph.SetHitStunAnimations();
    }

    #endregion


    #region Events

    /// <summary>
    /// Listen to needed events
    /// </summary>
    private void SetUpEvents()
    {
        //Action State Change
        if (TryGetComponent(out ActionStateHandler actionStateHandler))
            actionStateHandler.ActionStateChangedEvent += OnActionStateChanged;

        //Ground State Changed
        SceneObject sceneObject = GetComponent<SceneObject>();
        sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;

        //Move State Changed
        if (TryGetComponent(out MovementInputHandler movementHandler))
            movementHandler.MoveStateChangedEvent += OnMovementStateChanged;

        //Attack State Changed
        if (TryGetComponent(out AttackInputHandler attackHandler))
            attackHandler.AttackStateChangedEvent += OnAttackStateChanged;
    }


    /// <summary>
    /// Action State Changed
    /// </summary>
    /// <param name="actionState"></param>
    private void OnActionStateChanged(ActionState actionState)
    {
        animationGraph.ChangeActionStateInput(actionState);
    }


    /// <summary>
    /// Grounded State Changed
    /// </summary>
    /// <param name="groundedState"></param>
    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        animationGraph.ChangeGroundedStateInput(groundedState);
    }

    
    /// <summary>
    /// Movement state changed
    /// </summary>
    /// <param name="movementState"></param>
    private void OnMovementStateChanged(MovementType movementState)
    {
        animationGraph.ChangeMovementStateInput(movementState);
    }


    /// <summary>
    /// Attack state changed
    /// </summary>
    /// <param name="attackState"></param>
    private void OnAttackStateChanged(AttackType attackState)
    {
        animationGraph.ChangeAttackStateInput(attackState);
    }

    #endregion


    private void Update()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
        {
            //Check if animation changed
            if (curPlayingAnimation != clipInfo[0].clip)
            {
                Debug.Log("ANIMATION: Ended " + curPlayingAnimation);
                AnimationEndedEvent?.Invoke(curPlayingAnimation);

                curPlayingAnimation = clipInfo[0].clip;                                

                Debug.Log("ANIMATION: Started " + curPlayingAnimation);
                AnimationStartedEvent?.Invoke(curPlayingAnimation);
            }

            if (!IsCurrentAnimationLooping())
            {
                if (GetCurrentAnimationNormalizedTime() == 1)
                {
                    EndAnimation(curPlayingAnimation);
                }
            }
        }
    }

   

    public void EndAnimation(AnimationClip clip)
    {        
        if (curPlayingAnimation != null &&
            curPlayingAnimation == clip)
        {
            GetComponent<ActionStateHandler>().ChangeState(ActionState.Idle);
        }
    }
}
