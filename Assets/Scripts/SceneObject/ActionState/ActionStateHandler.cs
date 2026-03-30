using System;
using System.Collections;
using UnityEngine;

internal class ActionStateHandler : MonoBehaviour, IActionState
{
    private ISceneObject sceneObject;

    [SerializeField] private ActionState curActionState;
    public ActionState CurActionState => curActionState;

    [SerializeField] private IdleState curIdleState;
    public IdleState CurIdleState => curIdleState;

    [SerializeField] private MovementState curMovementState;
    public MovementState CurMovementState => curMovementState;

    [SerializeField] private AttackState curAttackState;
    public AttackState CurAttackState => curAttackState;

    [SerializeField] private HitStunState curHitStunState;
    public HitStunState CurHitStunState => curHitStunState;

    [SerializeField] private float hitStunFreezeTimer = 0.033f;
    private Coroutine hitStunTimerCoroutine = null;

    public event Action<ActionState> ActionStateChangedEvent;
    public event Action<IdleState> IdleStateChangedEvent;
    public event Action<MovementState> MovementStateChangedEvent;
    public event Action<AttackState> AttackStateChangedEvent;
    public event Action<HitStunState> HitStunStateChangedEvent;

    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("No ISceneObject found on " + gameObject.name);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        sceneObject.GroundedStateChangedEvent += OnGroundedStateChanged;
        sceneObject.ClimbStateChangedEvent += OnClimbStateChanged;
    }

    private void Start()
    {
        ChangeState(ActionState.Idle);
    }

    private void OnDestroy()
    {
        UnregisterToEvents();
    }

    public void UnregisterToEvents()
    {
        sceneObject.GroundedStateChangedEvent -= OnGroundedStateChanged;
        sceneObject.ClimbStateChangedEvent -= OnClimbStateChanged;
    }


    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        if (groundedState == GroundedState.Grounded)
        {
            //NOTE: Attacking state is handled in AttackStateHandler
            if (curActionState == ActionState.Attacking) return;

            //Prevent changing to idle when vaulting (Switching to idle will cancel the vault animation)
            if (curActionState == ActionState.Moving && curMovementState == MovementState.Vault) return;

            ChangeState(ActionState.Idle);
        }

        UpdateIdleState();
    }

    private void OnClimbStateChanged(ClimbState prevClimbState, ClimbState climbState)
    {
        UpdateIdleState();
    }

    private void UpdateIdleState()
    {
        if (sceneObject.CurClimbState == ClimbState.Climbing)
            curIdleState = IdleState.ClimbIdle;
        else if (sceneObject.CurGroundedState == GroundedState.Grounded)
            curIdleState = IdleState.GroundIdle;
        else
            curIdleState = IdleState.AirIdle;

        sceneObject.Log("Idle State: " + curIdleState);
        IdleStateChangedEvent?.Invoke(curIdleState);
    }


    #endregion


    #region States

    /// <summary>
    /// Change the action state without consideration for priority
    /// </summary>
    /// <param name="newState"></param>
    private void ChangeState(ActionState newState)
    {
        if (newState != curActionState)
        {
            curActionState = newState;

            sceneObject.Log("ActionState State: " + curActionState);
            ActionStateChangedEvent?.Invoke(curActionState);
        }
    }


    /// <summary>
    /// Atempt to change action state based on if newState takes more priority
    /// </summary>
    /// <param name="newState"></param>
    /// <returns></returns>
    private bool TryChangeState(ActionState newState)
    {
        if (newState > curActionState)
        {
            ChangeState(newState);
            return true;
        }

        else if (newState == curActionState)
            return true;

        return false;
    }


    public void ChangeMovementState(MovementState movementState)
    {
        if (curMovementState == movementState) 
            return;

        if (TryChangeState(ActionState.Moving))
        {
            curMovementState = movementState;             
            sceneObject.Log("Movement State: " + curMovementState);
            MovementStateChangedEvent?.Invoke(curMovementState);

            if (curMovementState == MovementState.Null)
                ChangeState(ActionState.Idle);
        }
    }

    public void ChangeAttackState(AttackState attackState)
    {
        if (curAttackState != AttackState.Null)
            return;

        if (TryChangeState(ActionState.Attacking))
        {
            curAttackState = attackState;
            sceneObject.Log("Attack State: " + curAttackState);
            AttackStateChangedEvent?.Invoke(curAttackState);
        }
    }

    private void ChangeHitStunState(HitStunState hitStunState)
    {
        curHitStunState = hitStunState;
        sceneObject.Log("HitStun State: " + curHitStunState);
        HitStunStateChangedEvent?.Invoke(curHitStunState);
    }

    #endregion

    #region Hitstun

    public void SetHitStun(float timer)
    {
        if (hitStunTimerCoroutine != null)
            StopCoroutine(hitStunTimerCoroutine);

        hitStunTimerCoroutine = StartCoroutine(HitStunTimer(timer));
    }

    private IEnumerator HitStunTimer(float timer)
    {
        ChangeHitStunState(HitStunState.Freeze);
        yield return new WaitForSeconds(hitStunFreezeTimer);

        ChangeHitStunState(HitStunState.Launch);
        yield return new WaitForSeconds(timer);

        ChangeHitStunState(HitStunState.Null);
    }

    #endregion
}