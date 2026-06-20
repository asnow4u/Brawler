using System;
using UnityEngine;

public enum MovementState
{
    Null = -1,
    GroundMove = 0,
    AirMove = 1,
    ClimbMove = 2,
    WallLean = 3,
    WallSlide = 4,
    LedgeClimb = 5,
    GroundJump = 6,
    AirJump = 7,
    WallJump = 8,
    ClimbJump = 9,
    Dash = 10
}

public interface IMovement
{
    public MovementState CurMovementState { get; }

    public event Action<MovementState> MovementStateChangedEvent;
}
