using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(AnimationHandler))]
[RequireComponent(typeof(Rigidbody))]
internal class AttackHandler : MonoBehaviour, IAttack
{    
    //Dependencies
    private ISceneObject sceneObject;
    private IAttackInput attackInput;
    private IActionState actionState;   
    private IStats statHandler;
    private IAnimation animationHandler;

    //Components
    private Rigidbody rb;
            
    private AttackState curAttackState => actionState.CurAttackState;    
    private AttackStatData curAttackData;

    #region Initialize    

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError($"No ISceneObject found on {gameObject.name}.", gameObject);

        attackInput = GetComponent<IAttackInput>();
        if (attackInput == null)
            Debug.LogError($"No IAttackInput found on {gameObject.name}.", gameObject);

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        animationHandler = GetComponent<IAnimation>();
        rb = GetComponent<Rigidbody>();

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;
        animationHandler.AnimationEndedEvent += OnAnimationEnded;

        attackInput.UpAttackPerformedEvent += PerformUpAttack;
        attackInput.RightAttackPerformedEvent += PerformRightAttack;
        attackInput.LeftAttackPerformedEvent += PerformLeftAttack;
        attackInput.DownAttackPerformedEvent += PerformDownAttack;
    }    

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void UnregisterFromEvents()
    {
        actionState.GroundedStateChangedEvent -= OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent -= OnAttackStatsChanged;
        animationHandler.AnimationEndedEvent += OnAnimationEnded;

        attackInput.UpAttackPerformedEvent -= PerformUpAttack;
        attackInput.RightAttackPerformedEvent -= PerformRightAttack;
        attackInput.LeftAttackPerformedEvent -= PerformLeftAttack;
        attackInput.DownAttackPerformedEvent -= PerformDownAttack;
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

    private void OnAnimationEnded(AnimationClip endedClip)
    {
        if (curAttackState == AttackState.Null)
            return;

        AnimationClip clip = null;
        switch (actionState.CurAttackState)
        {
            case AttackState.UpTilt:
                clip = curAttackData.UpTilt.Animation;
                break;

            case AttackState.UpAir:
                clip = curAttackData.UpAir.Animation;
                break;

            case AttackState.ForwardTilt:
                clip = curAttackData.ForwardTilt.Animation;
                break;

            case AttackState.ForwardAir:
                clip = curAttackData.ForwardAir.Animation;
                break;

            case AttackState.DownTilt:
                clip = curAttackData.DownTilt.Animation;
                break;

            case AttackState.DownAir:
                clip = curAttackData.DownAir.Animation;
                break;
        }

        if (clip != null && clip == endedClip)
            SetCurrentAttackState(AttackState.Null);
    }


    #region Attack State

    private void SetCurrentAttackState(AttackState attackState)
    {       
        actionState.ChangeAttackState(attackState);
    }

    #endregion


    #region Perform Attack

    public void PerformUpAttack()
    {
        if (curAttackState != AttackState.Null || curAttackData == null)
            return;

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

    public void PerformDownAttack()
    {
        if (curAttackState != AttackState.Null || curAttackData == null)
            return;
        
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

    public void PerformRightAttack()
    {
        if (curAttackState != AttackState.Null || curAttackData == null)
            return;

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

    public void PerformLeftAttack()
    {
        if (curAttackState != AttackState.Null || curAttackData == null)
            return;

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
