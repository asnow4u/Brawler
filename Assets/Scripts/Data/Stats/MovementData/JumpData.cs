using UnityEngine;

public abstract class JumpData : BaseMovementData
{
    public float JumpVelocity;

    private const string JumpSquatEndEvent = "EndJumpSquat";

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               JumpVelocity > 0;
    }

    /// <summary>Whether the jump animation has an EndJumpSquat event.</summary>
    public bool HasJumpSquatEnd()
    {
        return TryGetAnimationEventTime(JumpSquatEndEvent, out _);
    }

    /// <summary>Time from the start of the jump animation to its EndJumpSquat event, divided by the animation speed. 0 when the event is missing.</summary>
    public float GetJumpSquatDuration()
    {
        if (!TryGetAnimationEventTime(JumpSquatEndEvent, out float eventTime))
            return 0f;

        float speed = AnimationData.AnimationSpeed;
        return speed > 0f ? eventTime / speed : eventTime;
    }

    private bool TryGetAnimationEventTime(string functionName, out float time)
    {
        time = 0f;

        if (AnimationData == null || AnimationData.Animation == null)
            return false;

        foreach (AnimationEvent animationEvent in AnimationData.Animation.events)
        {
            if (animationEvent.functionName == functionName)
            {
                time = animationEvent.time;
                return true;
            }
        }

        return false;
    }
}
