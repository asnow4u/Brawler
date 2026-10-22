using System;
using UnityEngine;

public enum GuardState 
{ 
    Null = -1, 
    Raise = 0, 
    Hold = 1, 
    Blockstun = 2, 
    Drop = 3, 
    Counter = 4 
}

public interface IGuard
{
    public GuardState CurGuardState { get; }
    public float GuardWalkSpeed { get; }

    public AnimationClip GuardAnimation { get; }
    public float GuardAnimationSpeed { get; }
    public AnimationClip CounterAnimation { get; }
    public float CounterAnimationSpeed { get; }

    public event Action<GuardState> GuardStateChangedEvent;
}
