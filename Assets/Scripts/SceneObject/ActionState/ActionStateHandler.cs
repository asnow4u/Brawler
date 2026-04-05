using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
public class ActionStateHandler : MonoBehaviour, IActionState
{
    private ISceneObject sceneObject;

    [SerializeField] private GroundedState curGroundedState;
    public GroundedState CurGroundedState => curGroundedState;

    [SerializeField] private ClimbState curClimbState;
    public ClimbState CurClimbState => curClimbState;

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

    public event Action<GroundedState> GroundedStateChangedEvent;
    public event Action<ClimbState> ClimbStateChangedEvent;

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
            Debug.LogError("ActionStateHandler requires a component that implements ISceneObject.");
    }

    private void Start()
    {
        ChangeState(ActionState.Idle);
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
        CheckClimbingAvailability();
    }

    private void UpdateGroundedState()
    {
        GroundedState groundedState = curGroundedState;

        //Switch to climbing
        bool groundCollision = sceneObject.TryDetectCollision(Direction.Down, 0.01f, LayerMask.GetMask("Environment"), out _);
        groundedState = groundCollision ? GroundedState.Grounded : GroundedState.Airborn;

        if (groundedState != curGroundedState)
        {
            curGroundedState = groundedState;
            sceneObject.Log("Grounded State: " + curGroundedState);
            GroundedStateChangedEvent?.Invoke(curGroundedState);
            
            UpdateIdleState();
        }
    }

    private void CheckClimbingAvailability()
    {
        if (!sceneObject.ClimbableSurfaceAvailable())
            ChangeClimbState(ClimbState.Unavailable);
        
        else if (curClimbState == ClimbState.Unavailable)
            ChangeClimbState(ClimbState.Available);
    }

    #endregion


    #region States

    private void ChangeState(ActionState newState)
    {
        if (newState != curActionState)
        {
            curActionState = newState;

            sceneObject.Log("ActionState State: " + curActionState);
            ActionStateChangedEvent?.Invoke(curActionState);

            //Cancel Climbing in some states
            if (curClimbState == ClimbState.Climbing && 
               (curActionState == ActionState.Attacking || curActionState == ActionState.HitStun))
                ChangeClimbState(ClimbState.Available);
        }
    }

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

    private void UpdateIdleState()
    {
        if (curClimbState == ClimbState.Climbing)
            curIdleState = IdleState.ClimbIdle;
        else if (curGroundedState == GroundedState.Grounded)
            curIdleState = IdleState.GroundIdle;
        else
            curIdleState = IdleState.AirIdle;

        sceneObject.Log("Idle State: " + curIdleState);
        IdleStateChangedEvent?.Invoke(curIdleState);
    }

    public void ChangeClimbState(ClimbState climbState)
    {
        if (climbState != curClimbState)
        {
            curClimbState = climbState;
            sceneObject.Log("Climb State: " + curClimbState);
            ClimbStateChangedEvent?.Invoke(curClimbState);

            UpdateIdleState();
        }
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
        if (curAttackState == attackState)
            return;

        if (attackState == AttackState.Null || TryChangeState(ActionState.Attacking))
        {
            if (attackState == AttackState.Null)
                ChangeState(ActionState.Idle);

            curAttackState = attackState;
            sceneObject.Log("Attack State: " + curAttackState);
            AttackStateChangedEvent?.Invoke(curAttackState);
        }
    }

    public void ChangeHitStunState(HitStunState hitStunState)
    {
        if (curHitStunState == hitStunState)
            return;

        if (hitStunState == HitStunState.Null || TryChangeState(ActionState.HitStun))
        {
            if (hitStunState == HitStunState.Null)
                ChangeState(ActionState.Idle);

            curHitStunState = hitStunState;
            sceneObject.Log("HitStun State: " + curHitStunState);
            HitStunStateChangedEvent?.Invoke(curHitStunState);
        }
    }

    #endregion    
}