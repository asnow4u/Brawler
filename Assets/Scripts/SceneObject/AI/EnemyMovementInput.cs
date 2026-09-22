using UnityEngine;

/// <summary>
/// Movement input source for an AI driven enemy. Set by Enemy and read by InputMovementHandler.
/// An enemy without this component cannot move.
/// </summary>
internal class EnemyMovementInput : MonoBehaviour, IMovementInput
{
    private IInputBuffer inputBuffer;

    private Vector2 movement;
    private float jumpHeld;

    public float HorizontalInfluence => Mathf.Clamp(movement.x, -1f, 1f);
    public float VerticalInfluence => Mathf.Clamp(movement.y, -1f, 1f);
    public float JumpInfluence => Mathf.Clamp01(jumpHeld);

    //NOTE: Fast fall and short hop are not supported.


    #region Initialize

    private void Awake()
    {
        inputBuffer = GetComponent<IInputBuffer>();
        if (inputBuffer == null)
            Debug.LogError("EnemyMovementInput requires an IInputBuffer on the same object.", this);
    }

    #endregion


    #region Movement Inputs

    /// <summary>Sets horizontal and vertical influence.</summary>
    public void SetMovement(Vector2 movementInput)
    {
        movement = movementInput;
    }

    /// <summary>Sets horizontal influence only.</summary>
    public void SetHorizontal(float horizontal)
    {
        movement.x = horizontal;
    }

    /// <summary>Sets vertical influence only.</summary>
    public void SetVertical(float vertical)
    {
        movement.y = vertical;
    }

    public void ClearMovement()
    {
        movement = Vector2.zero;
    }

    #endregion


    #region Jump Input

    /// <summary>Buffers a jump press and holds jump until ReleaseJump is called.</summary>
    public void PressJump(float value = 1f)
    {
        jumpHeld = Mathf.Clamp01(value);
        inputBuffer?.Buffer(BufferedInput.Jump, jumpHeld);
    }

    public void ReleaseJump()
    {
        jumpHeld = 0f;
    }

    #endregion


    #region Dash Input

    public void PressDash()
    {
        inputBuffer?.Buffer(BufferedInput.Dash);
    }

    #endregion


    #region Reset

    /// <summary>Clears held influence and any buffered jump or dash.</summary>
    public void ClearAllInput()
    {
        movement = Vector2.zero;
        jumpHeld = 0f;

        inputBuffer?.Clear(BufferedInput.Jump);
        inputBuffer?.Clear(BufferedInput.Dash);
    }

    #endregion
}
