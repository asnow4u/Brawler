using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAnimator
{
    public event Action<string> OnAnimationStateStartedEvent;
    public event Action<string> OnAnimationStateEndedEvent;
    public event Action<AnimationClip, string> OnAnimationTriggerEvent;

    public void SetUp(SceneObject obj);

    public void PlayAnimation(string animation);

    public void PlayIdleAnimation();

    public void SetFloatPerameter(string name, float value);
}
