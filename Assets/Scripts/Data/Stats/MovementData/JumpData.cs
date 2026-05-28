using UnityEngine;

[CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/SceneObject/Movement/Jump")]
public class JumpData : BaseMovementData
{
    public float InitialVelocity;
    public float JumpAcceleration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               InitialVelocity > 0 &&
               JumpAcceleration > 0;
    }
}
