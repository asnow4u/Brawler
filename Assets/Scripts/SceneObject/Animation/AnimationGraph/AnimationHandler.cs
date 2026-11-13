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

    private ISceneObject sceneObject;
    private Transform model;
    private Quaternion modelRestRotation;
    private bool rotatesOnHitStun;
    private float hitStunAngle;
    private float recoveryStartAngle;
    private float recoveryElapsed;
    private bool isEasingUpright;

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

        sceneObject = GetComponent<ISceneObject>();
        model = animator.transform;
        modelRestRotation = model.localRotation;
        rotatesOnHitStun = sceneObject.ObjectType == SceneObjectType.Player || sceneObject.ObjectType == SceneObjectType.Enemy;

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
        hurtBoxHandler.OnHitEvent += OnHitReceived;
        hurtBoxHandler.LaunchRedirectedEvent += OnLaunchRedirected;
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
        
        
        hurtBoxHandler.OnHitEvent -= OnHitReceived;
        hurtBoxHandler.LaunchRedirectedEvent -= OnLaunchRedirected;
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
            if (animationGraph.IsBlending)
            {
                hasRootMotionSample = false;
            }
            else
            {
                if (hasRootMotionSample)
                    velocity = rootMotionBone.parent.TransformVector(sample - lastRootMotionSample) / deltaTime;

                lastRootMotionSample = sample;
                hasRootMotionSample = true;
            }
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
        if (rotatesOnHitStun)
            UpdateHitStunRotationState(hitStunState);

        if (hitStunState == HitStunState.Null)
            return;

        animationGraph.ChangeHitStunStateInput(hitStunState);
    }

    private void OnGuardStateChanged(GuardState guardState)
    {
        if (guardState == GuardState.Null)
            return;

        // NOTE: An airborne counter keeps playing the guard clip.
        if (guardState == GuardState.Counter && actionState.CurGroundedState == GroundedState.Airborn)
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


    #region Hit Stun Rotation

    /// <summary>Rotates the model about its Z axis so its back (model up) faces the launch direction.</summary>
    private void OnHitReceived(KnockBackHitData hitData)
    {
        if (!rotatesOnHitStun)
            return;

        isEasingUpright = false;
        SetHitStunAngle(CalculateHitStunAngle(hitData.KnockBackVelocity));
    }

    /// <summary>Rotates the model to the redirected launch direction. During recovery the ease continues from the new angle.</summary>
    private void OnLaunchRedirected(Vector3 launchVelocity)
    {
        if (!rotatesOnHitStun)
            return;

        float angle = CalculateHitStunAngle(launchVelocity);

        if (isEasingUpright)
            recoveryStartAngle = angle;

        SetHitStunAngle(angle);
    }

    /// <summary>Returns the model Z angle that points the model's up along the launch, in facing-local space.</summary>
    private float CalculateHitStunAngle(Vector3 launchVelocity)
    {
        return Mathf.Atan2(Mathf.Abs(launchVelocity.x), launchVelocity.y) * Mathf.Rad2Deg;
    }

    private void UpdateHitStunRotationState(HitStunState hitStunState)
    {
        if (hitStunState == HitStunState.Recovery)
        {
            recoveryStartAngle = hitStunAngle;
            recoveryElapsed = 0f;
            isEasingUpright = true;
        }
        else if (hitStunState == HitStunState.Null)
        {
            isEasingUpright = false;
            SetHitStunAngle(0f);
        }
    }

    /// <summary>Eases the model's hitstun tilt back to upright over the recovery duration.</summary>
    private void EaseUpright(float deltaTime)
    {
        recoveryElapsed += deltaTime;

        float duration = hurtBoxHandler.RecoveryDuration;
        float t = duration > 0f ? Mathf.Clamp01(recoveryElapsed / duration) : 1f;

        SetHitStunAngle(Mathf.SmoothStep(recoveryStartAngle, 0f, t));

        if (t >= 1f)
            isEasingUpright = false;
    }

    private void SetHitStunAngle(float angle)
    {
        hitStunAngle = angle;
        model.localRotation = Quaternion.Euler(0f, 0f, angle) * modelRestRotation;
    }

    #endregion


    private void Update()
    {
        float deltaTime = Time.deltaTime;

        animationGraph.Update();
        animationGraph.Evaluate(deltaTime);
        ForwardRootMotion(deltaTime);

        if (isEasingUpright)
            EaseUpright(deltaTime);
    }
}
