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

public interface IHurtBoxHandler : IHitStunHandler
{
    public Guid[] LastHitBy { get; }
}

public interface IHitStunHandler
{
    public Vector3 EvaluateHitStunVelocity();

    public event Action<HitStunState> HitStunStateChangedEvent;
}

public interface IHurtBoxHandlerEditor
{
    public bool DebugMode { get; }
    public float DebugInfluence { get; }
    public float DebugLaunchAngle { get; }
    public float DebugDamage { get; }
    public float DebugDelaySeconds { get; }

    public void SetDebugMode(bool value);
    public void SetDebugInfluence(float value);
    public void SetDebugLaunchAngle(float value);
    public void SetDebugDamage(float value);
    public void SetDebugDelaySeconds(float value);

    public void ApplyDebugDamage();
}
