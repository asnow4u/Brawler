using System;
using UnityEngine;

public interface IAnimation
{
    public int GetFrameOfCurrentAnimation();

    public event Action<AnimationClip> AnimationStartedEvent;
    public event Action<AnimationClip> AnimationEndedEvent;
}
