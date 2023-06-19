using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAnimator
{
    public void SetUp(SceneObject obj);

    public void PlayAnimation(string animation);

    public void PlayIdleAnimation();

    public void SetFloatPerameter(string name, float value);
}
