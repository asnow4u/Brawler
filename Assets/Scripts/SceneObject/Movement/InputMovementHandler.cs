using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public partial class InputMovementHandler : MovementHandler, IMovementAction
{    
    private IMovementInput movementInput;

    //Movement State Data
    [SerializeField] private MovementState curMovementState;
    public MovementState CurMovementState => curMovementState;
    
    [Header("Influence")]
    [Range(0, 1)]
    [SerializeField] private float horizontalDeadzone;
    [Range(-1, 1)]
    [SerializeField] private float horizontalInfluence;

    [Range(0, 1)]
    [SerializeField] private float verticalDeadzone;
    [Range(-1, 1)]
    [SerializeField] private float verticalInfluence;

    [Range(0, 1)]
    [SerializeField] private float jumpDeadzone;
    [Range(0, 1)]
    [SerializeField] private float jumpInfluence;    
    
    //Climb Properties
    [SerializeField] private bool isClimbSliding = false;
    private const float climbSlideVelocityThreshold = -10f; //When switching to climbing, this determines whether a slide decceleration is applied

    //Ledge Climb Properties
    [Header("Ledge Climb")]
    [SerializeField] private float ledgeClimbDuration = 0.2f;
    private bool isLedgeClimbing = false;
    private float ledgeClimbStartTime = -100f;
    private Vector3 ledgeClimbStartPosition;
    private Vector3 ledgeClimbPullUpPosition;
    private Vector3 ledgeClimbStandPosition;
    private float storedLedgeClimbXVelocity;
    private const float requiredLedgeClimbPercentage = 0.3f;

    [Header("Buffer")]
    [SerializeField] private float movementBufferWindow = 0.1f;
    private ActionBuffer<float> actionBuffer;

    [Header("DI / Drift")]
    [Tooltip("Max degrees DI can rotate the launch angle during the hit pause. Magnitude is never changed.")]
    [Range(0f, 45f)]
    [SerializeField] protected float maxDIAngle = 15f;
    [Tooltip("Cap on total horizontal velocity mid-flight drift can add across one hitstun. Keeps drift a nudge, not a steer.")]
    [Range(0f, 10f)]
    [SerializeField] private float maxDriftSpeed = 2.5f;
    [Tooltip("How fast drift ramps toward maxDriftSpeed while a direction is held during Travel/Recovery.")]
    [Range(0f, 30f)]
    [SerializeField] private float driftAccel = 8f;
    private float driftVelocityApplied = 0f;

    public event Action<MovementState> MovementStateChangedEvent;
    
    
    #region Movement Conditions

    private bool groundedMovementAllowed => curMovementData.GroundedMovementValid &&
                                            horizontalInfluence != 0 &&
                                            actionState.CurActionState <= ActionState.Moving;
    private bool aerialMovementAllowed => curMovementData.AerialMovementValid &&
                                          (horizontalInfluence != 0 || verticalInfluence != 0) &&
                                          actionState.CurActionState <= ActionState.Attacking;
    private bool dashMovementAllowed => curMovementData.DashValid &&
                                        horizontalInfluence != 0 &&
                                        isLedgeClimbing == false &&
                                        actionState.CurActionState <= ActionState.Moving;
    private bool jumpMovementAllowed => curMovementData.GroundedJumpValid &&
                                        jumpRequested &&
                                        jumpInputAvailable &&
                                        actionState.CurActionState <= ActionState.Moving;
    private bool aerialJumpMovementAllowed => curMovementData.AerialJumpValid &&
                                              jumpRequested &&
                                              jumpInputAvailable &&
                                              airJumpsPerformed < curMovementData.AirJumpsAvailable &&
                                              actionState.CurActionState <= ActionState.Moving;

    private bool coyoteJumpMovementAllowed => jumpMovementAllowed &&
                                              Time.time <= lastGroundedTime + coyoteTimeDuration;

    private bool wallJumpAllowed => curMovementData.WallJumpValid &&
                                    jumpRequested &&
                                    jumpInputAvailable &&
                                    IsAgainstWall() &&
                                    actionState.CurActionState <= ActionState.Moving &&
                                    Time.time >= lastWallJumpTime + wallJumpDetachDuration;

    private bool wallLeanAllowed => curMovementData.WallLeanValid &&
                                    ((IsAgainstLeftWall() && horizontalInfluence < 0) || (IsAgainstRightWall() && horizontalInfluence > 0)) &&
                                    actionState.CurActionState <= ActionState.Moving;

    private bool wallSlideAllowed => curMovementData.WallSlideValid &&
                                     rb.linearVelocity.y < 0 &&
                                     ((IsAgainstRightWall() && horizontalInfluence >= 0) || (IsAgainstLeftWall() && horizontalInfluence <= 0)) && //Prevent wall sliding when moving away from wall
                                     verticalInfluence >= 0 && //Prevent wall sliding when fast falling
                                     actionState.CurActionState <= ActionState.Moving;

    private bool ledgeClimbAllowed => curMovementData.LedgeClimbValid &&
                                      ((horizontalInfluence > 0 && IsAgainstRightLedge()) || (horizontalInfluence < 0 && IsAgainstLeftLedge())) &&
                                      actionState.CurActionState <= ActionState.Moving;

    private bool IsAttackPermittedMovementState(MovementState moveState) => moveState == MovementState.AirMove;

    #endregion
    

    #region Initialize / Destroy

    protected override void Awake()
    {
        movementInput = GetComponent<IMovementInput>();
        if (movementInput == null)
            Debug.LogError("InputMovementHandler Requires a IMovmentInput. If no input is desired use SOMovementHandler instead.", this);
     
        actionBuffer = new ActionBuffer<float>(movementBufferWindow);        

        base.Awake();
    }

    protected override void RegisterToEvents()
    {        
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;

        movementInput.MovementPerformedEvent += SetMovementInfluence;
        movementInput.MovementStoppedEvent += ResetMovementInfluence;
        movementInput.JumpPerformedEvent += SetJumpInfluence;
        movementInput.JumpStoppedEvent += ResetJumpInfluence;

        base.RegisterToEvents();
    }

    protected override void UnregisterFromEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        actionState.GroundedStateChangedEvent -= OnGroundedStateChanged;

        movementInput.MovementPerformedEvent -= SetMovementInfluence;
        movementInput.MovementStoppedEvent -= ResetMovementInfluence;
        movementInput.JumpPerformedEvent -= SetJumpInfluence;
        movementInput.JumpStoppedEvent -= ResetJumpInfluence;
    
        base.UnregisterFromEvents();
    }

    private void OnActionStateChanged(ActionState state)
    {
        if (state == ActionState.Attacking && isDashing)
            EndDash();
    }

    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        hasJumped = false;

        if (groundedState == GroundedState.Grounded)
        {
            airJumpsPerformed = 0;
            StartWaveLanding();
        }
        else if (groundedState == GroundedState.Airborn)
        {
            if (isDashing)
                EndDash();

            if (curMovementState == MovementState.GroundMove)
                lastGroundedTime = Time.time;
        }
    }

    #endregion


    #region Environment Collision Check

    /// <summary>
    /// Determine if the sceneObject is against a wall
    /// </summary>
    private bool IsAgainstWall()
    {
        return IsAgainstRightWall() || IsAgainstLeftWall();
    }

    /// <summary>
    /// Determine if the sceneObject is against a wall on the right side
    /// </summary>
    private bool IsAgainstRightWall()
    {
        return sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Environment"), out _);
    }

    /// <summary>
    /// Determine if the sceneObject is against a wall on the left side
    /// </summary>
    private bool IsAgainstLeftWall()
    {
        return sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Environment"), out _);
    }

    private bool IsAgainstRightLedge()
    {
        if (sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Ledge"), out Collider collider))
        {
            return CheckLedge(collider, 1);
        }
        return false;    
    } 

    private bool IsAgainstLeftLedge()
    {
        if (sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Ledge"), out Collider collider))
        {
            return CheckLedge(collider, -1);
        }
        return false;
    }

    private bool CheckLedge(Collider collider, int dirX)
    {
        //Scene Object must be above the edge to vault
        if (sceneObject.Bounds.max.y <= collider.bounds.max.y) return false;

        //Calculate how much of the other collider is above this edge
        float heightDifference = sceneObject.Bounds.max.y - collider.bounds.max.y;
        float percentageAboveLedge = heightDifference / sceneObject.Bounds.size.y;  

        bool canClimb = percentageAboveLedge >= requiredLedgeClimbPercentage;

        if (canClimb)
        {
            ledgeClimbStartPosition = transform.position;

            float ledgeTopY = collider.bounds.max.y;

            // Center of the sceneObject lines up with the top of the collider
            float centerToPositionOffset = transform.position.y - sceneObject.Bounds.center.y;
            ledgeClimbStartPosition.y = ledgeTopY + centerToPositionOffset;

            float footOffset = transform.position.y - sceneObject.Bounds.min.y;
            ledgeClimbPullUpPosition = ledgeClimbStartPosition;
            ledgeClimbPullUpPosition.y = ledgeTopY + footOffset + 0.05f;

            ledgeClimbStandPosition = ledgeClimbPullUpPosition;
            ledgeClimbStandPosition.x += dirX * (sceneObject.Bounds.extents.x + 0.5f);
        }

        return canClimb;
    }

    #endregion


    #region Influence
    
    private void SetMovementInfluence(Vector2 inputInfluence)
    {
        if (inputInfluence.x > horizontalDeadzone || inputInfluence.x < -horizontalDeadzone)
            horizontalInfluence = Mathf.Clamp(inputInfluence.x, -1, 1);

        if (inputInfluence.y > verticalDeadzone || inputInfluence.y < -verticalDeadzone)
            verticalInfluence = Mathf.Clamp(inputInfluence.y, -1, 1);
    }

    private void ResetMovementInfluence()
    {
        horizontalInfluence = 0;
        verticalInfluence = 0;
    }

    public void SetJumpInfluence(float inputInfluence)
    {
        if (inputInfluence < jumpDeadzone)
            return;

        if (!jumpInputAvailable && inputInfluence == 0)
            jumpInputAvailable = true;

        jumpInfluence = Mathf.Clamp(inputInfluence, 0, 1);
        actionBuffer.Buffer(jumpInfluence);
    }

    private void ResetJumpInfluence()
    {
        if (!jumpInputAvailable)
            jumpInputAvailable = true;

        jumpInfluence = 0;
    }

    #endregion


    #region State Updates

    private void SetCurrentMoveState(MovementState moveState)
    {        
        if (moveState == curMovementState)
            return;

        bool permittedDuringAttack = actionState.CurActionState == ActionState.Attacking && 
                                     IsAttackPermittedMovementState(moveState);

        if (moveState == MovementState.Null || permittedDuringAttack || actionState.TryChangeState(ActionState.Moving))
        {
            if (moveState == MovementState.Null && actionState.CurActionState == ActionState.Moving)
                actionState.ChangeState(ActionState.Idle);

            curMovementState = moveState;
            sceneObject.Log("Movement State: " + moveState.ToString());
            MovementStateChangedEvent?.Invoke(moveState);
        }
    }        

    private void UpdateGroundedMovementState()
    {
        if (isJumpingSquating || isLedgeClimbing) 
            return;

        //Dash - locked until it ends; only a jump may cancel it (reversing direction will not)
        if (isDashing)
        {
            if (jumpMovementAllowed)
            {
                EndDash();
                SetCurrentMoveState(MovementState.GroundJump);
                StartJump();
            }
        }

        //Jump
        else if (jumpMovementAllowed)
        {
            SetCurrentMoveState(MovementState.GroundJump);
            StartJump();
        }

        //Horizontal Movement
        else if (groundedMovementAllowed)
        {
            // Against Wall
            if (wallLeanAllowed)
                SetCurrentMoveState(MovementState.WallLean);

            // Dash initiation (standstill, walk -> dash, or pivot)
            else if (dashInitiationAllowed)
                StartInitialRunDash();

            else
                SetCurrentMoveState(MovementState.GroundMove);
        }

        //Idle
        else
            SetCurrentMoveState(MovementState.Null);
    }

    private void UpdateAerialMovementState()
    {
        //NOTE: Movement not allowed while in jump animation
        if (isJumpingSquating || isLedgeClimbing)
            return;

        //Coyote Time Jump
        if (coyoteJumpMovementAllowed)
        {
            SetCurrentMoveState(MovementState.GroundJump);
            if (curMovementState == MovementState.GroundJump)
                StartJump();
        }

        //Wall Jump
        else if (wallJumpAllowed)
        {
            SetCurrentMoveState(MovementState.WallJump);
            if (curMovementState == MovementState.WallJump)
                StartJump();
        }

        //Air Jump
        else if (aerialJumpMovementAllowed)
        {
            SetCurrentMoveState(MovementState.AirJump);
            if (curMovementState == MovementState.AirJump)
                StartJump();
        }
        
        //Ledge Climb
        else if (ledgeClimbAllowed)
        {
            SetCurrentMoveState(MovementState.LedgeClimb);
            if (curMovementState == MovementState.LedgeClimb)
                BeginLedgeClimb();
        }

        //Wall Slide
        else if (wallSlideAllowed)
            SetCurrentMoveState(MovementState.WallSlide);
                       
        //Accelerate
        else if (aerialMovementAllowed)
            SetCurrentMoveState(MovementState.AirMove);

        //Idle
        else
            SetCurrentMoveState(MovementState.Null);
    }   

    /// <summary>
    /// Update the current climb movement state based on input influence and sceneObject state
    /// </summary>
    private void UpdateClimbMovementState()
    {
        if (isClimbSliding)
            return;

        //Jump
        if (jumpMovementAllowed)
            SetCurrentMoveState(MovementState.GroundJump);

        //Idle
        else
            SetCurrentMoveState(MovementState.Null);
    }

    #endregion


    #region Gravity

    protected override void ApplyGravity()
    {
        if (isLedgeClimbing)
            return;

        base.ApplyGravity();        
    }

    protected override float CalculateGravityForce()
    {
        if (actionState.CurActionState == ActionState.HitStun &&
            verticalInfluence < 0 && rb.linearVelocity.y <= 0)
        {
            return curMovementData.GravityFastFalling;   
        }
            
        return base.CalculateGravityForce();
    }

    #endregion


    #region Ground Movement

    protected override void UpdateGroundedMovement()
    {
        UpdateGroundedMovementState();

        switch (curMovementState)
        {
            case MovementState.Null:
                if (actionState.CurActionState == ActionState.Attacking && horizontalInfluence != 0)
                    DeccelerateGroundedAttackSlide();
                else
                    DeccelerateGroundedMovement();
                break;

            case MovementState.LedgeClimb:
                UpdateLedgeClimbMovement();
                break;

            case MovementState.WallLean:
                DeccelerateGroundedMovement();
                break;

            case MovementState.Dash:
                UpdateDash();
                break;

            case MovementState.GroundMove:
                UpdateGroundedAcceleration();
                break;

            case MovementState.GroundJump:
                UpdateGroundedAcceleration();
                break;
        }

        UpdateJumpSquat();
    }    

    private void UpdateGroundedAcceleration()
    {
        float currentX = rb.linearVelocity.x;

        // 1. Kill momentum if reversing
        if (horizontalInfluence != 0 && Mathf.Sign(horizontalInfluence) != Mathf.Sign(currentX))
        {
            currentX = 0;
        }

        // 2. Apply acceleration
        currentX += horizontalInfluence * curMovementData.GroundedAcceleration * Time.fixedDeltaTime;

        // 3. Clamp
        float maxSpeed = curMovementData.MaxGroundedVelocity;
        currentX = Mathf.Clamp(currentX, -maxSpeed, maxSpeed);

        rb.linearVelocity = new Vector3(currentX, rb.linearVelocity.y, 0);

        if (sceneObject.IsFacingRightDirection && rb.linearVelocity.x < 0 ||
            !sceneObject.IsFacingRightDirection && rb.linearVelocity.x > 0)
        {
            sceneObject.TurnAround();
        }
    }

    private void DeccelerateGroundedAttackSlide()
    {
        float decceleration = curMovementData.GroundedAttackDecceleration;

        if (decceleration == 0)
        {            
            decceleration = curMovementData.GroundedDecceleration;
            Debug.LogWarning("Attack Decceleration not set", gameObject);
        }

        if (rb.linearVelocity.x > 0)
        {
            float v = rb.linearVelocity.x - decceleration * Time.fixedDeltaTime;
            if (v < 0) v = 0;
            rb.linearVelocity = new Vector3(v, rb.linearVelocity.y, 0);
        }
        else if (rb.linearVelocity.x < 0)
        {
            float v = rb.linearVelocity.x + decceleration * Time.fixedDeltaTime;
            if (v > 0) v = 0;
            rb.linearVelocity = new Vector3(v, rb.linearVelocity.y, 0);
        }
    }

    #endregion


    #region Aerial Movement

    protected override void UpdateAerialMovement()
    {        
        UpdateAerialMovementState();

        switch (curMovementState)
        {
            case MovementState.LedgeClimb:
                UpdateLedgeClimbMovement();
                break;

            case MovementState.Null:
                DeccelerateAerialXMovement();
                DeccelerateAerialYRisingMovement();
                break;

            case MovementState.AirMove:

                //Horizontal Movement
                if (horizontalInfluence != 0)
                    AccelerateAerialXMovement();
                else
                    DeccelerateAerialXMovement();

                //Vertical pullback only when above rising max; fast-fall handled by gravity
                DeccelerateAerialYRisingMovement();
                break;

            case MovementState.GroundJump:
            case MovementState.AirJump:
            case MovementState.WallJump:

                //Horizontal Movement
                if (horizontalInfluence != 0)
                    AccelerateAerialXMovement();
                else
                    DeccelerateAerialXMovement();
                break;

            case MovementState.WallSlide:

                if (IsAgainstRightWall() && !sceneObject.IsFacingRightDirection || IsAgainstLeftWall() && sceneObject.IsFacingRightDirection)
                    sceneObject.TurnAround();

                DeccelerateWallSlide();
                break;
        }
    }

    private void AccelerateAerialXMovement()
    {
        float maxXVelocity = curMovementData.MaxAerialXVelocity;
        float acceleration = curMovementData.AerialXAcceleration;

        //Positive Acceleration
        if (horizontalInfluence > 0)
        {
            float acceleratedXValue = rb.linearVelocity.x + (acceleration * Time.fixedDeltaTime);

            if (acceleratedXValue > maxXVelocity)
                acceleratedXValue = maxXVelocity;

            rb.linearVelocity = new Vector3(acceleratedXValue, rb.linearVelocity.y, 0);
        }

        //Negative Acceleration
        else if (horizontalInfluence < 0)
        {
            float acceleratedXValue = rb.linearVelocity.x - (acceleration * Time.fixedDeltaTime);

            if (acceleratedXValue < -maxXVelocity)
                acceleratedXValue = -maxXVelocity;

            rb.linearVelocity = new Vector3(acceleratedXValue, rb.linearVelocity.y, 0);
        }
    }


    #endregion


    #region  HitStun

    protected override void OnRecievedHitStunKnockback(KnockBackHitData hitData)
    {
        actionBuffer.Clear();
        driftVelocityApplied = 0f;
        base.OnRecievedHitStunKnockback(hitData);
    }

    protected override void UpdateHitStunMovement()
    {
        isJumpSquatPending = false;
        base.UpdateHitStunMovement();
    }


    // Direction Launch Influence
    protected override Vector3 ApplyLaunchVelocity(Vector3 knockbackVelocity)
    {
        if (maxDIAngle <= 0f)
            return knockbackVelocity; 

        float magnitude = knockbackVelocity.magnitude;
        if (magnitude < 0.0001f)
            return knockbackVelocity;

        Vector2 knockDir = new Vector2(knockbackVelocity.x, knockbackVelocity.y) / magnitude;
        Vector2 input = new Vector2(horizontalInfluence, verticalInfluence);

        // Component of input perpendicular to the knockback direction.
        Vector2 perp = input - Vector2.Dot(input, knockDir) * knockDir;
        float perpAmount = Mathf.Clamp01(perp.magnitude);
        if (perpAmount < 0.0001f)
            return knockbackVelocity;

        Vector2 perpDir = perp / perp.magnitude;

        // Blend the original direction toward the perpendicular by the DI angle, then restore magnitude.
        float angleRad = maxDIAngle * Mathf.Deg2Rad * perpAmount;
        Vector2 newDir = knockDir * Mathf.Cos(angleRad) + perpDir * Mathf.Sin(angleRad);

        return new Vector3(newDir.x, newDir.y, 0f) * magnitude;
    }

    // Mid-flight drift (DI): additive, accumulator-capped horizontal nudge while a direction is held.
    protected override void ApplyHitStunDrift()
    {
        if (maxDriftSpeed <= 0f || horizontalInfluence == 0f)
            return;

        float delta = driftAccel * horizontalInfluence * Time.fixedDeltaTime;
        float newApplied = Mathf.Clamp(driftVelocityApplied + delta, -maxDriftSpeed, maxDriftSpeed);
        float actualDelta = newApplied - driftVelocityApplied;
        driftVelocityApplied = newApplied;

        if (actualDelta != 0f)
        {
            Vector3 v = rb.linearVelocity;
            v.x += actualDelta;
            rb.linearVelocity = v;
        }
    }

    #endregion


    #region Wall Slide    

    private void DeccelerateWallSlide()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > curMovementData.MaxWallSlideVelocity)
        {
            float decceleratedYValue = rb.linearVelocity.y + curMovementData.WallSlideDeceleration * Time.fixedDeltaTime;

            if (decceleratedYValue > 0)
                decceleratedYValue = 0;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
        }
    }

    #endregion


    #region Ledge Climb

    private void BeginLedgeClimb()
    {
        isLedgeClimbing = true;
        ledgeClimbStartTime = Time.time;
        storedLedgeClimbXVelocity = rb.linearVelocity.x;
        rb.linearVelocity = Vector3.zero;
        rb.useGravity = false;
        
        transform.position = ledgeClimbStartPosition;

        // Ensure player is facing the ledge
        if (horizontalInfluence > 0 && !sceneObject.IsFacingRightDirection)
            sceneObject.TurnAround();
        else if (horizontalInfluence < 0 && sceneObject.IsFacingRightDirection)
            sceneObject.TurnAround();
    }

    private void UpdateLedgeClimbMovement()
    {
        float elapsed = Time.time - ledgeClimbStartTime;

        if (elapsed >= ledgeClimbDuration)
        {
            transform.position = ledgeClimbStandPosition;
            rb.linearVelocity = new Vector3(storedLedgeClimbXVelocity, 0, 0);
            isLedgeClimbing = false;
            SetCurrentMoveState(MovementState.Null);
            return;
        }

            float t = Mathf.Clamp01(elapsed / ledgeClimbDuration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            if (easedT < 0.5f)
            {
                float phaseT = easedT * 2f;
            transform.position = Vector3.Lerp(ledgeClimbStartPosition, ledgeClimbPullUpPosition, phaseT);
            }
            else
            {
                float phaseT = (easedT - 0.5f) * 2f;
            transform.position = Vector3.Lerp(ledgeClimbPullUpPosition, ledgeClimbStandPosition, phaseT);
            }
    }

    #endregion
}