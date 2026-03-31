using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(IActionState))]
[RequireComponent(typeof(Rigidbody))]
internal class StatHandler : MonoBehaviour, IStats
{
    //Dependecies
    protected ISceneObject sceneObject;
    protected IActionState actionState;

    [SerializeField] protected SceneObjectData baseSceneObjectData;

    protected Rigidbody rb;
    private float MassRatio => Mathf.Clamp(rb.mass, baseSceneObjectData.MinMass, baseSceneObjectData.MaxMass) / baseSceneObjectData.MaxMass;

    //Events
    public event Action<AnimationStatData> AnimationStatsChangedEvent;
    public event Action<MovementStatData> MovementStatsChangedEvent;
    public event Action<AttackStatData> AttackStatsChangedEvent;

    #region Initialize

    protected virtual void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("StatHandler: No SceneObject found.", gameObject);

        actionState = GetComponent<IActionState>();
        if (actionState == null)
            Debug.LogError("StatHandler: No ActionStateHandler found.", gameObject);

        if (baseSceneObjectData == null)
            Debug.LogError("StatHandler: No SceneObject Base Data found", gameObject);
        if (!baseSceneObjectData.IsValid())
            Debug.LogError("StatHandler: SceneObject Base Data is not valid", gameObject);

        rb = GetComponent<Rigidbody>();
        rb.mass = baseSceneObjectData.MinMass;

        RegisterToEvents();
    }

    protected virtual void RegisterToEvents()
    {
        //Debug
        baseSceneObjectData.MovementCollection.OnChangedEvent += MovementCollectionChanged;
    }

    protected virtual void Start()
    {
        UpdateSceneObjectStats();
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    protected virtual void UnregisterFromEvents()
    {
        //Debug
        baseSceneObjectData.MovementCollection.OnChangedEvent += MovementCollectionChanged;
    }

    private void MovementCollectionChanged()
    {
        UpdateMovementStats(baseSceneObjectData.MovementCollection);
    }

    #endregion


    #region Data

    [ContextMenu("Update Stats")]
    private void UpdateSceneObjectStats()
    {
        UpdateAnimationStats(baseSceneObjectData.MovementCollection);
        UpdateMovementStats(baseSceneObjectData.MovementCollection);        
    }

    protected void UpdateAnimationStats(MovementDataCollection movementData = null, AttackDataCollection attackData = null)
    {
        AnimationStatData statData = ParseAnimationData(movementData, attackData);
        if (statData != null)
            AnimationStatsChangedEvent?.Invoke(statData);
    }

    protected void UpdateMovementStats(MovementDataCollection moveData)
    {
        MovementStatData statData = ParseMovementData(moveData);
        if (statData != null)
            MovementStatsChangedEvent?.Invoke(statData);
    }

    protected void UpdateAttackStats(AttackDataCollection attackData)
    {
        AttackStatData statData = ParseAttackData(attackData);
        if (statData != null)
            AttackStatsChangedEvent?.Invoke(statData);
    }

    private AnimationStatData ParseAnimationData(MovementDataCollection movementData = null, AttackDataCollection attackData = null)
    {
        AnimationStatData statData = new AnimationStatData();

        statData.GroundedIdleAnimation = baseSceneObjectData.AnimationCollection.GroundIdleAnimation;
        statData.AirIdleAnimation = baseSceneObjectData.AnimationCollection.AirIdleAnimation;
        statData.HitStunAnimation = baseSceneObjectData.AnimationCollection.HitStunAnimation;

        if (movementData != null)
        {
            //NOTE: Order needs to match that of the ActionState.MovementState enum

            List<AnimationClip> movementAnimations = new List<AnimationClip>();

            if (movementData.MoveData != null)
                movementAnimations.Add(movementData.MoveData.Animation);
            else
                movementAnimations.Add(null);

            if (movementData.AirMoveData != null)
                movementAnimations.Add(movementData.AirMoveData.Animation);
            else
                movementAnimations.Add(null);

            if (movementData.ClimbMoveData != null)
                movementAnimations.Add(movementData.ClimbMoveData.Animation);
            else
                movementAnimations.Add(null);

            if (movementData.WallLeanData != null)
                movementAnimations.Add(movementData.WallLeanData.Animation);
            else
                movementAnimations.Add(null);

            if (movementData.VaultData != null)
                movementAnimations.Add(movementData.VaultData.Animation);
            else
                movementAnimations.Add(null);

            if (movementData.JumpData != null)
                movementAnimations.Add(movementData.JumpData.Animation);
            else
                movementAnimations.Add(null);

            if (movementData.AirJumpData != null)
                movementAnimations.Add(movementData.AirJumpData.Animation);
            else
                movementAnimations.Add(null);

            statData.MovementAnimations = movementAnimations.ToArray();
        }

        if (attackData != null)
        {
            List<AnimationClip> attackAnimations = new List<AnimationClip>();

            if (attackData.UpTiltData != null)
                attackAnimations.Add(attackData.UpTiltData.Animation);
            else
                attackAnimations.Add(null);

            if (attackData.ForwardTiltData != null)
                attackAnimations.Add(attackData.ForwardTiltData.Animation);
            else
                attackAnimations.Add(null);

            if (attackData.DownTiltData != null)
                attackAnimations.Add(attackData.DownTiltData.Animation);
            else
                attackAnimations.Add(null);

            if (attackData.UpAirData != null)
                attackAnimations.Add(attackData.UpAirData.Animation);
            else
                attackAnimations.Add(null);

            if (attackData.ForwardAirData != null)
                attackAnimations.Add(attackData.UpAirData.Animation);
            else
                attackAnimations.Add(null);

            if (attackData.DownAirData != null)
                attackAnimations.Add(attackData.DownAirData.Animation);
            else
                attackAnimations.Add(null);

            statData.AttackAnimations = attackAnimations.ToArray();
        }

        return statData;
    }   

    private MovementStatData ParseMovementData(MovementDataCollection data)
    {
        if (data == null) return null;

        MovementStatData statData = new MovementStatData();

        statData.MaxGroundedVelocity = Mathf.Lerp(baseSceneObjectData.GroundedMaxVelocityMax, baseSceneObjectData.GroundedMaxVelocityMin, MassRatio);
        statData.GroundedDecceleration = baseSceneObjectData.GroundedDecceleration;

        statData.MaxAerialXVelocity = Mathf.Lerp(baseSceneObjectData.AerialMaxXVelocityMax, baseSceneObjectData.AerialMaxXVelocityMin, MassRatio);
        statData.AerialXDecceleration = baseSceneObjectData.AerialXDecceleration;
        statData.MaxAerialYVelocity = Mathf.Lerp(baseSceneObjectData.AerialMaxYVelocityMax, baseSceneObjectData.AerialMaxYVelocityMin, MassRatio);
        statData.AerialUpYDecceleration = baseSceneObjectData.AerialYDecceleration + Mathf.Abs(Physics.gravity.y) * baseSceneObjectData.GravityMultiplier;
        statData.AerialDownYDecceleration = baseSceneObjectData.AerialYDecceleration - Mathf.Abs(Physics.gravity.y) * baseSceneObjectData.GravityMultiplier;
        statData.GravityMultiplier = baseSceneObjectData.GravityMultiplier;

        if (data.MoveData != null)
        {
            statData.GroundedAcceleration = Mathf.Lerp(data.MoveData.GroundedXMaxAcceleration, data.MoveData.GroundedXMinAcceleration, MassRatio);
            statData.GroundedMovementValid = data.MoveData.IsValid();
        }

        if (data.AirMoveData != null)
        {
            statData.AerialXAcceleration = Mathf.Lerp(data.AirMoveData.AerialXMaxAcceleration, data.AirMoveData.AerialXMinAcceleration, MassRatio);
            statData.AerialYAcceleration = Mathf.Lerp(data.AirMoveData.AerialYMaxAcceleration, data.AirMoveData.AerialYMinAcceleration, MassRatio);
            statData.AerialMovementValid = data.AirMoveData.IsValid();
        }

        if (data.ClimbMoveData != null)
        {
            statData.MaxClimbXVelocity = data.ClimbMoveData.ClimbXVelocity;
            statData.MaxClimbUpYVelocity = data.ClimbMoveData.ClimbUpYVelocity;
            statData.MaxClimbDownYVelocity = data.ClimbMoveData.ClimbDownYVelocity;
            statData.ClimbSlideDecceleration = Mathf.Lerp(data.ClimbMoveData.MaxClimbSlideDecceleration, data.ClimbMoveData.MinClimbSlideDecceleration, MassRatio);
            statData.ClimbMovementValid = data.ClimbMoveData.IsValid();
        }

        if (data.JumpData != null)
        {
            statData.InitialJumpVelocity = Mathf.Lerp(data.JumpData.MaxInitialVelocity, data.JumpData.MinInitialVelocity, MassRatio);
            statData.JumpAcceleration = Mathf.Lerp(data.JumpData.MaxJumpAcceleration, data.JumpData.MinJumpAcceleration, MassRatio);
            statData.GroundedJumpValid = data.JumpData.IsValid();
        }

        if (data.AirJumpData != null)
        {
            statData.InitialAirJumpVelocity = Mathf.Lerp(data.AirJumpData.MaxInitialVelocity, data.AirJumpData.MinInitialVelocity, MassRatio);
            statData.AirJumpAcceleration = Mathf.Lerp(data.AirJumpData.MaxJumpAcceleration, data.AirJumpData.MinJumpAcceleration, MassRatio);
            statData.AirJumpsAvailable = data.AirJumpData.AdditionalJumpsAvailable;
            statData.AerialJumpValid = data.AirJumpData.IsValid();
        }

        if (data.VaultData != null)
        {
            statData.VaultValid = data.VaultData.IsValid();
        }

        if (data.WallLeanData != null)
        {
            statData.WallLeanValid = data.WallLeanData.IsValid();
        }

        return statData;
    }

    private AttackStatData ParseAttackData(AttackDataCollection data)
    {
        if (data == null) return null;

        AttackStatData attackData = new AttackStatData();

        if (data.UpTiltData != null)
            attackData.UpTilt = new AttackStatData.AttackStats(AttackState.UpTilt, data.UpTiltData);

        if (data.ForwardTiltData != null)
            attackData.ForwardTilt = new AttackStatData.AttackStats(AttackState.ForwardTilt, data.ForwardTiltData);

        if (data.DownTiltData != null)
            attackData.DownTilt = new AttackStatData.AttackStats(AttackState.DownTilt, data.DownTiltData);

        if (data.UpAirData != null)
            attackData.UpAir = new AttackStatData.AttackStats(AttackState.UpAir, data.UpAirData);

        if (data.ForwardAirData != null)
            attackData.ForwardAir = new AttackStatData.AttackStats(AttackState.ForwardAir, data.ForwardAirData);

        if (data.DownAirData != null)
            attackData.DownAir = new AttackStatData.AttackStats(AttackState.DownAir, data.DownAirData);

        return attackData;
    }

    #endregion
}
