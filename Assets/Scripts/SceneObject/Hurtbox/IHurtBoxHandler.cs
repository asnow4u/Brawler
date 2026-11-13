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
    public float RecoveryDuration { get; }
    public event Action<HitStunState> HitStunStateChangedEvent;
    public event Action<Vector3> LaunchRedirectedEvent;

    /// <summary>Faces against the new launch velocity and reports it.</summary>
    public void RedirectLaunch(Vector3 launchVelocity);

    /// <summary>Ends hitstun when it is in Recovery. Returns true when hitstun was ended.</summary>
    public bool TryEndRecovery();

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
    public bool DebugLockDamage { get; }
    public float DebugLockedDamage { get; }

    public void SetDebugMode(bool value);
    public void SetDebugBaseForce(float value);
    public void SetDebugInfluence(float value);
    public void SetDebugLaunchAngle(float value);
    public void SetDebugDamage(float value);
    public void SetDebugDelaySeconds(float value);
    public void SetDebugLockDamage(bool value);
    public void SetDebugLockedDamage(float value);

    public void ApplyDebugDamage();
}

public interface ISOHurtBoxHandlerEditor
{
    public float DebugStaggerDuration { get; }

    public void SetDebugStaggerDuration(float value);
    public void ApplyDebugStagger();
}
