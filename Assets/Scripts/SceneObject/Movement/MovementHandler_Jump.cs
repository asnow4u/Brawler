using System.Collections;
using System.ComponentModel;
using UnityEngine;

internal partial class MovementHandler
{    
    [Header("Coyote Time")]
    private const float coyoteTimeDuration = 0.1f;
    private float lastGroundedTime = -100f;

    [Header("Wall Jump")]
    private const float wallJumpDetachDuration = 0.2f;
    private float lastWallJumpTime = -100f;

    private bool jumpInputAvailable = true; //Jump available is only true after the user has released the jump button
    
    [Header("Jump Squat")]
    private bool isJumpSquatPending = false;
    private float lastJumpSquatTime = -100f;
    private bool isJumpingSquating => Time.time < lastJumpSquatTime + curMovementData.JumpSquatDuration;

    private int airJumpsPerformed = 0;


    private void StartJump()
    {
        if (curMovementState == MovementState.GroundJump)
        {
            BeginJumpSquat();
            return;
        }

        float jumpVelocity = 0;

        if (curMovementState == MovementState.AirJump)
        {
            airJumpsPerformed++;
            jumpVelocity = curMovementData.AirJumpVelocity;
        }

        else if (curMovementState == MovementState.WallJump)
        {
            lastWallJumpTime = Time.time;

            float angle = curMovementData.WallJumpAngle;
            float angleRad = angle * Mathf.Deg2Rad;

            float dirX = Mathf.Cos(angleRad);
            float dirY = Mathf.Sin(angleRad);

            if (IsAgainstRightWall())
            {
                dirX = -dirX;
            }

            float velocityMagnitude = curMovementData.WallJumpVelocity;
            rb.linearVelocity = new Vector3(dirX * velocityMagnitude, dirY * velocityMagnitude, 0);

            if (dirX > 0 && !sceneObject.IsFacingRightDirection || dirX < 0 && sceneObject.IsFacingRightDirection)
            {
                sceneObject.TurnAround();
            }

            jumpInputAvailable = false;
            lastJumpSquatTime = Time.time;
            return;
        }

        if (jumpVelocity > 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);
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
    }
    
}
