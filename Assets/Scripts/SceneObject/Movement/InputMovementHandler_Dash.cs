using UnityEngine;

public partial class InputMovementHandler
{
    [Header("Dash Initiation")]
    [Tooltip("Minimum stick magnitude to initiate a grounded dash. A tilt below this walks (normal acceleration); a tilt at or above it dashes. This is the walk/dash split.")]
    [Range(0f, 1f)]
    [SerializeField] private float dashInfluenceThreshold = 0.5f;
    [Tooltip("A qualifying tilt only initiates a dash when current speed in the press direction is below this fraction of MaxGroundedVelocity. Lets you dash from a standstill, out of a walk, or into a pivot - but not re-dash while already running at speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float dashInitiationSpeedScaler = 0.5f;

    [Header("Input Dash Charge")]
    [Tooltip("Minimum time between input dashes. Prevents mashing dash to hold a permanent speed boost.")]
    [SerializeField] private float dashCooldown = 0.4f;
    private bool hasDashed = false;
    private float lastDashTime = -100f;
    private bool isDashing = false;
    private Vector2 dashDirection = Vector2.zero;
    private float dashStartVelocity = 0;
    private float dashEndVelocity = 0;
    private float dashDuration = 0;
    private float dashDurationTotal = 0;
    
    private static readonly Vector2[] eightWayDirections =
    {
        new Vector2( 1f, 0f), // 0 - right
        new Vector2( 0.7f,  0.7f), //  45 - up right
        new Vector2( 0f, 1f), // 90 - up
        new Vector2(-0.7f,  0.7f), // 135 - up left
        new Vector2(-1f, 0f), // 180 - left
        new Vector2(-0.7f, -0.7f), // 225 - down left
        new Vector2( 0f, -1f), // 270 - down
        new Vector2( 0.7f, -0.7f), // 315 - down right
    };

    private bool dashMovementAllowed => curMovementData.DashValid &&
                                        isLedgeClimbing == false &&
                                        actionState.CurActionState <= ActionState.Moving;

    private bool inputDashAllowed => dashMovementAllowed &&
                                     Time.time >= lastDashTime + dashCooldown &&
                                     (actionState.CurGroundedState == GroundedState.Grounded || !hasDashed);

    private bool dashInitiationAllowed => dashMovementAllowed &&
                                          !IsInJumpSquat &&
                                          horizontalInfluence != 0 &&
                                          Mathf.Abs(horizontalInfluence) >= dashInfluenceThreshold &&
                                          rb.linearVelocity.x * Mathf.Sign(horizontalInfluence) < curMovementData.MaxGroundedVelocity * dashInitiationSpeedScaler;


    private void StartWaveLanding()
    {
        if (!dashMovementAllowed || horizontalInfluence == 0)
            return;

        Vector2 direction = new Vector2(Mathf.Sign(horizontalInfluence), 0f);
        float velocity = curMovementData.WaveLandDashVelocity;
        float duration = curMovementData.WaveLandDashDuration;

        BeginDash(direction, velocity, curMovementData.MaxGroundedVelocity, duration);
    }

    private void StartInitialRunDash()
    {
        if (!dashMovementAllowed || horizontalInfluence == 0)
            return;

        Vector2 direction = new Vector2(Mathf.Sign(horizontalInfluence), 0f);
        float velocity = curMovementData.InitialDashVelocity;
        float duration = curMovementData.InitialDashDuration;

        BeginDash(direction, velocity, curMovementData.MaxGroundedVelocity, duration);
    }

    private void StartInputDash()
    {
        if (!inputDashAllowed)
            return;

        Vector2 direction = ResolveDashDirection();
        float velocity = curMovementData.InputDashVelocity;
        float endVelocity = ResolveInputDashEndVelocity(direction);
        float duration = curMovementData.InputDashDuration;

        hasDashed = true;
        lastDashTime = Time.time;

        BeginDash(direction, velocity, endVelocity, duration);
    }

    private Vector2 ResolveDashDirection()
    {
        //Wall Slide - always dash up
        if (curMovementState == MovementState.WallSlide)
            return Vector2.up;

        float facingX = sceneObject.IsFacingRightDirection ? 1f : -1f;

        //Grounded - strip vertical, snap to a full horizontal dash
        if (actionState.CurGroundedState == GroundedState.Grounded)
            return new Vector2(horizontalInfluence == 0 ? facingX : Mathf.Sign(horizontalInfluence), 0f);

        //Airborne - snap to the nearest of 8 directions
        if (horizontalInfluence == 0 && verticalInfluence == 0)
            return new Vector2(facingX, 0f);

        float angle = Mathf.Atan2(verticalInfluence, horizontalInfluence) * Mathf.Rad2Deg;
        int index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % 8;
        return eightWayDirections[index];
    }

    private float ResolveInputDashEndVelocity(Vector2 direction)
    {
        return Mathf.Lerp(curMovementData.HorizontalEndDashVelocity,
                          curMovementData.VerticalEndDashVelocity,
                          Mathf.Abs(direction.y));
    }

    private void BeginDash(Vector2 direction, float velocity, float endVelocity, float duration)
    {
        dashDirection = direction;
        dashStartVelocity = velocity;
        dashEndVelocity = endVelocity;
        dashDuration = duration;
        dashDurationTotal = duration;

        isDashing = true;
        SetCurrentMoveState(MovementState.Dash);

        if (dashDirection.x > 0 && !sceneObject.IsFacingRightDirection ||
            dashDirection.x < 0 && sceneObject.IsFacingRightDirection)
        {
            sceneObject.TurnAround();
        }
    }

    private void UpdateDash()
    {
        if (dashDirection.x > 0 && IsAgainstRightWall() ||
            dashDirection.x < 0 && IsAgainstLeftWall())
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            EndDash();
            return;
        }

        dashDuration -= Time.fixedDeltaTime;
        if (dashDuration <= 0)
        {
            rb.linearVelocity = new Vector3(dashDirection.x * dashEndVelocity, dashDirection.y * dashEndVelocity, 0);
            EndDash();
            return;
        }
        
        float t = 1f - (dashDuration / dashDurationTotal);
        float decayT = Mathf.InverseLerp(curMovementData.DashSpeedHoldPercentage, 1f, t);
        float speed = Mathf.Lerp(dashStartVelocity, dashEndVelocity, decayT);
        
        rb.linearVelocity = new Vector3(dashDirection.x * speed, dashDirection.y * speed, 0);
    }

    private void RefreshDash()
    {
        hasDashed = false;
    }
    
    private void EndDash()
    {
        isDashing = false;
        dashDirection = Vector2.zero;
        dashStartVelocity = 0;
        dashEndVelocity = 0;
        dashDuration = 0;
        dashDurationTotal = 0;
    }
}
