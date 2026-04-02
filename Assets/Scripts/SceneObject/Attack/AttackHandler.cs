using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(IActionState))]
[RequireComponent(typeof(IStats))]
[RequireComponent(typeof(Rigidbody))]
internal class AttackHandler : MonoBehaviour, IAttack
{    
    //Dependencies
    private ISceneObject sceneObject;
    private IAttackInput attackInput;
    private IActionState actionState;   
    private IStats stats;

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
        if (actionState == null)
            Debug.LogError($"No IActionState found on {gameObject.name}.", gameObject);

        stats = GetComponent<IStats>();
        if (stats == null)
            Debug.LogError($"No IEquipment found on {gameObject.name}.", gameObject);

        rb = GetComponent<Rigidbody>();
        if (rb == null)
            Debug.LogError($"No Rigidbody found on {gameObject.name}.", gameObject);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        stats.AttackStatsChangedEvent += OnAttackStatsChanged;

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
        stats.AttackStatsChangedEvent -= OnAttackStatsChanged;

        attackInput.UpAttackPerformedEvent -= PerformUpAttack;
        attackInput.RightAttackPerformedEvent -= PerformRightAttack;
        attackInput.LeftAttackPerformedEvent -= PerformLeftAttack;
        attackInput.DownAttackPerformedEvent -= PerformDownAttack;
    }

    #endregion

    /// <summary>
    /// Handle grounded state changes while performing an aerial attack
    /// </summary>
    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        if (groundedState == GroundedState.Grounded && curAttackState != AttackState.Null)
        {
            SetCurrentAttackState(AttackState.Null);            
        }
    }

    /// <summary>
    /// Handle weapon equipping
    /// </summary>
    private void OnAttackStatsChanged(AttackStatData data)
    {        
        curAttackData = data;
    }


    #region Attack State

    /// <summary>
    /// Attempt to set the current attack state <br></br>
    /// This will initiate the animation of the attackType
    /// </summary>
    private void SetCurrentAttackState(AttackState attackState)
    {       
        actionState.ChangeAttackState(attackState);
    }

    #endregion


    #region Perform Attack

    /// <summary>
    /// Try to perform a grounded / Air Up attack
    /// </summary>
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

    /// <summary>
    /// Try to perform a grounded / Air Down attack
    /// </summary>
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

    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
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

    /// <summary>
    /// Try to perform a grounded / Air Forward attack <\br>
    /// Turn around if facing the wrong direction
    /// </summary>
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
