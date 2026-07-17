using System;
using UnityEngine;

public interface IMovementInput
{
    public event Action<Vector2> MovementPerformedEvent;
    public event Action MovementStoppedEvent;
    public event Action<float> JumpPerformedEvent;
    public event Action JumpStoppedEvent;
    public event Action DashPerformedEvent;
}

public interface IAttackInput
{
    public event Action<Vector2> AttackPerformedEvent;
}

public interface IInteractionInput
{    
    public event Action InteractionPerformedEvent;
}


public interface IEquipmentInput 
{
    public event Action ToggleEquippedWeaponEvent;
}

#region Debug

public interface IMovementInputEditor
{
    public void DebugMovementInput(Vector2 movementInput);

    public void DebugJumpInput(float jumpInput);
}

public interface IAttackInputEditor
{    
    public void DebugAttackInput(Vector2 direction);
}

#endregion

