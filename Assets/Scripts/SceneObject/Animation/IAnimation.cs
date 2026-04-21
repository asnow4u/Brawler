using System;
using UnityEngine;

public enum AnimationEventState { Null, AttackStarted, EnableHitbox, DisableHitbox, AttackEnded }
public interface IAnimation
{
    public float GetCurrentAnimationDelta();
    public void PauseAnimation(float seconds);

    public event Action<AnimationEventState> AnimationEventFiredEvent;
}


public interface IAnimationEditor
{
    public bool DebugMode { get; }
    public AttackState DebugAttackState { get; }
    public bool IsDebugAnimationPlaying { get; }
    public float CurrentDebugAnimationTime { get; }
    public AnimationClip CurrentDebugAnimationClip { get; }

    public void SetDebugMode(bool value);
    public void SetDebugAttackState(AttackState state);
    public void PlayDebugAnimation();
    public void PauseDebugAnimation();
    public void JumpDebugAnimationToStart();
    public void JumpDebugAnimationToEnd();
    public void StepDebugAnimationForward();
    public void StepDebugAnimationBackward();



}
