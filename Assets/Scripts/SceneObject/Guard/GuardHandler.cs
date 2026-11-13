using System;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(Rigidbody))]
public class GuardHandler : HitBoxHandler, IGuard, IHitInterceptor
{
    private ISceneObject sceneObject;
    private IActionState actionState;
    private ISOHurtBoxHandler hurtBoxHandler;
    private IGuardInput guardInput;
    private IInputBuffer inputBuffer;
    private IAttackCancel[] attackCancelSources;

    [SerializeField] private GuardData guardData;

    [Header("State")]
    [SerializeField] private GuardState curGuardState = GuardState.Null;
    public GuardState CurGuardState => curGuardState;
    private float stateTimer = 0f;

    [Header("Timers")]
    [SerializeField] private float guardTimer = 0f;
    [SerializeField] private float cooldownTimer = 0f;
    private bool needsRelease = false;

    [Header("Counter")]
    [Tooltip("Root of the counter blast. Holds the blast HitBox and any visuals. Scaled to the blast radius while active.")]
    [SerializeField] private Transform counterBlastRoot;
    private float counterWindowTimer = 0f;
    private float blastTimer = 0f;
    private bool isBlastActive = false;

    public float GuardWalkSpeed => guardData != null ? guardData.GuardWalkSpeed : 0f;
    public float AirDriftSpeed => guardData != null ? guardData.AirDriftSpeed : 0f;
    public float AirGravityScale => guardData != null ? guardData.AirGravityScale : 1f;
    public float AirMaxFallSpeed => guardData != null ? guardData.AirMaxFallSpeed : 0f;
    public float AirSlowdownRate => guardData != null ? guardData.AirSlowdownRate : 0f;
    public AnimationClip GuardAnimation => guardData != null && guardData.GuardAnimation != null ? guardData.GuardAnimation.Animation : null;
    public float GuardAnimationSpeed => guardData != null && guardData.GuardAnimation != null ? guardData.GuardAnimation.AnimationSpeed : 1f;
    public AnimationClip CounterAnimation => guardData != null && guardData.CounterAnimation != null ? guardData.CounterAnimation.Animation : null;
    public float CounterAnimationSpeed => guardData != null && guardData.CounterAnimation != null ? guardData.CounterAnimation.AnimationSpeed : 1f;

    public event Action<GuardState> GuardStateChangedEvent;

    private bool IsGrounded => actionState.CurGroundedState == GroundedState.Grounded;
    private bool IsGuardHeld => guardInput != null && guardInput.GuardHeld;
    private bool IsCounterWindowOpen => counterWindowTimer > 0f;
    private bool IsOnCooldown => cooldownTimer > 0f;
    private bool IsGuardUp => curGuardState == GuardState.Raise ||
                              curGuardState == GuardState.Hold ||
                              curGuardState == GuardState.Blockstun;

    // True while an attack or jump press is buffered.
    private bool IsActionRequested => inputBuffer != null &&
                                      (inputBuffer.Peek(BufferedInput.Attack) || inputBuffer.Peek(BufferedInput.Jump));

    // Idle, moving, or in the Recovery phase of hitstun.
    private bool IsAbleToGuard => actionState.CurActionState <= ActionState.Moving ||
                                  (actionState.CurActionState == ActionState.HitStun &&
                                   hurtBoxHandler != null &&
                                   hurtBoxHandler.CurHitStunState == HitStunState.Recovery);

    private bool CanRaise => guardData != null &&
                             IsGuardHeld &&
                             !needsRelease &&
                             !IsOnCooldown &&
                             !IsActionRequested &&
                             IsAbleToGuard;


    #region Initialize

    protected override void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        actionState = GetComponent<IActionState>();

        hurtBoxHandler = GetComponent<ISOHurtBoxHandler>();
        if (hurtBoxHandler == null)
            Debug.LogError($"GuardHandler requires an ISOHurtBoxHandler on {gameObject.name}.", gameObject);

        guardInput = GetComponent<IGuardInput>();
        if (guardInput == null)
            Debug.LogError($"GuardHandler requires an IGuardInput on {gameObject.name}.", gameObject);

        inputBuffer = GetComponent<IInputBuffer>();
        attackCancelSources = GetComponents<IAttackCancel>();

        if (guardData == null)
            Debug.LogError($"GuardHandler has no GuardData on {gameObject.name}.", gameObject);

        base.Awake();
    }

    protected override IHitBox[] CollectHitboxs()
    {
        if (counterBlastRoot == null)
        {
            Debug.LogError($"GuardHandler has no counter blast root on {gameObject.name}.", gameObject);
            return null;
        }

        return counterBlastRoot.GetComponentsInChildren<IHitBox>(true);
    }

    private void Start()
    {
        if (hitboxs != null)
        {
            foreach (IHitBox hitbox in hitboxs)
                hitbox.SetOwner(sceneObject.UniqueID);
        }

        SetBlastRadius(0f);
    }

    protected override void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;

        foreach (IAttackCancel cancelSource in attackCancelSources)
            cancelSource.PerformedAttackCancel += OnPerformedAttackCancel;
    }

    protected override void UnregisterFromEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;

        foreach (IAttackCancel cancelSource in attackCancelSources)
            cancelSource.PerformedAttackCancel -= OnPerformedAttackCancel;
    }

    #endregion


    #region Event Handlers

    private void OnActionStateChanged(ActionState state)
    {
        if (state != ActionState.Guarding && curGuardState != GuardState.Null)
            ChangeGuardState(GuardState.Null);
    }

    private void OnPerformedAttackCancel(BufferedInput input)
    {
        if (curGuardState != GuardState.Null)
            CancelGuard();
    }

    #endregion


    private void FixedUpdate()
    {
        if (guardData == null)
            return;

        float deltaTime = Time.fixedDeltaTime;

        counterWindowTimer = Mathf.Max(0f, counterWindowTimer - deltaTime);
        cooldownTimer = Mathf.Max(0f, cooldownTimer - deltaTime);

        UpdateRelease();
        UpdateGuardTimer(deltaTime);
        UpdateGuardState(deltaTime);
        UpdateBlast(deltaTime);
    }


    #region Guard State

    /// <summary>A press held while on cooldown is ignored until released.</summary>
    private void UpdateRelease()
    {
        if (!IsGuardHeld)
            needsRelease = false;
        else if (IsOnCooldown && curGuardState == GuardState.Null)
            needsRelease = true;
    }

    private void UpdateGuardTimer(float deltaTime)
    {
        if (!IsGuardUp)
            return;

        guardTimer += deltaTime;

        if (guardTimer >= guardData.GuardDuration)
            CancelGuard();
    }

    private void UpdateGuardState(float deltaTime)
    {
        switch (curGuardState)
        {
            case GuardState.Null:
                if (CanRaise)
                    BeginRaise();
                break;

            case GuardState.Raise:
                if (IsActionRequested)
                {
                    CancelGuard();
                    break;
                }

                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                    ChangeGuardState(GuardState.Hold);
                break;

            case GuardState.Hold:
                if (!IsGuardHeld && IsCounterWindowOpen)
                    BeginCounter();
                else if (!IsGuardHeld)
                    BeginDrop();
                else if (IsActionRequested)
                    CancelGuard();
                break;

            case GuardState.Blockstun:
                if (!IsGuardHeld && IsCounterWindowOpen)
                {
                    BeginCounter();
                    break;
                }

                stateTimer -= deltaTime;
                if (stateTimer > 0f)
                    break;

                if (!IsGuardHeld)
                    BeginDrop();
                else if (IsActionRequested)
                    CancelGuard();
                else
                    ChangeGuardState(GuardState.Hold);
                break;

            case GuardState.Counter:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                    CancelGuard();
                break;

            case GuardState.Drop:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                    CancelGuard();
                break;
        }
    }

    private void BeginRaise()
    {
        if (actionState.CurActionState == ActionState.HitStun && !hurtBoxHandler.TryEndRecovery())
            return;

        if (!actionState.TryChangeState(ActionState.Guarding))
            return;

        guardTimer = 0f;
        needsRelease = true;
        stateTimer = guardData.RaiseTime;
        ChangeGuardState(GuardState.Raise);
    }

    private void BeginDrop()
    {
        stateTimer = guardData.DropTime;
        ChangeGuardState(GuardState.Drop);
    }

    /// <summary>Clears the guard and returns the action state to idle.</summary>
    private void CancelGuard()
    {
        ChangeGuardState(GuardState.Null);

        if (actionState.CurActionState == ActionState.Guarding)
            actionState.ChangeState(ActionState.Idle);
    }

    /// <summary>Changes the guard state. Entering Null from any other state starts the cooldown.</summary>
    private void ChangeGuardState(GuardState newState)
    {
        if (newState == curGuardState)
            return;

        if (curGuardState == GuardState.Counter)
            EndBlast();

        if (newState == GuardState.Null)
        {
            cooldownTimer = guardData != null ? guardData.Cooldown : 0f;
            counterWindowTimer = 0f;
            guardTimer = 0f;
        }

        curGuardState = newState;
        sceneObject.Log("Guard State: " + curGuardState);
        GuardStateChangedEvent?.Invoke(curGuardState);
    }

    #endregion


    #region Hit Interception

    public bool TryIntercept(HitData hitData, out HitResponse response)
    {
        response = HitResponse.Hit;

        if (curGuardState == GuardState.Counter)
        {
            response = HitResponse.Ignored;
            return true;
        }

        if (curGuardState != GuardState.Hold && curGuardState != GuardState.Blockstun)
            return false;

        if (hitData.Unblockable)
            return false;

        ApplyPushback(hitData);

        stateTimer = guardData.BlockstunTime;
        counterWindowTimer = guardData.CounterWindow;
        ChangeGuardState(GuardState.Blockstun);

        response = HitResponse.Blocked(hitData.Damage * guardData.AttackerStaggerPerDamage);
        return true;
    }

    /// <summary>Pushes the object along the hit's launch angle. On the ground only the horizontal part applies.</summary>
    private void ApplyPushback(HitData hitData)
    {
        float speed = guardData.PushbackBase + guardData.PushbackPerDamage * hitData.Damage;
        float angleRad = hitData.LaunchAngle * Mathf.Deg2Rad;
        Vector3 pushback = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * speed;

        if (IsGrounded)
            rb.linearVelocity = new Vector3(pushback.x, rb.linearVelocity.y, 0f);
        else
            rb.linearVelocity = pushback;
    }

    #endregion


    #region Counter

    private void BeginCounter()
    {
        counterWindowTimer = 0f;
        stateTimer = guardData.CounterDuration;
        ChangeGuardState(GuardState.Counter);

        blastTimer = 0f;
        isBlastActive = true;
        SetBlastRadius(0f);
        ClearHitRecord();
        EnableHitBoxs();
    }

    private void UpdateBlast(float deltaTime)
    {
        if (!isBlastActive)
            return;

        blastTimer += deltaTime;

        float expandTime = Mathf.Max(guardData.BlastExpandTime, Mathf.Epsilon);
        float t = Mathf.Clamp01(blastTimer / expandTime);
        SetBlastRadius(Mathf.Lerp(0f, guardData.BlastRadius, t));

        if (t >= 1f)
            EndBlast();
    }

    private void EndBlast()
    {
        if (!isBlastActive)
            return;

        isBlastActive = false;
        DisableHitboxs();
        SetBlastRadius(0f);
    }

    /// <summary>Scales the blast root so a unit-diameter sphere spans the given radius.</summary>
    private void SetBlastRadius(float radius)
    {
        if (counterBlastRoot != null)
            counterBlastRoot.localScale = Vector3.one * Mathf.Max(radius * 2f, 0.001f);
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);

        Vector3 center = sceneObject.Bounds.center;
        float launchAngle = hitPoint.x >= center.x ? guardData.CounterLaunchAngle : 180f - guardData.CounterLaunchAngle;

        HitData baseHitData = new HitData(guardData.CounterBaseForce, guardData.CounterInfluence, launchAngle, guardData.CounterDamage, guardData.CounterHitStunTime, hitPoint);
        SceneObjectHitData hitData = new SceneObjectHitData(sceneObject.UniqueID, baseHitData);
        hitData.SourcePosition = center;
        hitData.Unblockable = true;

        DeclareHit(new AttackHitSenderData(guardData.CounterHitPauseTime, -1, launchAngle), hurtBox, hitData);
    }

    #endregion
}
