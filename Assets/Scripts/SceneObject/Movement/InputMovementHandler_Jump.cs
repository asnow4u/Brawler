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

    // Jump Animation
    private IAnimationEvent animationEventHandler;
    private bool isJumpAnimationActive = false;
    private AnimationEventState lastJumpAnimationEvent = AnimationEventState.Null;
    private bool isJumpSquatLocked => lastJumpAnimationEvent == AnimationEventState.JumpSquatStarted;
    
    // Jump Squat
    private bool isJumpSquatPending = false;
    private bool isLaunchPending = false;
    private bool hasJumped = false;
    public bool IsInJumpSquat => isJumpSquatLocked || isJumpSquatPending || (hasJumped && actionState.CurGroundedState == GroundedState.Grounded);
    private bool isJumpMovementStateLocked => isJumpSquatLocked || isJumpSquatPending;
    private bool isLaunchRising => hasJumped && rb.linearVelocity.y > 0;

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

    private static bool IsJumpState(MovementState moveState) => moveState == MovementState.GroundJump ||
                                                                moveState == MovementState.AirJump ||
                                                                moveState == MovementState.WallJump;

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
        EndFastFall();
        BeginJumpAnimation();

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
            return;
        }

        if (jumpYVelocity > 0)
        {
            rb.linearVelocity = new Vector3(jumpXVelocity, jumpYVelocity, 0);
            jumpInputAvailable = false;
        }
    }


    #region Jump Animation

    private void BeginJumpAnimation()
    {
        isJumpAnimationActive = true;
        lastJumpAnimationEvent = AnimationEventState.Null;
    }

    private void EndJumpAnimation()
    {
        isJumpAnimationActive = false;
        lastJumpAnimationEvent = AnimationEventState.Null;
    }

    /// <summary>Ends the jump animation and drops any squat or launch that has not happened yet.</summary>
    private void CancelJump()
    {
        EndJumpAnimation();
        isJumpSquatPending = false;
        isLaunchPending = false;
    }

    /// <summary>Records jump events from the active jump clip. JumpSquatEnded queues the grounded launch; JumpEnded ends the jump animation.</summary>
    private void OnJumpAnimationEvent(AnimationEventState state)
    {
        if (!isJumpAnimationActive || !IsJumpState(curMovementState))
            return;

        switch (state)
        {
            case AnimationEventState.JumpSquatStarted:
                lastJumpAnimationEvent = state;
                break;

            case AnimationEventState.JumpSquatEnded:
                lastJumpAnimationEvent = state;
                if (isJumpSquatPending)
                    isLaunchPending = true;
                break;

            case AnimationEventState.JumpEnded:
                EndJumpAnimation();
                break;
        }
    }

    #endregion


    #region Jump Squat

    private void BeginJumpSquat()
    {
        isJumpSquatPending = true;
        isLaunchPending = false;
        jumpInputAvailable = false;
        lastGroundedTime = -100f; // Consume coyote time to prevent double jumps
    }

    private void UpdateJumpSquat()
    {
        if (isJumpSquatPending && isLaunchPending)
            LaunchFromSquat();
    }

    private void LaunchFromSquat()
    {
        bool jumpHeld = jumpInfluence > 0;
        float launchVelocity = jumpHeld ? curMovementData.JumpVelocity : curMovementData.ShortHopVelocity;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, launchVelocity, 0);
        isJumpSquatPending = false;
        isLaunchPending = false;
        hasJumped = true;
    }

    #endregion
}
