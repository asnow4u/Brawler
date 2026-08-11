using System.Collections;
using System.ComponentModel;
using UnityEngine;

public partial class InputMovementHandler
{    
    [Header("Coyote Time")]
    private const float coyoteTimeDuration = 0.1f;
    private float lastGroundedTime = -100f;

    [Header("Wall Jump")]
    private const float wallJumpDetachDuration = 0.2f;
    private float lastWallJumpTime = -100f;
    
    // Jump Squat
    private bool isJumpSquatPending = false;
    private bool hasJumped = false;
    public bool IsInJumpSquat => isJumpSquatPending || (hasJumped && actionState.CurGroundedState == GroundedState.Grounded);
    private float lastJumpSquatTime = -100f;
    private bool isJumpingSquating => Time.time < lastJumpSquatTime + curMovementData.JumpSquatDuration;

    // NOTE: Prevents multiple Jumps from single held jump input
    private bool jumpInputAvailable = true;  
    private int airJumpsPerformed = 0;


    #region Movement Conditions

    private bool jumpRequested => jumpInfluence > 0 ||
                                  (inputBuffer != null && inputBuffer.Peek(BufferedInput.Jump));

    private bool groundedJumpMovementAllowed => curMovementData.GroundedJumpValid &&
                                                jumpRequested &&
                                                jumpInputAvailable &&
                                                actionState.CurActionState <= ActionState.Moving;

    private bool coyoteJumpMovementAllowed => groundedJumpMovementAllowed &&
                                              Time.time <= lastGroundedTime + coyoteTimeDuration;

    private bool aerialJumpMovementAllowed => curMovementData.AerialJumpValid &&
                                              jumpRequested &&
                                              jumpInputAvailable &&
                                              airJumpsPerformed < curMovementData.AirJumpsAvailable &&
                                              actionState.CurActionState <= ActionState.Moving;

    private bool wallJumpAllowed => curMovementData.WallJumpValid &&
                                    jumpRequested &&
                                    jumpInputAvailable &&
                                    IsAgainstWall() &&
                                    actionState.CurActionState <= ActionState.Moving &&
                                    Time.time >= lastWallJumpTime + wallJumpDetachDuration;

    #endregion


    private void UpdateJump()
    {
        if (!jumpInputAvailable && jumpInfluence == 0)
            jumpInputAvailable = true;

        UpdateJumpSquat();
    }

    private void StartJump()
    {
        ConsumeBufferedJump();

        if (curMovementState == MovementState.GroundJump)
        {
            BeginJumpSquat();
            return;
        }

        float jumpXVelocity = rb.linearVelocity.x;
        float jumpYVelocity = 0;

        if (curMovementState == MovementState.AirJump)
        {
            airJumpsPerformed++;
            jumpXVelocity = horizontalInfluence * curMovementData.MaxAerialXVelocity;
            jumpYVelocity = curMovementData.AirJumpVelocity;

            if (horizontalInfluence > 0 && !sceneObject.IsFacingRightDirection || horizontalInfluence < 0 && sceneObject.IsFacingRightDirection)
                sceneObject.TurnAround();
        }

        else if (curMovementState == MovementState.WallJump)
        {
            lastWallJumpTime = Time.time;

            float angle = curMovementData.WallJumpAngle;
            float angleRad = angle * Mathf.Deg2Rad;

            float dirX = Mathf.Cos(angleRad);
            float dirY = Mathf.Sin(angleRad);

            if (IsAgainstRightWall())
                dirX = -dirX;

            float velocityMagnitude = curMovementData.WallJumpVelocity;
            rb.linearVelocity = new Vector3(dirX * velocityMagnitude, dirY * velocityMagnitude, 0);

            if (dirX > 0 && !sceneObject.IsFacingRightDirection || dirX < 0 && sceneObject.IsFacingRightDirection)
                sceneObject.TurnAround();

            jumpInputAvailable = false;
            lastJumpSquatTime = Time.time;
            return;
        }

        if (jumpYVelocity > 0)
        {
            rb.linearVelocity = new Vector3(jumpXVelocity, jumpYVelocity, 0);
            jumpInputAvailable = false;
            lastJumpSquatTime = Time.time;
        }
    }

    private void BeginJumpSquat()
    {
        lastJumpSquatTime = Time.time;
        isJumpSquatPending = true;
        jumpInputAvailable = false;
        lastGroundedTime = -100f; // Consume coyote time to prevent double jumps
    }

    private void UpdateJumpSquat()
    {
        if (!isJumpSquatPending)
            return;

        if (Time.time >= lastJumpSquatTime + curMovementData.JumpSquatDuration)
            LaunchFromSquat();
    }

    private void LaunchFromSquat()
    {
        bool jumpHeld = jumpInfluence > 0;
        float launchVelocity = jumpHeld ? curMovementData.JumpVelocity : curMovementData.ShortHopVelocity;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, launchVelocity, 0);
        isJumpSquatPending = false;
        hasJumped = true;
    }
}
