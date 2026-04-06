using System;
using UnityEngine;

public interface IHurtBoxHandler
{
    public event Action<HitData> OnHitEvent;
}

public interface IHurtBoxHandlerEditor
{
    public bool DebugMode { get; }
    public float DebugInfluence { get; }
    public float DebugLaunchAngle { get; }
    public float DebugDamage { get; }

    public void SetDebugMode(bool value);
    public void SetDebugInfluence(float value);
    public void SetDebugLaunchAngle(float value);
    public void SetDebugDamage(float value);

    public void ApplyDebugDamage();
}
