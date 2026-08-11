using UnityEngine;

public class HitSenderData
{
    public float hitPauseTime;

    public HitSenderData(float hitPauseTime = 0f)
    {
        this.hitPauseTime = hitPauseTime;
    }
}

public class AttackHitSenderData : HitSenderData
{
    public int AttackState;
    public float LaunchAngle;

    public bool Cancelable;

    public AttackHitSenderData(float hitPauseTime, int attackState, float launchAngle, bool cancelable = true) : base(hitPauseTime)
    {
        AttackState = attackState;
        LaunchAngle = launchAngle;
        Cancelable = cancelable;
    }
}
