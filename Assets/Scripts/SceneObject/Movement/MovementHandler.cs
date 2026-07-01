using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(HurtBoxHandler))]
[RequireComponent(typeof(Rigidbody))]
internal partial class MovementHandler : MonoBehaviour, IMovement
{
    private ISceneObject sceneObject;
    private IMovementInput movementInput;
    private IActionState actionState;
    private IStats statHandler;
    private ISOHurtBoxHandler hurtBoxHandler;

    private Rigidbody rb;
    private ActionBuffer<float> actionBuffer;

    //Movement State Data
    [SerializeField] private MovementState curMovementState;
    public MovementState CurMovementState => curMovementState;
    
    private MovementStatData curMovementData = null;

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

    //Conditions
    private bool groundedMovementAllowed => curMovementData.GroundedMovementValid &&
                                            horizontalInfluence != 0 &&
                                            actionState.CurActionState <= ActionState.Moving;
    private bool aerialMovementAllowed => curMovementData.AerialMovementValid &&
                                          (horizontalInfluence != 0 || verticalInfluence != 0) &&
                                          actionState.CurActionState <= ActionState.Attacking;
    private bool climbMovementAllowed => curMovementData.ClimbMovementValid &&
                                         (horizontalInfluence != 0 || verticalInfluence != 0) &&
                                         actionState.CurActionState <= ActionState.Moving;
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

    public event Action<MovementState> MovementStateChangedEvent;

    #region Initialize / Destroy

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("MovementHandler requires a component that implements ISceneObject");

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        hurtBoxHandler = GetComponent<ISOHurtBoxHandler>();

        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0;
        rb.useGravity = false;

        //Not a requirement (Null means no inputs are ever recieved
        movementInput = GetComponent<IMovementInput>();        
        actionBuffer = new ActionBuffer<float>(movementBufferWindow);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.ActionStateChangedEvent += OnActionStateChanged;
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        actionState.ClimbStateChangedEvent += OnClimbStateChanged;

        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;

        hurtBoxHandler.OnHitEvent += ApplyHitStunKnockback;
        hurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
        
        if (movementInput != null)
        {
            movementInput.MovementPerformedEvent += SetMovementInfluence;
            movementInput.MovementStoppedEvent += ResetMovementInfluence;
            movementInput.JumpPerformedEvent += SetJumpInfluence;
            movementInput.JumpStoppedEvent += ResetJumpInfluence;
        }
    }

    private void OnDestroy()
    {
        UnregisterToEvents();
    }

    private void UnregisterToEvents()
    {
        actionState.ActionStateChangedEvent -= OnActionStateChanged;
        actionState.GroundedStateChangedEvent -= OnGroundedStateChanged;
        actionState.ClimbStateChangedEvent -= OnClimbStateChanged;

        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;

        hurtBoxHandler.OnHitEvent -= ApplyHitStunKnockback;
        hurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;

        if (movementInput != null)
        {
            movementInput.MovementPerformedEvent -= SetMovementInfluence;
            movementInput.MovementStoppedEvent -= ResetMovementInfluence;
            movementInput.JumpPerformedEvent -= SetJumpInfluence;
            movementInput.JumpStoppedEvent -= ResetJumpInfluence;
        }
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

    /// <summary>
    /// Handle Climb state changed event
    /// </summary>
    private void OnClimbStateChanged(ClimbState climbState)
    {
        isClimbSliding = false;

        //Reset jumps
        if (climbState == ClimbState.Climbing)
        {
            airJumpsPerformed = 0;

            rb.useGravity = false;

            if (rb.linearVelocity.y < climbSlideVelocityThreshold)
            {
                rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
                isClimbSliding = true;
            }
            else
                rb.linearVelocity = Vector3.zero;
        }

        else
            rb.useGravity = true;
    }

    private void OnMovementStatsChanged(MovementStatData movementData)
    {
        curMovementData = movementData;
    }

    #endregion


    #region Wall Check

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


    #region State    

    /// <summary>
    /// Set the current movement state to <paramref name="inputData"/> if possible
    /// </summary>
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

    /// <summary>
    /// Update the current grounded movement state based on input influence and sceneObject state
    /// </summary>
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

    /// <summary>
    /// Update the current aerial movement state based on input influence and sceneObject state
    /// </summary>
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

        //Accelerate
        else if (climbMovementAllowed)
            SetCurrentMoveState(MovementState.ClimbMove);

        //Idle
        else
            SetCurrentMoveState(MovementState.Null);
    }

    #endregion


    #region Movement Update

    private void FixedUpdate()
    {
        if (actionState.CurActionState == ActionState.HitStun)
        {
            isJumpSquatPending = false; // Cancel a pending squat launch if hit mid-squat
            UpdateHitStunMovement();
        }

        else
        {
            ApplyGravity();

            if (actionState.CurGroundedState == GroundedState.Grounded)
                UpdateGroundedMovement();

            else if (actionState.CurGroundedState == GroundedState.Airborn)
                UpdateAerialMovement();

            UpdateJumpSquat();
        }
    }

    /// <summary>
    /// Apply gravity based on current state. Selects from movement gravity values when not in hit stun,
    /// and from hit stun gravity values when in Travel or Recovery. Launch and Pause skip gravity entirely.
    /// After gravity is applied, downward velocity is clamped to the appropriate max fall speed.
    /// </summary>
    private void ApplyGravity()
    {
        if (isLedgeClimbing)
            return;

        float gravityForce;

        if (actionState.CurActionState == ActionState.HitStun)
        {
            HitStunState hitStunState = hurtBoxHandler.CurHitStunState;

            //Launch and Pause: no gravity (committed trajectory / frozen)
            if (hitStunState == HitStunState.Launch || hitStunState == HitStunState.Pause)
                return;

            if (hitStunState == HitStunState.Travel)
                gravityForce = curMovementData.GravityHitStunTravel;
            else if (hitStunState == HitStunState.Recovery)
                gravityForce = curMovementData.GravityHitStunRecovery;
            else
                return;
        }
        else
        {
            if (rb.linearVelocity.y > 0)
                gravityForce = curMovementData.GravityRaising;
            else if (verticalInfluence < 0 && rb.linearVelocity.y <= 0)
                gravityForce = curMovementData.GravityFastFalling;
            else
                gravityForce = curMovementData.GravityFalling;
        }

        rb.linearVelocity += new Vector3(0, -gravityForce * Time.fixedDeltaTime, 0);

        //Clamp downward velocity to the appropriate max fall speed
        if (rb.linearVelocity.y < 0)
        {
            float maxFall = curMovementData.MaxFallVelocity;
            if (rb.linearVelocity.y < -maxFall)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxFall, 0);
        }
    }

    /// <summary>
    /// Update movement on the ground based on current movement state
    /// </summary>
    private void UpdateGroundedMovement()
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
    }    

    /// <summary>
    /// Update horizontal movement in the air
    /// </summary>
    private void UpdateAerialMovement()
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
    
    private void UpdateHitStunMovement()
    {
        if (isSplatHolding)
        {
            UpdateBounceSplat();
            return;
        }

        switch (hurtBoxHandler.CurHitStunState)
        {
            case HitStunState.Launch:
                CheckForHitStunBounce();
                break;

            case HitStunState.Travel:
                UpdateHitStunDeceleration();
                ApplyGravity();
                CheckForHitStunBounce();
                break;

            case HitStunState.Recovery:
                UpdateHitStunDeceleration();
                ApplyGravity();
                CheckForHitStunBounce();
                break;
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


    #region Climb Movement

    private void CheckForClimbingStateChange()
    {
        if (!curMovementData.ClimbMovementValid ||
            isJumpingSquating ||
            verticalInfluence == 0 ||
            actionState.CurClimbState == ClimbState.Unavailable)
        {
            return;
        }

        if (actionState.CurClimbState == ClimbState.Available)
        {
            if (actionState.CurGroundedState == GroundedState.Grounded && verticalInfluence > 0)
                actionState.ChangeClimbState(ClimbState.Climbing);

            else if (actionState.CurGroundedState == GroundedState.Airborn && verticalInfluence != 0)
                actionState.ChangeClimbState(ClimbState.Climbing);
        }

        else if (actionState.CurClimbState == ClimbState.Climbing)
        {
            if (actionState.CurGroundedState == GroundedState.Grounded && verticalInfluence < 0)
                actionState.ChangeClimbState(ClimbState.Available);
        }
    }    

    private void UpdateClimbMovement()
    {
        UpdateClimbMovementState();

        switch (curMovementState)
        {
            case MovementState.Null:
                if (rb.linearVelocity.y > climbSlideVelocityThreshold)
                    rb.linearVelocity = new Vector3(0, 0, 0);
                else
                    UpdateClimbDecceleration();
                break;

            case MovementState.ClimbMove:
                UpdateClimbAcceleration();
                break;

            case MovementState.GroundJump:
                StartJump();
                actionState.ChangeClimbState(ClimbState.Available);
                break;
        }
    }

    /// <summary>
    /// Accelerate while on a climbable surface
    /// </summary>
    private void UpdateClimbAcceleration()
    {
        float climbXVelocity = horizontalInfluence * curMovementData.MaxClimbXVelocity;
        float climbYVelocity = 0;

        //Climb Up
        if (verticalInfluence > 0)
            climbYVelocity = verticalInfluence * curMovementData.MaxClimbUpYVelocity;

        //Climb Down
        else if (verticalInfluence < 0)
            climbYVelocity = verticalInfluence * curMovementData.MaxClimbDownYVelocity;

        rb.linearVelocity = new Vector3(climbXVelocity, climbYVelocity, 0);
    }

    /// <summary>
    /// Deccelerate while on a climbable surface <br/>
    /// This occurs when grabbing a climbable surface while moving downwards
    /// </summary>
    private void UpdateClimbDecceleration()
    {
        float decceleration = curMovementData.ClimbSlideDecceleration;

        float velocity = rb.linearVelocity.y + (decceleration * Time.fixedDeltaTime);

        if (velocity > climbSlideVelocityThreshold)
        {
            rb.linearVelocity = Vector3.zero;
            isClimbSliding = false;
        }
        else
            rb.linearVelocity = new Vector3(0, velocity, 0);
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