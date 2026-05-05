using UnityEngine;

[CreateAssetMenu(fileName = "WallJump", menuName = "ScriptableObjects/SceneObject/Movement/WallJump")]
public class WallJumpData : BaseMovementData
{
    public float MaxInitialVelocity;
    public float MinInitialVelocity;
    public float MaxJumpAcceleration;
    public float MinJumpAcceleration;
    [Tooltip("The angle of the jump direction." +
        "Assume wall normal is 0 degrees")]
    public float JumpAngle; 

    public override bool IsValid()
    {
        return Animation != null &&
               MaxInitialVelocity > 0 &&
               MinInitialVelocity > 0 &&
               MaxInitialVelocity >= MinInitialVelocity &&
               MaxJumpAcceleration > 0 &&
               MinJumpAcceleration > 0 &&
               MaxJumpAcceleration >= MinJumpAcceleration;
    }
}
