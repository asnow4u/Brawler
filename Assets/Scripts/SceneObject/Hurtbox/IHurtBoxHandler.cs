using System;
using UnityEngine;

public enum HitStunState 
{ 
    Null = -1, 
    Pause = 0, 
    Launch = 1, 
    Travel = 2, 
    Recovery = 3
}

public enum StaggerState
{
    Null = -1,
    Stagger = 0
}

public interface IHurtBoxHandler
{
    public Guid[] LastHitBy { get; }
    public event Action<KnockBackHitData> OnHitEvent;
}

public interface ISOHurtBoxHandler : IHurtBoxHandler
{    
    public HitStunState CurHitStunState { get; }   
    public event Action<HitStunState> HitStunStateChangedEvent;

    public StaggerState CurStaggerState { get; }
    public event Action<StaggerState> StaggerStateChangedEvent;

    /// <summary>Locks the scene object in stagger for the given duration. Ignored during hitstun.</summary>
    public void Stagger(float duration);
}

public interface IHurtBoxHandlerEditor
{
    public bool DebugMode { get; }
    public float DebugBaseForce { get; }
    public float DebugInfluence { get; }
    public float DebugLaunchAngle { get; }
    public float DebugDamage { get; }
    public float DebugDelaySeconds { get; }

    public void SetDebugMode(bool value);
    public void SetDebugBaseForce(float value);
    public void SetDebugInfluence(float value);
    public void SetDebugLaunchAngle(float value);
    public void SetDebugDamage(float value);
    public void SetDebugDelaySeconds(float value);

    public void ApplyDebugDamage();
}

public interface ISOHurtBoxHandlerEditor
{
    public float DebugStaggerDuration { get; }

    public void SetDebugStaggerDuration(float value);
    public void ApplyDebugStagger();
}
