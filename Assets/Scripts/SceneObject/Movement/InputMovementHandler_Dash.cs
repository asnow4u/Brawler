using UnityEngine;

public partial class InputMovementHandler
{
    [Header("Dash Initiation")]
    [Tooltip("Minimum stick magnitude to initiate a grounded dash. This is for dashing while stopped.")]
    [Range(0f, 1f)]
    [SerializeField] private float dashInfluenceThreshold = 0.5f;
    [Tooltip("A qualifying tilt only initiates a dash when current speed in the press direction is below this fraction of MaxGroundedVelocity. Lets you dash from a standstill, out of a walk, or into a pivot - but not re-dash while already running at speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float dashInitiationSpeedScaler = 0.5f;

    [Header("Input Dash")]
    [Tooltip("Minimum time between input dashes.")]
    [SerializeField] private float dashCooldown = 0.5f;
    private bool hasDashed = false;
    private float lastDashTime = -100f;

    private bool isDashing = false;
    private Vector2 dashDirection = Vector2.zero;
    private float dashStartVelocity = 0;
    private float dashEndVelocity = 0;
    private float dashDuration = 0;
    private float dashDurationTotal = 0;    
    
    #region Movement Conditions

    private bool dashMovementAllowed => curMovementData.DashValid &&
                                        isLedgeClimbing == false &&
                                        actionState.CurActionState <= ActionState.Moving;

    private bool dashInitiationAllowed => dashMovementAllowed &&
                                          !IsInJumpSquat &&
                                          horizontalInfluence != 0 &&
                                          Mathf.Abs(horizontalInfluence) >= dashInfluenceThreshold &&
                                          rb.linearVelocity.x * Mathf.Sign(horizontalInfluence) < curMovementData.MaxGroundedVelocity * dashInitiationSpeedScaler;

    private bool inputDashAllowed => dashMovementAllowed &&
                                     !IsInJumpSquat &&
                                     Time.time >= lastDashTime + dashCooldown &&
                                     (actionState.CurGroundedState == GroundedState.Grounded || !hasDashed);

    #endregion


    private void StartInitialRunDash()
    {
        if (!dashMovementAllowed || horizontalInfluence == 0)
            return;

        Vector2 direction = new Vector2(Mathf.Sign(horizontalInfluence), 0f);
        float velocity = curMovementData.InitialDashVelocity;
        float duration = curMovementData.InitialDashDuration;

        BeginDash(direction, velocity, curMovementData.MaxGroundedVelocity, duration);
    }

    private void StartWaveLanding()
    {
        if (!dashMovementAllowed || horizontalInfluence == 0)
            return;

        Vector2 direction = new Vector2(Mathf.Sign(horizontalInfluence), 0f);
        float velocity = curMovementData.WaveLandDashVelocity;
        float duration = curMovementData.WaveLandDashDuration;

        BeginDash(direction, velocity, curMovementData.MaxGroundedVelocity, duration);
    }

    private void StartInputDash()
    {
        if (!inputDashAllowed)
            return;

        hasDashed = true;
        lastDashTime = Time.time;

        StartHorizontalDash();
    }

    private void StartDashCancel()
    {
        StartHorizontalDash();
    }

    private void StartHorizontalDash()
    {
        BeginDash(ResolveDashDirection(),
                  curMovementData.InputDashVelocity,
                  curMovementData.HorizontalEndDashVelocity,
                  curMovementData.InputDashDuration);
    }

    private Vector2 ResolveDashDirection()
    {
        if (horizontalInfluence != 0)
            return new Vector2(Mathf.Sign(horizontalInfluence), 0f);

        return new Vector2(sceneObject.IsFacingRightDirection ? 1f : -1f, 0f);
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

    private void RefreshDash()
    {
        hasDashed = false;
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
