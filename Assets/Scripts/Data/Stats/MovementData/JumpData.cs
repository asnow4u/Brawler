using UnityEngine;

public abstract class JumpData : BaseMovementData
{
    public float JumpVelocity;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               JumpVelocity > 0;
    }
}
