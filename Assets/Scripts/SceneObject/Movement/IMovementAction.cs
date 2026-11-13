using System;
using UnityEngine;

public enum MovementState
{
    Null = -1,
    GroundMove = 0,
    AirMove = 1,
    LedgeClimb = 5,
    GroundJump = 6,
    AirJump = 7,
    WallJump = 8,
    WallRun = 9,
    Dash = 10
}

public interface IMovementAction
{
    public MovementState CurMovementState { get; }
    public bool IsInJumpSquat { get; }
    public event Action<MovementState> MovementStateChangedEvent;
}
