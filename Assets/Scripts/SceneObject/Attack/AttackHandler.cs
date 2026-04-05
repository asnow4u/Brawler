using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(Rigidbody))]
internal class AttackHandler : MonoBehaviour, IAttack
{    
    //Dependencies
    private ISceneObject sceneObject;
    private IAttackInput attackInput;
    private IActionState actionState;   
    private IStats statHandler;

    //Components
    private Rigidbody rb;
            
    private AttackState curAttackState => actionState.CurAttackState;
    
    private AttackStatData curAttackData;

    private Coroutine attackFrameCounterCoroutine;

    #region Initialize    

    private void Awake()
    {
        attackInput = GetComponent<IAttackInput>();
        if (attackInput == null)
            Debug.LogError($"No IAttackInput found on {gameObject.name}.", gameObject);

        sceneObject = GetComponent<ISceneObject>();
        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        rb = GetComponent<Rigidbody>();

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        statHandler.AttackStatsChangedEvent += OnAttackStatsChanged;

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


    #region Attack State

    private void SetCurrentAttackState(AttackState attackState)
    {       
        actionState.ChangeAttackState(attackState);

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

        if (clip != null)
            attackFrameCounterCoroutine = StartCoroutine(AttackFrameCounter(clip));
        
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


    private IEnumerator AttackFrameCounter(AnimationClip animation)
    {
        yield return new WaitForSeconds(animation.length);
        actionState.ChangeAttackState(AttackState.Null);
    }
}
