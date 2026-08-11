using System;
using UnityEngine;

public partial class InputMovementHandler
{
    private float hitCancelExecuteTime = -1f;
    private bool IsHitCancelPending => hitCancelExecuteTime > 0f;

    public event Action<BufferedInput> PerformedAttackCancel;

    private void UpdateBuffer()
    {
        UpdateOnHitCancelWindow();
        TryConsumeBufferedDash();
    }

    private void TryConsumeBufferedDash()
    {
        if (inputBuffer == null || !inputDashAllowed)
            return;

        if (!inputBuffer.TryConsume(BufferedInput.Dash))
            return;

        StartInputDash();
    }

    private void ConsumeBufferedJump()
    {
        inputBuffer?.TryConsume(BufferedInput.Jump);
    }


    #region On-Hit Cancel

    private void UpdateOnHitCancelWindow()
    {
        if (!IsHitCancelPending)
            return;

        //Still frozen - whatever is held stays held until the pause releases
        if (Time.time < hitCancelExecuteTime)
            return;

        TryExecuteCancel();
        hitCancelExecuteTime = -1f;
    }

    private void TryExecuteCancel()
    {
        if (inputBuffer == null)
            return;

        if (!inputBuffer.TryGetNewestLive(InputSets.AttackHitCancelInputs, out BufferedInput newest))
            return;

        if (newest == BufferedInput.Jump)
            TryJumpCancel();

        else if (newest == BufferedInput.Dash)
            TryDashCancel();
    }

    private bool TryJumpCancel()
    {
        bool grounded = actionState.CurGroundedState == GroundedState.Grounded;

        if (grounded)
        {
            if (!curMovementData.GroundedJumpValid)
                return false;
        }
        else
        {
            if (!curMovementData.AerialJumpValid || airJumpsPerformed >= curMovementData.AirJumpsAvailable)
                return false;
        }

        if (!inputBuffer.TryConsume(BufferedInput.Jump))
            return false;

        BeginCancel(BufferedInput.Jump);

        SetCurrentMoveState(grounded ? MovementState.GroundJump : MovementState.AirJump);
        StartJump();
        return true;
    }

    private bool TryDashCancel()
    {
        if (!curMovementData.DashValid || IsInJumpSquat)
            return false;

        if (!inputBuffer.TryConsume(BufferedInput.Dash))
            return false;

        BeginCancel(BufferedInput.Dash);
        StartDashCancel();
        return true;
    }

    private void BeginCancel(BufferedInput input)
    {
        ClearCancelWindow();
        PerformedAttackCancel?.Invoke(input);
    }

    private void ClearCancelWindow()
    {
        hitCancelExecuteTime = -1f;
    }

    #endregion
}
