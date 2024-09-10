using RayAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


/*NOTES: 
    *   The playable api bypasses the animator controller when playing animations
        Methods like animator.GetCurrentAnimatorClipInfo() will not work while using the playable api
*/


public class AnimationHandler : MonoBehaviour
{
    private Animator animator;
    private AnimationGraph animationGraph;

    private SceneObject sceneObject;

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

    #region Initialize

    public void Setup()
    {
        //Animator
        animator = GetComponentInChildren<Animator>();
        Debug.Assert(animator != null, "Animator is Null!", gameObject);
        Debug.Assert(groundIdleAnimation != null, "Ground Idle Animation not set!", gameObject);
        Debug.Assert(airIdleAnimation != null, "Air Idle Animation not set!", gameObject);

        sceneObject = GetComponent<SceneObject>();
        animationGraph = animator.gameObject.AddComponent<AnimationGraph>();

        SetUpEventListeners();
    }


    public void Initialize()
    {
        animationGraph.Initialize();
        SetAnimationToGraph();
        animationGraph.ResetToIdle(sceneObject.CurGroundedState);
    }

    #endregion


    #region AnimationGraph

    /// <summary>
    /// Create animationGraph and set animations
    /// </summary>
    private void SetAnimationToGraph()
    {       
        SetIdleAnimations();
        SetMovementAnimations();
        SetAttackAnimations();
        //SetHitStunAnimations();
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
    private void SetUpEventListeners()
    {
        //Action State Change        
        sceneObject.ActionStateHandler.ActionStateChangedEvent += OnActionStateChanged;

        //Ground State Changed
        sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;

        //Move State Changed        
        sceneObject.MovementInputHandler.MoveStateChangedEvent += OnMovementStateChanged;

        //Attack State Changed
        sceneObject.AttackInputHandler.AttackStateChangedEvent += OnAttackStateChanged;
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
        if (movementState == MovementType.Null)
        {
            if (sceneObject.ActionStateHandler.CurActionState == ActionState.Moving)
               sceneObject.ActionStateHandler.ChangeState(ActionState.Idle);     
        }
        else
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
    

    /// <summary>
    /// Check the current animation playing from graph </br>
    /// Invoke events on changes to the currentPlayingAnimation </br>
    /// Determine when an animation ends   
    /// </summary>
    private void Update()
    {
        //Get animationClip from graph
        AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();

        if (clipPlayable.GetAnimationClip() != null)
        {
            //Check if animation changed
            if (curPlayingAnimation != clipPlayable.GetAnimationClip())
            {
                if (curPlayingAnimation != null)
                    AnimationEndedEvent?.Invoke(curPlayingAnimation);

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



    public void EndAnimation(AnimationClip clip)
    {        
        if (curPlayingAnimation != null &&
            curPlayingAnimation == clip)
        {
            GetComponent<ActionStateHandler>().ChangeState(ActionState.Idle);
        }
    }
}
