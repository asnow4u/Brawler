using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HurtBoxHandler))]
internal class MovementHandler : MonoBehaviour, IMovement
{
    //Dependencies
    private ISceneObject sceneObject;
    private IMovementInput movementInput;
    private IActionState actionState;
    private IStats statHandler;
    private IHitStunHandler hitStunHandler;

    //Components
    private Rigidbody rb;

    //Movement State Data
    private MovementState curMovementState;
    public MovementState CurMovementState => curMovementState;
    
    private MovementStatData curMovementData = null;

    [Header("Influence")]
    [Range(-1, 1)]
    [SerializeField] private float horizontalInfluence;

    [Range(-1, 1)]
    [SerializeField] private float verticalInfluence;

    [Range(0, 1)]
    [SerializeField] private float jumpInfluence;    

    //Jump Properties
    [Header("Coyote Time")]
    private const float coyoteTimeDuration = 0.1f;
    private float lastGroundedTime = -100f;

    private bool jumpInputAvailable = true; //Jump available is only true after the user has released the jump button
    private bool isJumpingSquating = false;
    private const int JUMPSQUATFRAMECOUNT = 2; //How many frames does it take for a jump before user is actionable
    private Coroutine jumpSquatCoroutine;
    private int airJumpsPerformed = 0;

    //Climb Properties
    [SerializeField] private bool isClimbSliding = false;
    private const float climbSlideVelocityThreshold = -10f; //When switching to climbing, this determines whether a slide decceleration is applied

    //Edge Vault Properties        
    private const float requiredPercentageAboveVaultEdge = 0.3f;
    [SerializeField] private bool isVaulting = false;
    private float vaultStoredVelocity;

    //Bounce Properties
    [Header("Bounce")]
    [SerializeField] private float bounceDegrade = 0.9f;
    private const float minGroundBounceVelocity = 20f;

    //Conditions
    private bool groundedMovementAllowed => curMovementData.GroundedMovementValid &&
                                            horizontalInfluence != 0 &&
                                            actionState.CurActionState <= ActionState.Moving;
    private bool aerialMovementAllowed => curMovementData.AerialMovementValid &&
                                          (horizontalInfluence != 0 || verticalInfluence != 0) &&
                                          actionState.CurActionState <= ActionState.Moving;
    private bool climbMovementAllowed => curMovementData.ClimbMovementValid &&
                                         (horizontalInfluence != 0 || verticalInfluence != 0) &&
                                         actionState.CurActionState <= ActionState.Moving;
    private bool jumpMovementAllowed => curMovementData.GroundedJumpValid &&
                                        jumpInfluence > 0 &&
                                        jumpInputAvailable &&
                                        actionState.CurActionState <= ActionState.Moving;
    private bool aerialJumpMovementAllowed => curMovementData.AerialJumpValid &&
                                              jumpInfluence > 0 &&
                                              jumpInputAvailable &&
                                              airJumpsPerformed < curMovementData.AirJumpsAvailable &&
                                              actionState.CurActionState <= ActionState.Moving;
    private bool wallLeanAllowed => curMovementData.WallLeanValid &&
                                    IsRunningAgainstWall() &&
                                    actionState.CurActionState <= ActionState.Moving;
    private bool vaultMovementAllowed => curMovementData.VaultValid &&
                                         actionState.CurActionState <= ActionState.Moving;

    public event Action<MovementState> MovementStateChangedEvent;

    #region Getters

    public float HorizontalInfluence => horizontalInfluence;
    public float VerticalInfluence => verticalInfluence;

    #endregion


    #region Initialize / Destroy

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("MovementHandler requires a component that implements ISceneObject");

        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        hitStunHandler = GetComponent<IHitStunHandler>();

        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0;

        //Not a requirement (Null means no inputs are ever recieved
        movementInput = GetComponent<IMovementInput>();        

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        actionState.GroundedStateChangedEvent += OnGroundedStateChanged;
        actionState.ClimbStateChangedEvent += OnClimbStateChanged;

        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;
        
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
        actionState.GroundedStateChangedEvent -= OnGroundedStateChanged;
        actionState.ClimbStateChangedEvent -= OnClimbStateChanged;

        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;

        if (movementInput != null)
        {
            movementInput.MovementPerformedEvent -= SetMovementInfluence;
            movementInput.MovementStoppedEvent -= ResetMovementInfluence;
            movementInput.JumpPerformedEvent -= SetJumpInfluence;
            movementInput.JumpStoppedEvent -= ResetJumpInfluence;
        }
    }

    /// <summary>
    /// Handle Ground state changed event
    /// </summary>
    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        if (groundedState == GroundedState.Grounded)
            airJumpsPerformed = 0;
        else if (groundedState == GroundedState.Airborn)
        {
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

    private void FixedUpdate()
    {
        if (actionState.CurActionState == ActionState.HitStun)
        {
            UpdateHitStunMovement();
            return;
        }

        //CheckForClimbingStateChange();
        //if (actionState.CurClimbState == ClimbState.Climbing)
        //    UpdateClimbMovement();

        else if (actionState.CurGroundedState == GroundedState.Grounded)
            UpdateGroundedMovement();

        else if (actionState.CurGroundedState == GroundedState.Airborn)
            UpdateAerialMovement();
    }


    #region State    

    /// <summary>
    /// Set the current movement state to <paramref name="inputData"/> if possible
    /// </summary>
    private void SetCurrentMoveState(MovementState moveState)
    {        
        if (moveState == curMovementState)
            return;

        if (moveState == MovementState.Null || actionState.TryChangeState(ActionState.Moving))
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
        if (isJumpingSquating || isVaulting) 
            return;

        //Jump
        if (jumpMovementAllowed)
        {
            SetCurrentMoveState(MovementState.GroundJump);
            StartJump();
        }

        //Horizontal Movement
        else if (groundedMovementAllowed)
        {
            if (wallLeanAllowed)
                SetCurrentMoveState(MovementState.WallLean);
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
        if (isJumpingSquating || isVaulting)
            return;

        //Coyote Time Jump
        if (jumpMovementAllowed && Time.time <= lastGroundedTime + coyoteTimeDuration)
        {
            SetCurrentMoveState(MovementState.GroundJump);
            StartJump();
        }

        //Air Jump
        else if (aerialJumpMovementAllowed)
        {
            SetCurrentMoveState(MovementState.AirJump);
            StartJump();
        }
                       
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


    #region Wall Check

    /// <summary>
    /// Determine if the sceneObject is against a wall
    /// </summary>
    private bool IsRunningAgainstWall()
    {
        return IsAgainstRightWall() || IsAgainstLeftWall();
    }

    /// <summary>
    /// Determine if the sceneObject is against a wall on the right side
    /// </summary>
    private bool IsAgainstRightWall()
    {
        if (sceneObject.IsFacingRightDirection)
        {
            if (horizontalInfluence >= 0 && sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Environment"), out _))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determine if the sceneObject is against a wall on the left side
    /// </summary>
    private bool IsAgainstLeftWall()
    {
        if (!sceneObject.IsFacingRightDirection)
        {
            if (horizontalInfluence <= 0 && sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Environment"), out _))
                return true;
        }

        return false;
    }

    #endregion


    #region Movement Influence
    
    private void SetMovementInfluence(Vector2 inputInfluence)
    {
        horizontalInfluence = Mathf.Clamp(inputInfluence.x, -1, 1);
        verticalInfluence = Mathf.Clamp(inputInfluence.y, -1, 1);
    }

    private void ResetMovementInfluence()
    {
        horizontalInfluence = 0;
        verticalInfluence = 0;
    }

    public void SetJumpInfluence(float inputInfluence)
    {
        if (!jumpInputAvailable && inputInfluence == 0)
            jumpInputAvailable = true;

        jumpInfluence = Mathf.Clamp(inputInfluence, 0, 1);
    }

    private void ResetJumpInfluence()
    {
        if (!jumpInputAvailable)
            jumpInputAvailable = true;

        jumpInfluence = 0;
    }

    #endregion


    #region Collision

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ClimbableEdge edge))
        {
            UpdateEdgeClimb(edge);
        }
    }

    #endregion


    #region Grounded Movement

    /// <summary>
    /// Update movement on the ground based on current movement state
    /// </summary>
    private void UpdateGroundedMovement()
    {            
        UpdateGroundedMovementState();

        switch (curMovementState)
        {
            case MovementState.Null:
                DeccelerateGroundedMovement();
                break;

            case MovementState.WallLean:
                DeccelerateGroundedMovement();
                break;

            case MovementState.GroundMove:
                UpdateGroundedAcceleration();
                break;

            case MovementState.GroundJump:
                UpdateGroundedAcceleration();
                UpdateJumpVelocity();
                break;
        }
    }

    /// <summary>
    /// Accelerate on the ground by an acceleration value to a max velocity from <see cref="currentMovementCollection"/>
    /// </summary>
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

    /// <summary>
    /// Deccelerate grounded movement based on base deccelration value and the sceneObject's mass.
    /// </summary>
    private void DeccelerateGroundedMovement()
    {
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

    /// <summary>
    /// Update horizontal movement in the air
    /// </summary>
    private void UpdateAerialMovement()
    {
        UpdateAerialMovementState();

        switch (curMovementState)
        {
            case MovementState.Null:
                DeccelerateAerialXMovement();
                DeccelerateAerialYMovement();
                break;

            case MovementState.AirMove:

                //Horizontal Movement
                if (horizontalInfluence != 0)
                    AerialXAccelerate();
                else
                    DeccelerateAerialXMovement();

                //Vertical Movement
                if (verticalInfluence != 0)
                    AerialYAccelerate();
                else
                    DeccelerateAerialYMovement();
                break;

            case MovementState.GroundJump:

                //Horizontal Movement
                if (horizontalInfluence != 0)
                    AerialXAccelerate();
                else
                    DeccelerateAerialXMovement();

                UpdateJumpVelocity();
                break;
        }
    }

    /// <summary>
    /// Accelerate in the air on the X axis
    /// </summary>
    private void AerialXAccelerate()
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

    /// <summary>
    /// Accelerate in the air on the Y axis
    /// </summary>
    private void AerialYAccelerate()
    {
        float maxYVelocity = curMovementData.MaxAerialYVelocity;
        float acceleration = curMovementData.AerialYAcceleration;

        if (verticalInfluence < 0 && rb.linearVelocity.y < 0)
        {
            float acceleratedYValue = rb.linearVelocity.y - (acceleration * Time.fixedDeltaTime);

            if (acceleratedYValue < -maxYVelocity)
                acceleratedYValue = -maxYVelocity;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratedYValue, 0);
        }

        else
            DeccelerateAerialYMovement();
    }

    /// <summary>
    /// Deccelerate in the air on the X axis 
    /// </summary>
    private void DeccelerateAerialXMovement()
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

    /// <summary>
    /// Deccelerate in the air on the Y axis 
    /// </summary>
    private void DeccelerateAerialYMovement()
    {
        if (Mathf.Abs(rb.linearVelocity.y) > curMovementData.MaxAerialYVelocity)
        {
            //Positive Decceleration
            if (rb.linearVelocity.y > 0)
            {
                float decceleratedYValue = rb.linearVelocity.y - curMovementData.AerialUpYDecceleration * Time.fixedDeltaTime;

                if (decceleratedYValue < 0)
                    decceleratedYValue = 0;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
            }

            //Negative Decceleration
            else if (rb.linearVelocity.y < 0)
            {
                float decceleratedYValue = rb.linearVelocity.y + curMovementData.AerialDownYDecceleration * Time.fixedDeltaTime;

                if (decceleratedYValue > 0)
                    decceleratedYValue = 0;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
            }
        }

        //Gravity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y + (Physics.gravity.y * curMovementData.GravityMultiplier * Time.fixedDeltaTime), 0);
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


    #region Jump

    private void StartJump()
    {
        float jumpVelocity = 0;

        if (curMovementState == MovementState.GroundJump)
        {
            jumpVelocity = curMovementData.InitialJumpVelocity;
            lastGroundedTime = -100f; // Consume coyote time to prevent double jumps
        }

        else if (curMovementState == MovementState.AirJump)
        {
            //CheckTurnAround();
            airJumpsPerformed++;
            jumpVelocity = curMovementData.InitialAirJumpVelocity;
        }

        if (jumpVelocity > 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);
            jumpInputAvailable = false;
            jumpSquatCoroutine = StartCoroutine(JumpFrameCounter());
        }
    }


    private IEnumerator JumpFrameCounter()
    {
        int frameCount = 0;
        
        isJumpingSquating = true;

        while (frameCount < JUMPSQUATFRAMECOUNT)
        {
            frameCount++;
            yield return null;
        }

        isJumpingSquating = false;
    }

    /// <summary>
    /// Velocity applied to rb based on how long the jump button has been held
    /// </summary>
    private void UpdateJumpVelocity()
    {
        float acceleration = 0;

        if (curMovementState == MovementState.GroundJump)
            acceleration = curMovementData.JumpAcceleration * jumpInfluence;

        else if (curMovementState == MovementState.AirJump)
            acceleration = curMovementData.AirJumpAcceleration * jumpInfluence;

        if (acceleration > 0)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y + (acceleration * Time.fixedDeltaTime), 0);
    }

    #endregion


    #region Edge Climb

    public void UpdateEdgeClimb(ClimbableEdge edge)
    {
        if (curMovementState == MovementState.Vault || !vaultMovementAllowed) return;

        BoxCollider edgeCollider = edge.transform.GetComponent<BoxCollider>();

        //Scene Object must be above the edge to vault
        if (sceneObject.Bounds.max.y <= edgeCollider.bounds.max.y) return;

        //Calculate how much of the other collider is above this edge
        float edgeHeightDifference = sceneObject.Bounds.max.y - edgeCollider.bounds.max.y;
        float percentageAboveEdge = edgeHeightDifference / sceneObject.Bounds.size.y;            

        //Determine if scene object is heading towards the edge
        bool correctInfluence = (edge.IsRight && HorizontalInfluence < 0 && edge.transform.position.x < transform.position.x) ||
                                (!edge.IsRight && HorizontalInfluence > 0 && edge.transform.position.x > transform.position.x);            

        if (percentageAboveEdge >= requiredPercentageAboveVaultEdge && correctInfluence)
        {
            //Turn to face edge if not already facing
            if ((edge.IsRight && sceneObject.IsFacingRightDirection) ||
                (!edge.IsRight && !sceneObject.IsFacingRightDirection))
            {
                sceneObject.TurnAround();
            }

            vaultStoredVelocity = rb.linearVelocity.x;
            rb.linearVelocity = Vector3.zero;

            transform.position = edge.transform.position;
                
            SetCurrentMoveState(MovementState.Vault);
            isVaulting = true;
        }                
    }

    #endregion


    #region HitStun Movement

    private void UpdateHitStunMovement()
    {
        rb.linearVelocity = hitStunHandler.EvaluateHitStunVelocity();
        CheckForHitStunBounce();
    }

    private void CheckForHitStunBounce()
    {
        Bounds bounds = sceneObject.Bounds;
        Vector3 direction = rb.linearVelocity.normalized;
        float distance = rb.linearVelocity.magnitude * Time.fixedDeltaTime;

        //Get bound points for bounce check
        List<Vector3> boundPoints = new List<Vector3>();
        float centralZ = (bounds.max.z + bounds.min.z) / 2;

        //X Direction
        if (direction.x > 0)
        {
            boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ)); // Top-right
            boundPoints.Add(new Vector3(bounds.max.x, bounds.center.y, centralZ));  // Right-center
            boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ)); // Bottom-right
        }
        else if (direction.x < 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ)); // Top-left
            boundPoints.Add(new Vector3(bounds.min.x, bounds.center.y, centralZ));  // Left-center
            boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ)); // Bottom-left
        }

        //Y Direction
        if (direction.y > 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ)); // Top-left
            boundPoints.Add(new Vector3(bounds.center.x, bounds.max.y, centralZ)); // Top-center
            boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ)); // Top-right
        }
        else if (direction.y < 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ)); // Bottom-left
            boundPoints.Add(new Vector3(bounds.center.x, bounds.min.y, centralZ)); // Bottom-center
            boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ)); // Bottom-right
        }

        if (boundPoints.Count == 0)
            return;

        //Environment check
        List<Vector3> hitNormals = new List<Vector3>();
        foreach (var point in boundPoints)
        { 
            if (Physics.Raycast(point, direction, out RaycastHit hit, distance, LayerMask.GetMask("Environment")))
                hitNormals.Add(hit.normal);
        }

        if (hitNormals.Count == 0)
            return;

        Vector3 bounceVelocity = CalculateBounceVelocity(hitNormals);
        rb.linearVelocity = bounceVelocity;
    }

    private Vector3 CalculateBounceVelocity(List<Vector3> hitNormals)
    {
        if (hitNormals == null || hitNormals.Count == 0)
            return Vector3.zero;

        //Calculate bounce velocity
        Vector3 averageNormal = Vector3.zero;

        foreach (var normal in hitNormals)
            averageNormal += normal;
        averageNormal /= hitNormals.Count;

        Vector3 bounceVelocity = Vector3.Reflect(rb.linearVelocity, averageNormal) * bounceDegrade;

        //Prevent small bounces on ground
        if (bounceVelocity.magnitude < minGroundBounceVelocity &&
            rb.linearVelocity.y < 0 && bounceVelocity.y > 0)
        {
            return Vector3.zero;
        }

        return bounceVelocity;
    }

    #endregion
}