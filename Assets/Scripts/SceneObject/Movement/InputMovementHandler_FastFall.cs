using UnityEngine;

public partial class InputMovementHandler
{
    private bool isFastFalling = false;
    public bool IsFastFalling => isFastFalling;


    #region Movement Conditions

    // States where an active fast fall stays latched. Leaving any of them drops it, so the
    // player has to press down again rather than inheriting a stale fast fall.
    private bool fastFallSustainAllowed => curMovementData.AerialMovementValid &&
                                           actionState.CurGroundedState == GroundedState.Airborn &&
                                           actionState.CurActionState != ActionState.HitStun &&
                                           !isLedgeClimbing &&
                                           !isDashing &&
                                           curMovementState != MovementState.WallSlide;

    // Shared requirements for any fast fall, cancelled or not.
    private bool fastFallAllowed => fastFallSustainAllowed &&
                                    !isJumpingSquating &&
                                    !IsInJumpSquat &&
                                    actionState.CurActionState <= ActionState.Attacking;

    // NOTE: Starting a fast fall normally requires the player to be falling. The on-hit cancel lifts that
    // requirement - that is the only thing the cancel adds over the plain trigger.    
    private bool fastFallStartAllowed => fastFallAllowed &&
                                         rb.linearVelocity.y < 0;

    #endregion


    private void UpdateFastFall()
    {
        if (isFastFalling)
        {
            if (!fastFallSustainAllowed)
                EndFastFall();

            return;
        }

        if (IsHitCancelPending)
            return;

        TryStartFastFall();
    }

    private void TryStartFastFall()
    {
        if (inputBuffer == null || !fastFallStartAllowed)
            return;

        if (!inputBuffer.TryConsume(BufferedInput.FastFall))
            return;

        BeginFastFall();
    }

    private void BeginFastFall()
    {
        isFastFalling = true;

        float fastFallVelocity = curMovementData.FastFallVelocity;

        if (rb.linearVelocity.y > -fastFallVelocity)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -fastFallVelocity, 0);
    }

    private void EndFastFall()
    {
        isFastFalling = false;
    }
}
