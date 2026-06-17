using UnityEngine;

[CreateAssetMenu(fileName = "GroundedJump", menuName = "ScriptableObjects/SceneObject/Movement/GroundedJump")]
public class GroundedJumpData : JumpData
{
    public float ShortHopVelocity;
    public float JumpSquatDuration; //4 frames = 0.066 @ 60fps

    public override bool IsValid()
    {
        return base.IsValid() &&
               ShortHopVelocity > 0 &&
               ShortHopVelocity <= JumpVelocity &&
               JumpSquatDuration > 0;
    }
}
