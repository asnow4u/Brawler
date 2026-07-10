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
        rb.mass = baseSceneObjectData.Mass;

        RegisterToEvents();
    }

    protected virtual void RegisterToEvents()
    {
        SubscribeToRuntimeDataChanges();        
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
        UnsubscribeFromRuntimeDataChanges();
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

        AnimationData groundIdleAnimation = baseSceneObjectData.AnimationCollection.GroundIdleAnimation;
        statData.GroundedIdleAnimation = new AnimationStatData.AnimationData(groundIdleAnimation.Animation, groundIdleAnimation.AnimationSpeed);

        AnimationData airIdleAnimation = baseSceneObjectData.AnimationCollection.AirIdleAnimation;
        statData.AirIdleAnimation = new AnimationStatData.AnimationData(airIdleAnimation.Animation, airIdleAnimation.AnimationSpeed);

        AnimationData hitStunAnimation = baseSceneObjectData.AnimationCollection.HitStunAnimation;
        statData.HitStunAnimation = new AnimationStatData.AnimationData(hitStunAnimation.Animation, hitStunAnimation.AnimationSpeed);

        if (movementData != null)
        {
            //NOTE: Order needs to match that of the ActionState.MovementState enum

            Dictionary<int, AnimationStatData.AnimationData> movementAnimations = new Dictionary<int, AnimationStatData.AnimationData>();

            if (movementData.MoveData != null)
                movementAnimations.Add(0, new AnimationStatData.AnimationData(movementData.MoveData.AnimationData.Animation, movementData.MoveData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(0, null);            

            if (movementData.AirMoveData != null)
                movementAnimations.Add(1, new AnimationStatData.AnimationData(movementData.AirMoveData.AnimationData.Animation, movementData.AirMoveData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(1, null);            

            if (movementData.WallLeanData != null)
                movementAnimations.Add(3, new AnimationStatData.AnimationData(movementData.WallLeanData.AnimationData.Animation, movementData.WallLeanData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(3, null);

            if (movementData.WallSlideData != null)
                movementAnimations.Add(4, new AnimationStatData.AnimationData(movementData.WallSlideData.AnimationData.Animation, movementData.WallSlideData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(4, null);

            if (movementData.LedgeClimbData != null)
                movementAnimations.Add(5, new AnimationStatData.AnimationData(movementData.LedgeClimbData.AnimationData.Animation, movementData.LedgeClimbData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(5, null);

            if (movementData.GroundedJumpData != null)
                movementAnimations.Add(6, new AnimationStatData.AnimationData(movementData.GroundedJumpData.AnimationData.Animation, movementData.GroundedJumpData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(6, null);

            if (movementData.AirJumpData != null)
                movementAnimations.Add(7, new AnimationStatData.AnimationData(movementData.AirJumpData.AnimationData.Animation, movementData.AirJumpData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(7, null);

            if (movementData.WallJumpData != null)
                movementAnimations.Add(8, new AnimationStatData.AnimationData(movementData.WallJumpData.AnimationData.Animation, movementData.WallJumpData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(8, null);

            if (movementData.DashData != null)
                movementAnimations.Add(10, new AnimationStatData.AnimationData(movementData.DashData.AnimationData.Animation, movementData.DashData.AnimationData.AnimationSpeed));
            else
                movementAnimations.Add(10, null);

            statData.MovementAnimations = movementAnimations;
        }

        if (attackData != null)
        {
            //NOTE: Order needs to match that of the ActionState.AttackState enum

            Dictionary<int, AnimationStatData.AnimationData> attackAnimations = new Dictionary<int, AnimationStatData.AnimationData>();

            if (attackData.UpTiltData != null)
                attackAnimations.Add(0, new AnimationStatData.AnimationData(attackData.UpTiltData.AnimationData.Animation, attackData.UpTiltData.AnimationData.AnimationSpeed));
            else
                attackAnimations.Add(0, null);

            if (attackData.DownTiltData != null)
                attackAnimations.Add(1, new AnimationStatData.AnimationData(attackData.DownTiltData.AnimationData.Animation, attackData.DownTiltData.AnimationData.AnimationSpeed));
            else
                attackAnimations.Add(1, null);

            if (attackData.ForwardTiltData != null)
                attackAnimations.Add(2, new AnimationStatData.AnimationData(attackData.ForwardTiltData.AnimationData.Animation, attackData.ForwardTiltData.AnimationData.AnimationSpeed));
            else
                attackAnimations.Add(2, null);

            if (attackData.UpAirData != null)
                attackAnimations.Add(3, new AnimationStatData.AnimationData(attackData.UpAirData.AnimationData.Animation, attackData.UpAirData.AnimationData.AnimationSpeed));
            else
                attackAnimations.Add(3, null);

            if (attackData.DownAirData != null)
                attackAnimations.Add(4, new AnimationStatData.AnimationData(attackData.DownAirData.AnimationData.Animation, attackData.DownAirData.AnimationData.AnimationSpeed));
            else
                attackAnimations.Add(4, null);

            if (attackData.ForwardAirData != null)
                attackAnimations.Add(5, new AnimationStatData.AnimationData(attackData.ForwardAirData.AnimationData.Animation, attackData.ForwardAirData.AnimationData.AnimationSpeed));
            else
                attackAnimations.Add(5, null);

            statData.AttackAnimations = attackAnimations;
        }

        return statData;
    }   

    private MovementStatData ParseMovementData(MovementDataCollection data)
    {        
        MovementStatData statData = new MovementStatData();

        statData.MaxGroundedVelocity = baseSceneObjectData.GroundedMaxVelocity;
        statData.GroundedDecceleration = baseSceneObjectData.GroundedDecceleration;

        statData.MaxAerialXVelocity = baseSceneObjectData.AerialMaxXVelocity;
        statData.AerialXDecceleration = baseSceneObjectData.AerialXDecceleration;
        statData.MaxAerialRisingVelocity = baseSceneObjectData.AerialMaxRisingVelocity;
        statData.AerialRisingDecceleration = baseSceneObjectData.AerialRisingDecceleration;
        statData.MaxFallVelocity = baseSceneObjectData.AerialMaxFallVelocity;
    
        statData.GravityRaising = baseSceneObjectData.GravityRaising;
        statData.GravityFalling = baseSceneObjectData.GravityFalling;
        statData.GravityFastFalling = baseSceneObjectData.GravityFastFalling;
        statData.GravityHitStunTravel = baseSceneObjectData.GravityHitStunTravel;
        statData.GravityHitStunRecovery = baseSceneObjectData.GravityHitStunRecovery;

        if (data == null)
            return statData;

        if (data.MoveData != null && data.MoveData.IsValid())
        {
            statData.GroundedAcceleration = data.MoveData.GroundedXAcceleration;
            statData.GroundedAttackDecceleration = data.MoveData.GroundedAttackDecceleration;
            statData.GroundedMovementValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.MoveData is null or invalid", gameObject);

        if (data.DashData != null && data.DashData.IsValid())
        {
            statData.WaveLandDashVelocityScaler = data.DashData.WaveLandVelocityScaler;
            statData.WaveLandDashDuration = data.DashData.WaveLandDuration;
            statData.InitialDashVelocityScaler = data.DashData.InitialDashVelocityScaler;
            statData.InitialDashDuration = data.DashData.InitalDashDuration;
            statData.HorizontalDashVelocity = data.DashData.HorizontalDashVelocity;
            statData.HorizontalDashDuration = data.DashData.HorizontalDashDuration;
            statData.DashValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.DashData is null or invalid", gameObject);

        if (data.AirMoveData != null && data.AirMoveData.IsValid())
        {
            statData.AerialXAcceleration = data.AirMoveData.AerialXAcceleration;
            statData.AerialMovementValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.AirMoveData is null or invalid", gameObject);

        if (data.GroundedJumpData != null && data.GroundedJumpData.IsValid())
        {
            statData.JumpVelocity = data.GroundedJumpData.JumpVelocity;
            statData.ShortHopVelocity = data.GroundedJumpData.ShortHopVelocity;
            statData.JumpSquatDuration = data.GroundedJumpData.JumpSquatDuration;
            statData.GroundedJumpValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.GroundedJumpData is null or invalid", gameObject);

        if (data.AirJumpData != null && data.AirJumpData.IsValid())
        {
            statData.AirJumpVelocity = data.AirJumpData.JumpVelocity;
            statData.AirJumpsAvailable = data.AirJumpData.AdditionalJumpsAvailable;
            statData.AerialJumpValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.AirJumpData is null or invalid", gameObject);

        if (data.WallJumpData != null && data.WallJumpData.IsValid())
        {
            statData.WallJumpVelocity = data.WallJumpData.JumpVelocity;
            statData.WallJumpAngle = data.WallJumpData.JumpAngle;
            statData.WallJumpValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.WallJumpData is null or invalid", gameObject);

        if (data.LedgeClimbData != null && data.LedgeClimbData.IsValid())
        {
            statData.LedgeClimbValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.LedgeClimbData is null or invalid", gameObject);

        if (data.WallLeanData != null && data.WallLeanData.IsValid())
        {
            statData.WallLeanValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.WallLeanData is null or invalid", gameObject);

        if (data.WallSlideData != null && data.WallSlideData.IsValid())
        {
            statData.MaxWallSlideVelocity = data.WallSlideData.SlideVelocity;
            statData.WallSlideDeceleration = data.WallSlideData.SlideDecceleration;
            statData.WallSlideValid = true;
        }
        else
            Debug.LogWarning(gameObject.name + "'s MovementData.WallSlideData is null or invalid", gameObject);

        return statData;
    }

    #endregion


    #region Runtime Data Change

    private void SubscribeToRuntimeDataChanges()
    {
        #if UNITY_EDITOR

        baseSceneObjectData.OnChangedEvent += OnDataChanged;

        SubscribeToAnimationCollection(baseSceneObjectData.AnimationCollection);
        SubscribeToMovementCollection(baseSceneObjectData.MovementCollection);

        #endif
    }

    private void UnsubscribeFromRuntimeDataChanges()
    {
        #if UNITY_EDITOR

        baseSceneObjectData.OnChangedEvent -= OnDataChanged;

        UnsubscribeFromAnimationCollection(baseSceneObjectData.AnimationCollection);
        UnsubscribeFromMovementCollection(baseSceneObjectData.MovementCollection);

        #endif
    }


    #region Base-Animation Data
    
    private void SubscribeToAnimationCollection(BaseAnimationCollection animationData)
    {
        #if UNITY_EDITOR
        if (animationData == null) return;

        if (animationData.GroundIdleAnimation != null)
            animationData.GroundIdleAnimation.OnChangedEvent += OnDataChanged;
        if (animationData.AirIdleAnimation != null)
            animationData.AirIdleAnimation.OnChangedEvent += OnDataChanged;
        if (animationData.HitStunAnimation != null)
            animationData.HitStunAnimation.OnChangedEvent += OnDataChanged;
        #endif
    }

    private void UnsubscribeFromAnimationCollection(BaseAnimationCollection animationData)
    {
        #if UNITY_EDITOR
        if (animationData == null) return;

        if (animationData.GroundIdleAnimation != null)
            animationData.GroundIdleAnimation.OnChangedEvent -= OnDataChanged;
        if (animationData.AirIdleAnimation != null)
            animationData.AirIdleAnimation.OnChangedEvent -= OnDataChanged;
        if (animationData.HitStunAnimation != null)
            animationData.HitStunAnimation.OnChangedEvent -= OnDataChanged;
        #endif
    }

    #endregion


    #region  Movement Data
    
    private void SubscribeToMovementCollection(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        movementData.OnChangedEvent += OnMovementCollectionChanged;
        SubscribeToMovementData(movementData);
        #endif
    }

    private void UnsubscribeFromMovementCollection(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        movementData.OnChangedEvent -= OnMovementCollectionChanged;
        UnsubscribeFromMovementData(movementData);
        #endif
    }

    private void SubscribeToMovementData(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        if (movementData.MoveData != null)       movementData.MoveData.OnChangedEvent       += OnDataChanged;
        if (movementData.DashData != null)       movementData.DashData.OnChangedEvent       += OnDataChanged;
        if (movementData.AirMoveData != null)    movementData.AirMoveData.OnChangedEvent    += OnDataChanged;
        if (movementData.GroundedJumpData != null)       movementData.GroundedJumpData.OnChangedEvent       += OnDataChanged;
        if (movementData.AirJumpData != null)    movementData.AirJumpData.OnChangedEvent    += OnDataChanged;
        if (movementData.WallJumpData != null)   movementData.WallJumpData.OnChangedEvent   += OnDataChanged;
        if (movementData.LedgeClimbData != null) movementData.LedgeClimbData.OnChangedEvent += OnDataChanged;
        if (movementData.WallLeanData != null)   movementData.WallLeanData.OnChangedEvent   += OnDataChanged;
        if (movementData.WallSlideData != null)  movementData.WallSlideData.OnChangedEvent  += OnDataChanged;
        #endif
    }

    private void UnsubscribeFromMovementData(MovementDataCollection movementData)
    {
        #if UNITY_EDITOR
        if (movementData == null) return;

        if (movementData.MoveData != null)       movementData.MoveData.OnChangedEvent       -= OnDataChanged;
        if (movementData.DashData != null)       movementData.DashData.OnChangedEvent       -= OnDataChanged;
        if (movementData.AirMoveData != null)    movementData.AirMoveData.OnChangedEvent    -= OnDataChanged;
        if (movementData.GroundedJumpData != null)       movementData.GroundedJumpData.OnChangedEvent       -= OnDataChanged;
        if (movementData.AirJumpData != null)    movementData.AirJumpData.OnChangedEvent    -= OnDataChanged;
        if (movementData.WallJumpData != null)   movementData.WallJumpData.OnChangedEvent   -= OnDataChanged;
        if (movementData.LedgeClimbData != null) movementData.LedgeClimbData.OnChangedEvent -= OnDataChanged;
        if (movementData.WallLeanData != null)   movementData.WallLeanData.OnChangedEvent   -= OnDataChanged;
        if (movementData.WallSlideData != null)  movementData.WallSlideData.OnChangedEvent  -= OnDataChanged;
        #endif
    }

    private void OnMovementCollectionChanged()
    {
        #if UNITY_EDITOR
        UnsubscribeFromMovementData(baseSceneObjectData.MovementCollection);
        SubscribeToMovementData(baseSceneObjectData.MovementCollection);
        #endif

        OnDataChanged();
    }

    #endregion
    
    
    private void OnDataChanged()
    {
        if (baseSceneObjectData.IsValid())
            UpdateSceneObjectStats();
        else
            Debug.Log("StatHandler: Change to SceneObject Base Data is not valid", gameObject);
    }

    #endregion
}
