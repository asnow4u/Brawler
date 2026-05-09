using System;
using UnityEngine;

public interface IAnimationEvent
{
    public event Action<AnimationEventState> OnAnimationEventFiredEvent;
}
