using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(Rigidbody))]
public class AttackHandler : MonoBehaviour, IAttack
{    
    //Dependencies
    private ISceneObject sceneObject;
    private IAttackInput attackInput;
    private IActionState actionState;   
    private IStats statHandler;
    private IAnimationEvent animationEventHandler;

    //Components
    private Rigidbody rb;
            
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

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        rb = GetComponent<Rigidbody>();

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        animationEventHandler.OnAnimationEventFiredEvent += OnAnimationEvent;

        attackInput.AttackPerformedEvent += PerformAttack;
    }    

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void UnregisterFromEvents()
    {
        actionState.GroundedStateChangedEvent -= OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        animationEventHandler.OnAnimationEventFiredEvent -= OnAnimationEvent;

        attackInput.AttackPerformedEvent -= PerformAttack;
    }

    #endregion

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


    #region Attack State

    private void SetCurrentAttackState(AttackState attackState)
    {       
        if (curAttackState == attackState)
            return;

        if (attackState == AttackState.Null || actionState.TryChangeState(ActionState.Attacking))
        {
            if (attackState == AttackState.Null && actionState.CurActionState == ActionState.Attacking)
                actionState.ChangeState(ActionState.Idle);

            curAttackState = attackState;
            sceneObject.Log("Attack State: " + curAttackState);
            AttackStateChangedEvent?.Invoke(curAttackState);
        }
    }

    #endregion


    #region Perform Attack

    private void PerformAttack(Vector2 direction)
    {
        if (curAttackState != AttackState.Null || curAttackData == null)
            return;
        
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
            
            //NOTE: Resets y velocity. This helps prevent an areal grounded attack if performed on first few frame of jump
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);
            
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

            //NOTE: Resets y velocity. This helps prevent an areal grounded attack if performed on first few frame of jump
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, 0);

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
