using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IAnimator
{
    public event Action<string, AnimationTrigger.Type> OnAnimationUpdateEvent;

    public void SetUp();

    public bool IsStatePossible(ActionState requestedState);

    public void PlayAnimation(AnimationStateData animationData);

    public void EndCurrentAnimation();

    public void SetFloatPerameter(string name, float value);
}
