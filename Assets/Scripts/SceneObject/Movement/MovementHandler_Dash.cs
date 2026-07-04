using UnityEngine;

public partial class MovementHandler
{
    private bool isDashing = false;
    private int dashDirection = 0;
    private float dashStartVelocity = 0;
    private float dashDuration = 0;
    private float dashDurationTotal = 0;

    [Header("Dash Initiation")]
    [Tooltip("Minimum stick magnitude to initiate a grounded dash. A tilt below this walks (normal acceleration); a tilt at or above it dashes. This is the walk/dash split.")]
    [Range(0f, 1f)]
    [SerializeField] private float dashInfluenceThreshold = 0.5f;
    [Tooltip("A qualifying tilt only initiates a dash when current speed in the press direction is below this fraction of MaxGroundedVelocity. Lets you dash from a standstill, out of a walk, or into a pivot - but not re-dash while already running at speed.")]
    [Range(0f, 1f)]
    [SerializeField] private float dashInitiationSpeedScaler = 0.5f;

    private bool dashInitiationAllowed =>
        dashMovementAllowed &&
        Mathf.Abs(horizontalInfluence) >= dashInfluenceThreshold &&
        rb.linearVelocity.x * Mathf.Sign(horizontalInfluence) < curMovementData.MaxGroundedVelocity * dashInitiationSpeedScaler;

    private void StartWaveLanding()
    {
        if (!dashMovementAllowed)
            return;

        int direction = (int)Mathf.Sign(horizontalInfluence);
        float velocity = curMovementData.MaxGroundedVelocity * curMovementData.WaveLandDashVelocityScaler;
        float duration = curMovementData.WaveLandDashDuration;

        BeginDash(direction, velocity, duration);
    }

    private void StartInitialRunDash()
    {
        if (!dashMovementAllowed)
            return;

        int direction = (int)Mathf.Sign(horizontalInfluence);
        float velocity = curMovementData.MaxGroundedVelocity * curMovementData.InitialDashVelocityScaler;
        float duration = curMovementData.InitialDashDuration;

        BeginDash(direction, velocity, duration);
    }

    private void StartHorizontalDash()
    {
        if (!dashMovementAllowed)
            return;

        //TODO: need to handle when 0, should dash the direction your facing

        int direction = (int)Mathf.Sign(horizontalInfluence);
        float velocity = curMovementData.HorizontalDashVelocity;
        float duration = curMovementData.HorizontalDashDuration;

        BeginDash(direction, velocity, duration);
    }

    private void BeginDash(int direction, float velocity, float duration)
    {
        dashDirection = direction;
        dashStartVelocity = velocity;
        dashDuration = duration;
        dashDurationTotal = duration;

        isDashing = true;
        SetCurrentMoveState(MovementState.Dash);

        // Face the dash direction
        if (dashDirection > 0 && !sceneObject.IsFacingRightDirection ||
            dashDirection < 0 && sceneObject.IsFacingRightDirection)
        {
            sceneObject.TurnAround();
        }
    }

    /// <summary>
    /// Drive the dash each FixedUpdate. Speed eases linearly from the initial burst down to
    /// MaxGroundedVelocity across the dash duration, so control hands back to normal grounded movement
    /// with no velocity discontinuity. Ends when the duration elapses.
    /// </summary>
    private void UpdateDash()
    {
        dashDuration -= Time.fixedDeltaTime;
        if (dashDuration <= 0)
        {
            EndDash();
            return;
        }

        float t = 1f - (dashDuration / dashDurationTotal);
        float speed = Mathf.Lerp(dashStartVelocity, curMovementData.MaxGroundedVelocity, t);
        rb.linearVelocity = new Vector3(dashDirection * speed, rb.linearVelocity.y, 0);
    }

    private void EndDash()
    {
        isDashing = false;
        dashDirection = 0;
        dashStartVelocity = 0;
        dashDuration = 0;
        dashDurationTotal = 0;
    }
}
