using System;
using UnityEngine;

public enum AnimationEventState { Null, AttackStarted, EnableHitbox, DisableHitbox, AttackEnded }

[RequireComponent(typeof(Animator))]
internal class AnimationEventHandler : MonoBehaviour, IAnimationEvent
{
    public event Action<AnimationEventState> OnAnimationEventFiredEvent;

    public void StartAttack()
    {
        OnAnimationEventFiredEvent?.Invoke(AnimationEventState.AttackStarted);
    }

    public void EnableHitbox()
    {
        OnAnimationEventFiredEvent?.Invoke(AnimationEventState.EnableHitbox);
    }

    public void DisableHitbox()
    {
        OnAnimationEventFiredEvent?.Invoke(AnimationEventState.DisableHitbox);
    }

    public void EndAttack()
    {
        OnAnimationEventFiredEvent?.Invoke(AnimationEventState.AttackEnded);
    }
}
