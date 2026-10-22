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

    [Header("Stagger Bar")]
    [SerializeField] private float staggerBar = 0f;
    public float StaggerBar => staggerBar;
    private bool isBroken = false;

    [Header("Counter")]
    [Tooltip("Root of the counter blast. Holds the blast HitBox and any visuals. Scaled to the blast radius while active.")]
    [SerializeField] private Transform counterBlastRoot;
    private float counterWindowTimer = 0f;
    private float blastTimer = 0f;
    private bool isBlastActive = false;

    public float GuardWalkSpeed => guardData != null ? guardData.GuardWalkSpeed : 0f;
    public AnimationClip GuardAnimation => guardData != null && guardData.GuardAnimation != null ? guardData.GuardAnimation.Animation : null;
    public float GuardAnimationSpeed => guardData != null && guardData.GuardAnimation != null ? guardData.GuardAnimation.AnimationSpeed : 1f;
    public AnimationClip CounterAnimation => guardData != null && guardData.CounterAnimation != null ? guardData.CounterAnimation.Animation : null;
    public float CounterAnimationSpeed => guardData != null && guardData.CounterAnimation != null ? guardData.CounterAnimation.AnimationSpeed : 1f;

    public event Action<GuardState> GuardStateChangedEvent;

    private bool IsGrounded => actionState.CurGroundedState == GroundedState.Grounded;
    private bool IsGuardHeld => guardInput != null && guardInput.GuardHeld;
    private bool IsCounterWindowOpen => counterWindowTimer > 0f;

    // True while an attack or jump press is buffered.
    private bool IsActionRequested => inputBuffer != null &&
                                      (inputBuffer.Peek(BufferedInput.Attack) || inputBuffer.Peek(BufferedInput.Jump));

    private bool CanRaise => guardData != null &&
                             !isBroken &&
                             IsGuardHeld &&
                             IsGrounded &&
                             !IsActionRequested &&
                             actionState.CurActionState <= ActionState.Moving;


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

        if (hurtBoxHandler != null)
            hurtBoxHandler.StaggerStateChangedEvent += OnStaggerStateChanged;

        foreach (IAttackCancel cancelSource in attackCancelSources)
            cancelSource.PerformedAttackCancel += OnPerformedAttackCancel;
    }

    protected override void UnregisterFromEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;

        if (hurtBoxHandler != null)
            hurtBoxHandler.StaggerStateChangedEvent -= OnStaggerStateChanged;

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

    private void OnStaggerStateChanged(StaggerState state)
    {
        if (state == StaggerState.Null && isBroken)
        {
            isBroken = false;
            staggerBar = 0f;
        }
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

        UpdateStaggerBar(deltaTime);
        UpdateGuardState(deltaTime);
        UpdateBlast(deltaTime);
    }


    #region Guard State

    private void UpdateGuardState(float deltaTime)
    {
        switch (curGuardState)
        {
            case GuardState.Null:
                if (CanRaise)
                    BeginRaise();
                break;

            case GuardState.Raise:
                if (!IsGrounded)
                {
                    CancelGuard();
                    break;
                }

                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                    ChangeGuardState(GuardState.Hold);
                break;

            case GuardState.Hold:
                if (!IsGrounded)
                    CancelGuard();
                else if (!IsGuardHeld && IsCounterWindowOpen)
                    BeginCounter();
                else if (!IsGuardHeld || IsActionRequested)
                    BeginDrop();
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

                if (IsGuardHeld && !IsActionRequested)
                    ChangeGuardState(GuardState.Hold);
                else
                    BeginDrop();
                break;

            case GuardState.Counter:
                stateTimer -= deltaTime;
                if (stateTimer <= 0f)
                    BeginDrop();
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
        if (!actionState.TryChangeState(ActionState.Guarding))
            return;

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

    private void ChangeGuardState(GuardState newState)
    {
        if (newState == curGuardState)
            return;

        if (curGuardState == GuardState.Counter)
            EndBlast();

        curGuardState = newState;
        sceneObject.Log("Guard State: " + curGuardState);
        GuardStateChangedEvent?.Invoke(curGuardState);
    }

    #endregion


    #region Stagger Bar

    private void UpdateStaggerBar(float deltaTime)
    {
        if (isBroken)
            staggerBar = Mathf.Max(0f, staggerBar - guardData.BreakDrainRate * deltaTime);
        else if (curGuardState == GuardState.Null)
            staggerBar = Mathf.Max(0f, staggerBar - guardData.BarDecayRate * deltaTime);
        else if (curGuardState == GuardState.Hold || curGuardState == GuardState.Blockstun)
        {
            staggerBar += guardData.HoldFillRate * deltaTime;

            if (staggerBar >= guardData.StaggerBarMax)
                BreakGuard();
        }
    }

    private void BreakGuard()
    {
        staggerBar = guardData.StaggerBarMax;
        isBroken = true;
        counterWindowTimer = 0f;

        float breakDuration = guardData.BreakDrainRate > 0f ? guardData.StaggerBarMax / guardData.BreakDrainRate : 0f;
        hurtBoxHandler.Stagger(breakDuration);
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

        if (hitData.Unblockable || !IsFacingSource(hitData.SourcePosition))
            return false;

        staggerBar += hitData.Damage;

        if (staggerBar >= guardData.StaggerBarMax)
        {
            BreakGuard();
            response = HitResponse.Blocked(0f);
            return true;
        }

        ApplyPushback(hitData.Damage);

        stateTimer = guardData.BlockstunTime;
        counterWindowTimer = guardData.CounterWindow;
        ChangeGuardState(GuardState.Blockstun);

        response = HitResponse.Blocked(hitData.Damage * guardData.AttackerStaggerPerDamage);
        return true;
    }

    private bool IsFacingSource(Vector3 sourcePosition)
    {
        float sourceOffsetX = sourcePosition.x - sceneObject.Bounds.center.x;
        return sceneObject.IsFacingRightDirection ? sourceOffsetX >= 0f : sourceOffsetX <= 0f;
    }

    private void ApplyPushback(float damage)
    {
        float direction = sceneObject.IsFacingRightDirection ? -1f : 1f;
        float speed = guardData.PushbackBase + guardData.PushbackPerDamage * damage;

        Vector3 velocity = rb.linearVelocity;
        velocity.x = direction * speed;
        rb.linearVelocity = velocity;
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

        HitData baseHitData = new HitData(guardData.CounterBaseForce, guardData.CounterInfluence, launchAngle, guardData.CounterDamage, guardData.CounterHitStunTime, hitPoint, 0);
        SceneObjectHitData hitData = new SceneObjectHitData(sceneObject.UniqueID, baseHitData);
        hitData.SourcePosition = center;
        hitData.Unblockable = true;

        DeclareHit(new AttackHitSenderData(guardData.CounterHitPauseTime, -1, launchAngle), hurtBox, hitData);
    }

    #endregion
}
