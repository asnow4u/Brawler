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
    private const float jumpSquatDuration = 0.05f; // Changed from 2 frames to time-based duration
    private float lastJumpSquatTime = -100f;
    private bool isJumpingSquating => Time.time < lastJumpSquatTime + jumpSquatDuration;

    private int airJumpsPerformed = 0;


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
            
            float velocityMagnitude = curMovementData.InitialWallJumpVelocity;
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
            
        else if (curMovementState == MovementState.WallJump)
            acceleration = curMovementData.WallJumpAcceleration * jumpInfluence;

        if (acceleration > 0)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y + (acceleration * Time.fixedDeltaTime), 0);
    }
}
