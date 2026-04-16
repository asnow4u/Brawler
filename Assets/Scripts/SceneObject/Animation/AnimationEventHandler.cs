using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationEventHandler : MonoBehaviour
{
    public event Action<AnimationEventState> OnEventFired;

    public void StartAttack()
    {
        OnEventFired?.Invoke(AnimationEventState.AttackStarted);
    }

    public void EnableHitbox()
    {
        OnEventFired?.Invoke(AnimationEventState.EnableHitbox);
    }

    public void DisableHitbox()
    {
        OnEventFired?.Invoke(AnimationEventState.DisableHitbox);
    }

    public void EndAttack()
    {
        OnEventFired?.Invoke(AnimationEventState.AttackEnded);
    }
}
