using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(HurtBoxHandler))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public partial class MovementHandler : MonoBehaviour
{
    protected ISceneObject sceneObject;
    protected IActionState actionState;
    protected IStats statHandler;
    protected ISOHurtBoxHandler hurtBoxHandler;

    protected Rigidbody rb;
    protected Collider col;
    
    protected MovementStatData curMovementData = null;
    
    protected virtual bool UsesNativeGravity => true;
    protected float sleepVelocityThreshold;


    protected virtual void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("MovementHandler requires a component that implements ISceneObject");

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        hurtBoxHandler = GetComponent<ISOHurtBoxHandler>();
        
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0;
        rb.useGravity = UsesNativeGravity;
        sleepVelocityThreshold = Mathf.Sqrt(2f * rb.sleepThreshold);

        col = GetComponent<Collider>();

        InitHitStunCollision();

        RegisterToEvents();
    }

    protected virtual void RegisterToEvents()
    {
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;

        hurtBoxHandler.OnHitEvent += OnRecievedHitStunKnockback;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    protected virtual void UnregisterFromEvents()
    {
        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;

        hurtBoxHandler.OnHitEvent -= OnRecievedHitStunKnockback;
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }

    private void OnMovementStatsChanged(MovementStatData movementData)
    {
        curMovementData = movementData;
    }

    private void FixedUpdate()
    {
        if (curMovementData == null)
            return;

        if (actionState.CurActionState == ActionState.HitStun)
            UpdateHitStunMovement();
        else
            UpdateMovement();

        preSolveVelocity = rb.linearVelocity;
    }

    protected virtual void UpdateMovement()
    {
        if (!UsesNativeGravity)
            ApplyGravity();

        if (actionState.CurGroundedState == GroundedState.Grounded)
            UpdateGroundedMovement();

        else if (actionState.CurGroundedState == GroundedState.Airborn)
            UpdateAerialMovement();
    }


    #region Gravity

    protected virtual void ApplyGravity()
    {        
        float gravityForce = CalculateGravityForce();

        rb.linearVelocity += new Vector3(0, -gravityForce * Time.fixedDeltaTime, 0);

        //Clamp downward velocity to the appropriate max fall speed
        if (rb.linearVelocity.y < 0)
        {
            float maxFall = curMovementData.MaxFallVelocity;
            if (rb.linearVelocity.y < -maxFall)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFall, 0);
        }
    }

    protected virtual float CalculateGravityForce()
    {
        if (actionState.CurActionState == ActionState.HitStun)
        {
            HitStunState hitStunState = hurtBoxHandler.CurHitStunState;

            //Launch and Pause: no gravity (committed trajectory / frozen)
            if (hitStunState == HitStunState.Launch || hitStunState == HitStunState.Pause)
                return 0;

            if (hitStunState == HitStunState.Travel)
                return curMovementData.GravityHitStunTravel;
            else if (hitStunState == HitStunState.Recovery)
                return curMovementData.GravityHitStunRecovery;
            else
                return 0;
        }
        else
        {
            if (rb.linearVelocity.y > 0)
                return curMovementData.GravityRaising;
            else
                return curMovementData.GravityFalling;
        }
    }

    #endregion


    #region Grounded Movement

    protected virtual void UpdateGroundedMovement()
    {
        DeccelerateGroundedMovement();
    }

    protected void DeccelerateGroundedMovement()
    {
        if (UsesNativeGravity && Mathf.Abs(rb.linearVelocity.x) <= sleepVelocityThreshold)
            return;

        //Positive Decceleration
        if (rb.linearVelocity.x > 0)
        {
            float decceleratedXValue = rb.linearVelocity.x - curMovementData.GroundedDecceleration * Time.fixedDeltaTime;

            if (decceleratedXValue < 0)
                decceleratedXValue = 0;

            rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
        }

        //Negative Decceleration
        else if (rb.linearVelocity.x < 0)
        {
            float decceleratedXValue = rb.linearVelocity.x + curMovementData.GroundedDecceleration * Time.fixedDeltaTime;

            if (decceleratedXValue > 0)
                decceleratedXValue = 0;

            rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
        }
    }

    #endregion


    #region Aerial Movement

    protected virtual void UpdateAerialMovement()
    {
        DeccelerateAerialXMovement();
        DeccelerateAerialYRisingMovement();
    }

    protected void DeccelerateAerialXMovement()
    {
        //Only deccelerate if velocity is greater than max velocity
        if (Mathf.Abs(rb.linearVelocity.x) > curMovementData.MaxAerialXVelocity)
        {
            //Positive Decceleration
            if (rb.linearVelocity.x > 0)
            {
                float decceleratedXValue = rb.linearVelocity.x - curMovementData.AerialXDecceleration * Time.fixedDeltaTime;

                if (decceleratedXValue < 0)
                    decceleratedXValue = 0;

                rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
            }

            //Negative Decceleration
            else if (rb.linearVelocity.x < 0)
            {
                float decceleratedXValue = rb.linearVelocity.x + curMovementData.AerialXDecceleration * Time.fixedDeltaTime;

                if (decceleratedXValue > 0)
                    decceleratedXValue = 0;

                rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
            }
        }
    }

    protected void DeccelerateAerialYRisingMovement()
    {
        //Only pull back when rising above the max
        if (rb.linearVelocity.y > curMovementData.MaxAerialRisingVelocity)
        {
            float decceleratedYValue = rb.linearVelocity.y - curMovementData.AerialRisingDecceleration * Time.fixedDeltaTime;

            if (decceleratedYValue < curMovementData.MaxAerialRisingVelocity)
                decceleratedYValue = curMovementData.MaxAerialRisingVelocity;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
        }
    } 

    #endregion
}
