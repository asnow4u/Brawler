using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/SceneObject/Movement/Move")]
public class MoveData : BaseMovementData
{
    [Header("Grounded Movement")]
    public float GroundedXMaxAcceleration;
    public float GroundedXMinAcceleration;

    public override bool IsValid()
    {
        return Animation != null &&
               GroundedXMaxAcceleration > 0 &&
               GroundedXMinAcceleration > 0 &&
               GroundedXMaxAcceleration > GroundedXMinAcceleration;
    }
}
