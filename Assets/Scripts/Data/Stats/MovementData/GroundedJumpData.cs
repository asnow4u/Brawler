using UnityEngine;

[CreateAssetMenu(fileName = "GroundedJump", menuName = "ScriptableObjects/SceneObject/Movement/GroundedJump")]
public class GroundedJumpData : JumpData
{
    public float ShortHopVelocity;

    public override bool IsValid()
    {
        return base.IsValid() &&
               ShortHopVelocity > 0 &&
               ShortHopVelocity <= JumpVelocity;
    }
}
