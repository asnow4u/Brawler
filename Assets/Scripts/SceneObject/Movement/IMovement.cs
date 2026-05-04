using System;
using UnityEngine;

public enum MovementState
{
    Null = -1,
    GroundMove = 0,
    AirMove = 1,
    ClimbMove = 2,
    WallLean = 3,
    Vault = 4,
    GroundJump = 5,
    AirJump = 6,
    ClimbJump = 7
}

public interface IMovement
{
    public MovementState CurMovementState { get; }

    public event Action<MovementState> MovementStateChangedEvent;
}
