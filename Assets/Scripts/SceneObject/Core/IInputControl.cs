using System;
using UnityEngine;

public interface IMovementInput
{
    public float HorizontalInfluence { get; }
    public float VerticalInfluence { get; }
    public float JumpInfluence { get; }
}

public interface IAttackInput
{
}

public interface IInteractionInput
{
}

public interface IEquipmentInput 
{
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
