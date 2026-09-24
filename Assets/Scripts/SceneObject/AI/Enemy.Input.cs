using UnityEngine;

/// <summary>
/// Input surface of the enemy. Handlers on the prefab read these interfaces the same way they read
/// the player's; which handlers are present decides what the enemy can actually do.
/// </summary>
internal abstract partial class Enemy : IInputBuffer, IMovementInput, IAttackInput, IEquipmentInput
{
    [Header("Input Buffer")]
    [Tooltip("How long a press stays live in the buffer, in seconds.")]
    [SerializeField] private float inputBufferWindow = 0.2f;

    private InputBuffer inputBuffer;

    private Vector2 movement;
    private float jumpInfluence;

    //NOTE: Fast fall and short hop are not supported.


    private void InitializeInput()
    {
        inputBuffer = new InputBuffer(inputBufferWindow);
    }


    #region Movement Input

    public float HorizontalInfluence => Mathf.Clamp(movement.x, -1f, 1f);
    public float VerticalInfluence => Mathf.Clamp(movement.y, -1f, 1f);
    public float JumpInfluence => Mathf.Clamp01(jumpInfluence);

    protected bool IsJumpHeld => jumpInfluence > 0f;

    protected void SetMovement(Vector2 value)
    {
        movement = value;
    }

    /// <summary>Buffers a jump press and holds jump until ReleaseJump is called.</summary>
    protected void PressJump()
    {
        jumpInfluence = 1f;
        inputBuffer.Buffer(BufferedInput.Jump, jumpInfluence);
    }

    protected void ReleaseJump()
    {
        jumpInfluence = 0f;
    }

    #endregion


    #region Attack and Equipment Input

    /// <summary>Buffers an attack in a direction. The direction picks the slot.</summary>
    protected void PressAttack(Vector2 direction)
    {
        inputBuffer.Buffer(BufferedInput.Attack, direction.magnitude, direction);
    }

    /// <summary>Buffers a swap between the primary and secondary weapon.</summary>
    protected void PressSwapWeapon()
    {
        inputBuffer.Buffer(BufferedInput.SwapWeapon);
    }

    #endregion


    #region Input Buffer

    public float BufferWindow => inputBuffer.BufferWindow;
    public void Buffer(BufferedInput input, float value = 0f, Vector2 direction = default) => inputBuffer.Buffer(input, value, direction);
    public bool Peek(BufferedInput input) => inputBuffer.Peek(input);
    public bool TryConsume(BufferedInput input) => inputBuffer.TryConsume(input);
    public bool TryConsume(BufferedInput input, out InputRecord record) => inputBuffer.TryConsume(input, out record);
    public float LastPressTime(BufferedInput input) => inputBuffer.LastPressTime(input);
    public bool TryGetNewestLive(BufferedInput[] inputs, out BufferedInput newest) => inputBuffer.TryGetNewestLive(inputs, out newest);
    public void Clear(BufferedInput input) => inputBuffer.Clear(input);
    public void ClearAll() => inputBuffer.ClearAll();

    #endregion
}
