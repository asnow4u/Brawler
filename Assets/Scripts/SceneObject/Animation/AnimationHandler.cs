using System;
using UnityEngine;


/*NOTE: 
*   The playable api bypasses the animator controller when playing animations
    Methods like animator.GetCurrentAnimatorClipInfo() will not work while using the playable api
*/

[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(HurtBoxHandler))]
public class AnimationHandler : MonoBehaviour, IAnimation
{
    //Dependencies
    IActionState actionState;
    IStats statHandler;
    IHurtBoxHandler hurtBoxHandler;
    
    private Animator animator;
    private AnimationEventHandler eventHandler;    
    private AnimationGraph animationGraph;

    [SerializeField] RuntimeAnimatorController idleController;
    [SerializeField] RuntimeAnimatorController movementController;
    [SerializeField] RuntimeAnimatorController attackController;
    [SerializeField] RuntimeAnimatorController hitStunController;

    //Events    
    public event Action<AnimationEventState> AnimationEventFiredEvent;

    #region Getters

    public float GetCurrentAnimationDelta()
    {
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.normalizedTime % 1f;
    }

    #endregion


    #region Initialize / Destroy

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
            Debug.LogError("AnimationHandler Animator is null", gameObject);        
        animator.runtimeAnimatorController = null; //Ensure animator controller is null to avoid conflicts with playable graph

        eventHandler = animator.GetComponentInChildren<AnimationEventHandler>();
        if (eventHandler == null)
            Debug.LogError("AnimationHandler AnimatorEventHandler is null", gameObject);

        if (idleController == null || hitStunController == null)
            Debug.LogError("AnimationHandler IdleController and/or HitStunController are null", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();

        animationGraph = new AnimationGraph(animator, idleController, movementController, attackController, hitStunController);
        
        RegisterToEvents();
    }

    public void RegisterToEvents()
    {
        statHandler.AnimationStatsChangedEvent += OnAnimationStatsChanged;

        actionState.ActionStateChangedEvent += OnActionStateChanged;
        actionState.IdleStateChangedEvent += OnIdleStateChanged;
        actionState.MovementStateChangedEvent += OnMovementStateChanged;
        actionState.AttackStateChangedEvent += OnAttackStateChanged;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;

        eventHandler.OnEventFired += HandleAnimationEvent;
    }

    private void OnDestroy()
    {
        UnregisterToEvents();

        animationGraph.Dispose();
    }

    private void UnregisterToEvents()
    {
        statHandler.AnimationStatsChangedEvent -= OnAnimationStatsChanged;
        
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        actionState.IdleStateChangedEvent -= OnIdleStateChanged;
        actionState.MovementStateChangedEvent -= OnMovementStateChanged;
        actionState.AttackStateChangedEvent -= OnAttackStateChanged;
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;

        eventHandler.OnEventFired -= HandleAnimationEvent;
    }

    private void OnAnimationStatsChanged(AnimationStatData data)
    {
        animationGraph.SetIdleAnimations(data.GroundedIdleAnimation, data.AirIdleAnimation);
        animationGraph.SetMovementAnimations(data.MovementAnimations);
        animationGraph.SetAttackAnimations(data.AttackAnimations);
        animationGraph.SetHitStunAnimations(data.HitStunAnimation);
    }


    private void OnActionStateChanged(ActionState actionState)
    {
        animationGraph.OnActionStateChanged(actionState);
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

    private void OnHitStunStateChanged(HitStunState hitStunState)
    {
        if (hitStunState == HitStunState.Null)
            return;

        animationGraph.ChangeHitStunStateInput(hitStunState);
    }

    private void HandleAnimationEvent(AnimationEventState state)
    {
        AnimationEventFiredEvent?.Invoke(state);
    }

    #endregion


    private void Update()
    {
        animationGraph.Update();
    }

    public void PauseAnimation(float seconds)
    {
        animationGraph.Pause(seconds);
    }
}
