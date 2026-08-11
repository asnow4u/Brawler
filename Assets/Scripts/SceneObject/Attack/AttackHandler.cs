using System;
using System.Collections;
using UnityEngine;
using static AttackStatData;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(AttackHitBoxHandler))]
[RequireComponent(typeof(HurtBoxHandler))]
[RequireComponent(typeof(Rigidbody))]
public class AttackHandler : MonoBehaviour, IAttack
{
    private ISceneObject sceneObject;
    private IAttackInput attackInput;
    private IActionState actionState;   
    private IStats statHandler;
    private IInputBuffer inputBuffer;
    private IAttackHitBoxHandler attackHitBoxHandler;
    private ISOHurtBoxHandler hurtBoxHandler;
    private IAnimationEvent animationEventHandler;
    
    private IMovementAction movementHandler;
    private IAttackCancel[] attackCancelSources;
    private Rigidbody rb;

    [Header("State")]
    [SerializeField] private AttackState curAttackState;
    public AttackState CurAttackState => curAttackState;

    private AttackStatData curAttackData;

    public event Action<AttackState> AttackStateChangedEvent;

    #region Initialize    

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError($"No ISceneObject found on {gameObject.name}.", gameObject);

        attackInput = GetComponent<IAttackInput>();
        if (attackInput == null)
            Debug.LogError($"No IAttackInput found on {gameObject.name}.", gameObject);

        animationEventHandler = GetComponentInChildren<IAnimationEvent>();
        if (animationEventHandler == null)
            Debug.LogError($"No IAnimationEvent found on {gameObject.name}.", gameObject);

        inputBuffer = GetComponent<IInputBuffer>();
        if (inputBuffer == null)
            Debug.LogError($"No IInputBuffer found on {gameObject.name}.", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        attackHitBoxHandler = GetComponent<IAttackHitBoxHandler>();
        hurtBoxHandler = GetComponent<ISOHurtBoxHandler>();

        movementHandler = GetComponent<IMovementAction>();
        attackCancelSources = GetComponents<IAttackCancel>();
        rb = GetComponent<Rigidbody>();


        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        hurtBoxHandler.OnHitEvent += OnHitByAttack;
        animationEventHandler.OnAnimationEventFiredEvent += OnAnimationEvent;

        foreach (IAttackCancel cancelSource in attackCancelSources)
            cancelSource.PerformedAttackCancel += OnPerformedAttackCancel;
    }    

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void UnregisterFromEvents()
    {
        actionState.GroundedStateChangedEvent -= OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        hurtBoxHandler.OnHitEvent -= OnHitByAttack;
        animationEventHandler.OnAnimationEventFiredEvent -= OnAnimationEvent;

        foreach (IAttackCancel cancelSource in attackCancelSources)
            cancelSource.PerformedAttackCancel -= OnPerformedAttackCancel;
    }

    #endregion    


    #region Event Handlers

    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        if (groundedState == GroundedState.Grounded && curAttackState != AttackState.Null)
        {
            SetCurrentAttackState(AttackState.Null);
        }
    }

    private void OnAttackStatsChanged(AttackStatData data)
    {        
        curAttackData = data;
    }

    private void OnAnimationEvent(AnimationEventState state)
    {
        if (curAttackState == AttackState.Null)
            return;

        if (state == AnimationEventState.AttackEnded)
            SetCurrentAttackState(AttackState.Null);
    }

    private void OnHitByAttack(KnockBackHitData hitData)
    {
        inputBuffer?.Clear(BufferedInput.Attack);
    }    

    private void OnPerformedAttackCancel(BufferedInput input)
    {
        SetCurrentAttackState(AttackState.Null);
    }

    #endregion


    private void FixedUpdate()
    {
        TryBufferedAttack();
    }


    #region Attack State

    private void SetCurrentAttackState(AttackState attackState)
    {       
        if (curAttackState == attackState)
            return;

        if (attackState == AttackState.Null || actionState.TryChangeState(ActionState.Attacking))
        {
            if (attackState == AttackState.Null && actionState.CurActionState == ActionState.Attacking)
            {
                bool movementOngoing = movementHandler != null && 
                                       movementHandler.CurMovementState != MovementState.Null;

                actionState.ChangeState(movementOngoing ? ActionState.Moving : ActionState.Idle);
            }

            curAttackState = attackState;
            sceneObject.Log("Attack State: " + curAttackState);
            
            UpdateHitBoxHandler();
            AttackStateChangedEvent?.Invoke(curAttackState);
        }
    }

    private void UpdateHitBoxHandler()
    {
        
        if (curAttackData == null)
        {
            attackHitBoxHandler.SetAttackHitData(null, null);
            return;
        }

        AttackStats attackStats = null;
        switch (curAttackState)
        {
            case AttackState.UpTilt:
                attackStats = curAttackData.UpTilt;
                break;

            case AttackState.ForwardTilt:
                attackStats = curAttackData.ForwardTilt;
                break;

            case AttackState.DownTilt:
                attackStats = curAttackData.DownTilt;
                break;

            case AttackState.UpAir:
                attackStats = curAttackData.UpAir;
                break;

            case AttackState.ForwardAir:
                attackStats = curAttackData.ForwardAir;
                break;

            case AttackState.DownAir:
                attackStats = curAttackData.DownAir;
                break;
        }

        if (attackStats == null)
        {
            attackHitBoxHandler.SetAttackHitData(null, null);
            return;
        }

        attackHitBoxHandler.SetAttackHitData(
            new HitData( attackStats.BaseForce, attackStats.Influence, attackStats.LaunchAngle, attackStats.Damage, attackStats.HitStunTime, Vector3.zero, attackStats.Type),
            new AttackHitSenderData(attackStats.HitPauseTime, (int)curAttackState, attackStats.LaunchAngle)
        );

        attackHitBoxHandler.SetWeaponHitBoxs(curAttackData.WeaponRootGameObject);
    }

    #endregion


    #region Perform Attack

    private void TryBufferedAttack()
    {
        if (inputBuffer == null ||
            curAttackData == null ||
            curAttackState != AttackState.Null ||
            actionState.CurActionState > ActionState.Moving)
            return;

        // NOTE: Cant attack while in jump squat
        if (movementHandler != null && movementHandler.IsInJumpSquat)
            return;

        if (inputBuffer.TryConsume(BufferedInput.Attack, out InputRecord record))
            ExecuteAttack(record.Direction);
    }

    private void ExecuteAttack(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            if (direction.x > 0)
                PerformRightAttack();
            else
                PerformLeftAttack();
        }
        else
        {
            if (direction.y > 0)
                PerformUpAttack();
            else
                PerformDownAttack();
        }
    }

    private void PerformUpAttack()
    {
        if (actionState.CurGroundedState == GroundedState.Grounded)
        {
            if (curAttackData.UpTilt == null) return;
            SetCurrentAttackState(AttackState.UpTilt);                            
        }
        else
        {
            if (curAttackData.UpAir == null) return;
            SetCurrentAttackState(AttackState.UpAir);
        }
    }

    private void PerformDownAttack()
    {
        if (actionState.CurGroundedState == GroundedState.Grounded)
        {
            if (curAttackData.DownTilt == null) return;
            SetCurrentAttackState(AttackState.DownTilt);
        }
        else
        {
            if (curAttackData.DownAir == null) return;
            SetCurrentAttackState(AttackState.DownAir);
        }
    }

    private void PerformRightAttack()
    {
        if (actionState.CurGroundedState == GroundedState.Grounded)
        {                
            if (curAttackData.ForwardTilt == null) return;

            SetCurrentAttackState(AttackState.ForwardTilt);                               
            
            if (!sceneObject.IsFacingRightDirection)
                sceneObject.TurnAround();
        }
        else
        {
            if (curAttackData.ForwardAir == null) return;

            SetCurrentAttackState(AttackState.ForwardAir);

            if (!sceneObject.IsFacingRightDirection)
                sceneObject.TurnAround();
        }
    }

    private void PerformLeftAttack()
    {
        if (actionState.CurGroundedState == GroundedState.Grounded)
        {
            if (curAttackData.ForwardTilt == null) return;

            SetCurrentAttackState(AttackState.ForwardTilt);

            if (sceneObject.IsFacingRightDirection)
                    sceneObject.TurnAround();
        }
        else
        {
            if (curAttackData.ForwardAir == null) return;

            SetCurrentAttackState(AttackState.ForwardAir);

            if (sceneObject.IsFacingRightDirection)
                sceneObject.TurnAround();
        }
    }

    #endregion
}
