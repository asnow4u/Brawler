using UnityEngine;

[CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/SceneObject/Movement/Jump")]
public class JumpData : BaseMovementData
{
    public float MaxInitialVelocity;
    public float MinInitialVelocity;
    public float MaxJumpAcceleration;
    public float MinJumpAcceleration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               MaxInitialVelocity > 0 &&
               MinInitialVelocity > 0 &&
               MaxInitialVelocity >= MinInitialVelocity &&
               MaxJumpAcceleration > 0 &&
               MinJumpAcceleration > 0 &&
               MaxJumpAcceleration >= MinJumpAcceleration;
    }
}
