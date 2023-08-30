using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IAnimator
{
    public event Action<string, AnimationTrigger.Type> OnAnimationUpdateEvent;

    public void SetUp(SceneObject obj);

    public void PlayAnimation(string animationState, AnimationTrigger[] animationTriggers = null);

    public void PlayIdleAnimation();

    public void SetFloatPerameter(string name, float value);
}
