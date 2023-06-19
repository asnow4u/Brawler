using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackAnimationEventListener : MonoBehaviour
{
    public event Action<AnimationClip> AttackAnimationStarted;
    public event Action<AnimationClip> AttackAnimationEnded;
    public event Action<AnimationClip> AttackAnimationCollidersEnabled;
    public event Action<AnimationClip> AttackAnimationCollidersDisabled;

    public void OnAnimationAttackStarted(AnimationClip clip)
    {
        AttackAnimationStarted?.Invoke(clip);
    }

    public void OnAnimationAttackEnded(AnimationClip clip)
    {
        AttackAnimationEnded?.Invoke(clip);
    }

    public void OnAnimationAttackCollidersEnabled(AnimationClip clip)
    {
        AttackAnimationCollidersEnabled?.Invoke(clip);
    }

    public void OnAnimationAttackCollidersDisabled(AnimationClip clip)
    {
        AttackAnimationCollidersDisabled?.Invoke(clip);
    }
}
