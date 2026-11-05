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

    private AnimationGraph animationGraph;

    [Tooltip("Bone whose animated translation is used as root motion. It is held at its rest position after each evaluation. Leave empty for no root motion.")]
    [SerializeField] private Transform rootMotionBone;
    private Vector3 rootMotionBoneRest;
    private Vector3 lastRootMotionSample;
    private bool hasRootMotionSample = false;

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
        animator.applyRootMotion = false;

        if (rootMotionBone != null)
            rootMotionBoneRest = rootMotionBone.localPosition;

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
        hasRootMotionSample = false;
        rootMotionReceiver?.SetRootMotion(Vector3.zero, false);
    }

    /// <summary>Converts the root motion bone's change since the last evaluation into a world velocity, forwards it, then returns the bone to its rest position. Reports zero while blending between action states.</summary>
    private void ForwardRootMotion(float deltaTime)
    {
        if (rootMotionBone == null)
            return;

        Vector3 sample = rootMotionBone.localPosition;
        Vector3 velocity = Vector3.zero;

        if (deltaTime > 0f)
        {
            if (hasRootMotionSample && !animationGraph.IsBlending)
                velocity = rootMotionBone.parent.TransformVector(sample - lastRootMotionSample) / deltaTime;

            lastRootMotionSample = sample;
            hasRootMotionSample = true;
        }

        rootMotionBone.localPosition = rootMotionBoneRest;

        rootMotionReceiver?.SetRootMotion(velocity, animationGraph.IsPaused);
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
        float deltaTime = Time.deltaTime;

        animationGraph.Update();
        animationGraph.Evaluate(deltaTime);
        ForwardRootMotion(deltaTime);
    }
}
