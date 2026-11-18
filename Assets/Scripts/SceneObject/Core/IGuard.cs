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
    public float AirGravityScale { get; }
    public float AirMaxFallSpeed { get; }
    public float AirSlowdownRate { get; }

    public AnimationClip GroundGuardAnimation { get; }
    public float GroundGuardAnimationSpeed { get; }
    public AnimationClip AirGuardAnimation { get; }
    public float AirGuardAnimationSpeed { get; }

    /// <summary>Seconds the guard has been up. 0 while the guard is down.</summary>
    public float GuardTimer { get; }
    /// <summary>Seconds the guard stays up while held.</summary>
    public float GuardDuration { get; }

    /// <summary>True while the counter blast hitbox is active.</summary>
    public bool IsCounterBlastActive { get; }
    /// <summary>Counter blast expansion from 0 to 1.</summary>
    public float CounterBlastProgress { get; }
    /// <summary>World-space radius of the counter blast hitbox.</summary>
    public float CounterBlastRadius { get; }
    /// <summary>World-space center of the counter blast hitbox.</summary>
    public Vector3 CounterBlastCenter { get; }

    public event Action<GuardState> GuardStateChangedEvent;
}
