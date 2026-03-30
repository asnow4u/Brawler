using System;
using UnityEngine;

public interface IMovementInput
{
    /// <summary>
    /// Horizontal movement performed
    /// </summary>
    public event Action<Vector2> MovementPerformedEvent;

    /// <summary>
    /// Horizontal movement stopped
    /// </summary>
    public event Action MovementStoppedEvent;

    /// <summary>
    /// Vertical jump performed
    /// </summary>
    public event Action<float> JumpPerformedEvent;

    /// <summary>
    /// Vertical jump stopped
    /// </summary>
    public event Action JumpStoppedEvent;

    /// <returns>
    /// Whether inputs for horizontal movement is active
    /// </returns>
    public bool IsHorizontalMovementActive();

    /// <returns>
    /// Whether inputs for vertical jumps are active
    /// </returns>
    public bool IsVerticalJumpActive();
}


public interface IAttackInput
{
    /// <summary>
    /// Perform an attack in the upward direction
    /// </summary>
    public event Action UpAttackPerformedEvent;

    /// <summary>
    /// Perform an attack in the downward direction
    /// </summary>
    public event Action DownAttackPerformedEvent;

    /// <summary>
    /// Perform an attack in the left direction
    /// </summary>
    public event Action LeftAttackPerformedEvent;

    /// <summary>
    /// Perform an attack in the right direction
    /// </summary>
    public event Action RightAttackPerformedEvent;
}

public interface IInteractionInput
{    
    public event Action InteractionPerformedEvent;
}


public interface IEquipmentInput 
{
    public event Action ToggleEquippedWeaponEvent;
}

