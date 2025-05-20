using UnityEngine;

public interface IInputControl
{
    #region Movement

    /// <summary>
    /// Perform horizontal movement based on the <paramref name="movement"/>
    /// </summary>
    public void PerformMovement(Vector2 movement);

    /// <summary>
    /// Stop horizontal movement
    /// </summary>
    public void StopHorizontalMovement();

    /// <summary>
    /// Perform vertical jump with the given <paramref name="jumpStrength"/>
    /// </summary>
    public void PerformVerticalJump(float jumpStrength);

    /// <summary>
    /// Stop vertical jump
    /// </summary>
    public void StopJumpMovement();

    /// <returns>
    /// Whether inputs for horizontal movement is active
    /// </returns>
    public bool IsHorizontalMovementActive();

    /// <returns>
    /// Whether inputs for vertical jumps are active
    /// </returns>
    public bool IsVerticalJumpActive();

    #endregion


    #region Attack

    /// <summary>
    /// Perform an attack in the upward direction
    /// </summary>
    public void PerformUpAttack();

    /// <summary>
    /// Perform an attack in the downward direction
    /// </summary>
    public void PerformDownAttack();

    /// <summary>
    /// Perform an attack in the left direction
    /// </summary>
    public void PerformLeftAttack();

    /// <summary>
    /// Perform an attack in the right direction
    /// </summary>
    public void PerformRightAttack();

    /// <returns>
    /// Whether inputs for upward attack is active
    /// </returns>
    public bool IsUpAttackActive();

    /// <returns>
    /// Whether inputs for downward attack is active
    /// </returns>
    public bool IsDownAttackActive();

    /// <returns>
    /// Whether inputs for left attack is active
    /// </returns>
    public bool IsLeftAttackActive();

    /// <returns>
    /// Whether inputs for right attack is active
    /// </returns>
    public bool IsRightAttackActive();

    #endregion


    #region Interaction

    public void PerformInteraction();

    /// <returns>
    /// Check if interaction inputs are active
    ///</returns>
    public bool IsInteractionActive();

    #endregion
}
