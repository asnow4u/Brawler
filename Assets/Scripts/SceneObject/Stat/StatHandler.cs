using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(Rigidbody))]
public class StatHandler : MonoBehaviour, IStats
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
        if (baseSceneObjectData == null)
            Debug.LogError("StatHandler: No SceneObject Base Data found", gameObject);
        if (!baseSceneObjectData.IsValid())
            Debug.LogError("StatHandler: SceneObject Base Data is not valid", gameObject);

        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("StatHandler: No ISceneObject component found", gameObject);

        actionState = GetComponent<IActionState>();
        rb = GetComponent<Rigidbody>();
        rb.mass = baseSceneObjectData.MinMass;

        RegisterToEvents();
    }

    protected virtual void RegisterToEvents()
    {
        //Debug
        baseSceneObjectData.OnChangedEvent += soDataChanged;
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
        baseSceneObjectData.OnChangedEvent -= soDataChanged;
    }

    private void soDataChanged()
    {
        UpdateSceneObjectStats();
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

    protected void UpdateAttackStats(AttackStatData attackStats)
    {
        if (attackStats != null)
            AttackStatsChangedEvent?.Invoke(attackStats);
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

            Dictionary<int, AnimationClip> movementAnimations = new Dictionary<int, AnimationClip>();

            if (movementData.MoveData != null)
                movementAnimations.Add(0, movementData.MoveData.Animation);
            else
                movementAnimations.Add(0, null);

            if (movementData.AirMoveData != null)
                movementAnimations.Add(1, movementData.AirMoveData.Animation);
            else
                movementAnimations.Add(1, null);

            if (movementData.ClimbMoveData != null)
            {
                movementAnimations.Add(2, movementData.ClimbMoveData.Animation);
                statData.ClimbIdleAnimation = movementData.ClimbMoveData.IdleAnimation;
            }
            else
                movementAnimations.Add(2, null);

            if (movementData.WallLeanData != null)
                movementAnimations.Add(3, movementData.WallLeanData.Animation);
            else
                movementAnimations.Add(3, null);

            if (movementData.VaultData != null)
                movementAnimations.Add(4, movementData.VaultData.Animation);
            else
                movementAnimations.Add(4, null);

            if (movementData.JumpData != null)
                movementAnimations.Add(5, movementData.JumpData.Animation);
            else
                movementAnimations.Add(5, null);

            if (movementData.AirJumpData != null)
                movementAnimations.Add(6, movementData.AirJumpData.Animation);
            else
                movementAnimations.Add(6, null);

            statData.MovementAnimations = movementAnimations;
        }

        if (attackData != null)
        {
            //NOTE: Order needs to match that of the ActionState.AttackState enum

            Dictionary<int, AnimationClip> attackAnimations = new Dictionary<int, AnimationClip>();

            if (attackData.UpTiltData != null)
                attackAnimations.Add(0, attackData.UpTiltData.Animation);
            else
                attackAnimations.Add(0, null);

            if (attackData.DownTiltData != null)
                attackAnimations.Add(1, attackData.DownTiltData.Animation);
            else
                attackAnimations.Add(1, null);

            if (attackData.ForwardTiltData != null)
                attackAnimations.Add(2, attackData.ForwardTiltData.Animation);
            else
                attackAnimations.Add(2, null);

            if (attackData.UpAirData != null)
                attackAnimations.Add(3, attackData.UpAirData.Animation);
            else
                attackAnimations.Add(3, null);

            if (attackData.DownAirData != null)
                attackAnimations.Add(4, attackData.DownAirData.Animation);
            else
                attackAnimations.Add(4, null);

            if (attackData.ForwardAirData != null)
                attackAnimations.Add(5, attackData.ForwardAirData.Animation);
            else
                attackAnimations.Add(5, null);

            statData.AttackAnimations = attackAnimations;
        }

        return statData;
    }   

    private MovementStatData ParseMovementData(MovementDataCollection data)
    {        
        MovementStatData statData = new MovementStatData();

        statData.MaxGroundedVelocity = Mathf.Lerp(baseSceneObjectData.GroundedMaxVelocityMax, baseSceneObjectData.GroundedMaxVelocityMin, MassRatio);
        statData.GroundedDecceleration = baseSceneObjectData.GroundedDecceleration;

        statData.MaxAerialXVelocity = Mathf.Lerp(baseSceneObjectData.AerialMaxXVelocityMax, baseSceneObjectData.AerialMaxXVelocityMin, MassRatio);
        statData.AerialXDecceleration = baseSceneObjectData.AerialXDecceleration;
        statData.MaxAerialYVelocity = Mathf.Lerp(baseSceneObjectData.AerialMaxYVelocityMax, baseSceneObjectData.AerialMaxYVelocityMin, MassRatio);
        statData.AerialUpYDecceleration = baseSceneObjectData.AerialYDecceleration + Mathf.Abs(Physics.gravity.y) * baseSceneObjectData.GravityMultiplier;
        statData.AerialDownYDecceleration = baseSceneObjectData.AerialYDecceleration - Mathf.Abs(Physics.gravity.y) * baseSceneObjectData.GravityMultiplier;
        statData.GravityMultiplier = baseSceneObjectData.GravityMultiplier;

        if (data == null)
            return statData;

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

    #endregion
}
