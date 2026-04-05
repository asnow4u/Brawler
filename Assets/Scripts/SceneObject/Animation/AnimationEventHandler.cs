using System;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AnimationEventHandler : MonoBehaviour
{
    public event Action<AnimationEventState> OnEventFired;

    public void EnableHitbox()
    {
        OnEventFired?.Invoke(AnimationEventState.EnableHitbox);
    }

    public void DisableHitbox()
    {
        OnEventFired?.Invoke(AnimationEventState.DisableHitbox);
    }
}
