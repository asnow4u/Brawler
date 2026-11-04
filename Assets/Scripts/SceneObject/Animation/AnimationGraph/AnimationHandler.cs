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
    ISOHurtBoxHandler hurtBoxHandler;

    IMovementAction movementHandler;
    IAttack attackHandler;
    IGuard guardHandler;
    IHitBoxHandler[] hitBoxHandlers;
    IRootMotion rootMotionReceiver;

    private Animator animator;
    private RootMotionRelay rootMotionRelay;
    private AnimationGraph animationGraph;

    [SerializeField] RuntimeAnimatorController idleController;
    [SerializeField] RuntimeAnimatorController movementController;
    [SerializeField] RuntimeAnimatorController guardController;
    [SerializeField] RuntimeAnimatorController attackController;
    [SerializeField] RuntimeAnimatorController staggerController;
    [SerializeField] RuntimeAnimatorController hitStunController;

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
        animator.applyRootMotion = true;

        if (!animator.TryGetComponent(out rootMotionRelay))
            rootMotionRelay = animator.gameObject.AddComponent<RootMotionRelay>();

        // eventHandler = animator.GetComponentInChildren<AnimationEventHandler>();
        // if (eventHandler == null)
        //     Debug.LogError("AnimationHandler AnimatorEventHandler is null", gameObject);

        if (idleController == null || hitStunController == null)
            Debug.LogError("AnimationHandler IdleController and/or HitStunController are null", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        hurtBoxHandler = GetComponent<ISOHurtBoxHandler>();
        movementHandler = GetComponent<IMovementAction>();
        attackHandler = GetComponent<IAttack>();
        guardHandler = GetComponent<IGuard>();
        hitBoxHandlers = GetComponents<IHitBoxHandler>();
        rootMotionReceiver = GetComponent<IRootMotion>();
        animationGraph = new AnimationGraph(animator, idleController, movementController, guardController, attackController, staggerController, hitStunController);
        
        RegisterToEvents();
    }

    private void Start()
    {
        if (guardHandler == null)
            return;

        if (guardHandler.GuardAnimation != null)
            animationGraph.SetGuardAnimation(new AnimationStatData.AnimationData(guardHandler.GuardAnimation, guardHandler.GuardAnimationSpeed));

        if (guardHandler.CounterAnimation != null)
            animationGraph.SetCounterAnimation(new AnimationStatData.AnimationData(guardHandler.CounterAnimation, guardHandler.CounterAnimationSpeed));
    }

    public void RegisterToEvents()
    {
        statHandler.AnimationStatsChangedEvent += OnAnimationStatsChanged;

        actionState.ActionStateChangedEvent += OnActionStateChanged;
        actionState.IdleStateChangedEvent += OnIdleStateChanged;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
        hurtBoxHandler.StaggerStateChangedEvent += OnStaggerStateChanged;
        rootMotionRelay.RootMotionEvaluatedEvent += OnRootMotionEvaluated;

        if (movementHandler != null)
            movementHandler.MovementStateChangedEvent += OnMovementStateChanged;
        
        if (attackHandler != null)
            attackHandler.AttackStateChangedEvent += OnAttackStateChanged;

        if (guardHandler != null)
            guardHandler.GuardStateChangedEvent += OnGuardStateChanged;

        if (hitBoxHandlers != null)
        {
            foreach (IHitBoxHandler hitBoxHandler in hitBoxHandlers)
                hitBoxHandler.OnHitConnected += OnHitConnected;
        }
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
        
        
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
        hurtBoxHandler.StaggerStateChangedEvent -= OnStaggerStateChanged;
        rootMotionRelay.RootMotionEvaluatedEvent -= OnRootMotionEvaluated;

        if (movementHandler != null)
            movementHandler.MovementStateChangedEvent -= OnMovementStateChanged;

        if (attackHandler != null)
            attackHandler.AttackStateChangedEvent -= OnAttackStateChanged;

        if (guardHandler != null)
            guardHandler.GuardStateChangedEvent -= OnGuardStateChanged;

        if (hitBoxHandlers != null)
        {
            foreach (IHitBoxHandler hitBoxHandler in hitBoxHandlers)
                hitBoxHandler.OnHitConnected -= OnHitConnected;
        }
    }

    private void OnAnimationStatsChanged(AnimationStatData data)
    {
        animationGraph.SetIdleAnimations(data.GroundedIdleAnimation, data.AirIdleAnimation);
        animationGraph.SetMovementAnimations(data.MovementAnimations);
        animationGraph.SetAttackAnimations(data.AttackAnimations);
        animationGraph.SetHitStunAnimations(data.HitStunAnimation);
        animationGraph.SetStaggerAnimation(data.StaggerAnimation);
    }


    private void OnActionStateChanged(ActionState actionState)
    {
        animationGraph.OnActionStateChanged(actionState);
        rootMotionReceiver?.SetRootMotion(Vector3.zero, false);
    }

    /// <summary>Forwards root motion to the receiver. Reports zero while the graph is blending between action states.</summary>
    private void OnRootMotionEvaluated(Vector3 velocity)
    {
        if (rootMotionReceiver == null)
            return;

        if (animationGraph.IsBlending)
            velocity = Vector3.zero;

        rootMotionReceiver.SetRootMotion(velocity, animationGraph.IsPaused);
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

    private void OnGuardStateChanged(GuardState guardState)
    {
        if (guardState == GuardState.Null)
            return;

        animationGraph.ChangeGuardStateInput(guardState);
    }

    private void OnStaggerStateChanged(StaggerState staggerState)
    {
        if (staggerState == StaggerState.Null)
            return;

        animationGraph.ChangeStaggerStateInput(staggerState);
    }

    private void OnHitConnected(HitSenderData hitBoxConnectedData)
    {
        animationGraph.Pause(hitBoxConnectedData.hitPauseTime);
    }

    #endregion


    private void Update()
    {
        animationGraph.Update();
    }
}
