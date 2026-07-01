using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/SceneObject/Movement/Move")]
public class MoveData : BaseMovementData
{
    [Header("Grounded Movement")]
    public float GroundedXAcceleration;
    public float GroundedAttackDecceleration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               GroundedXAcceleration > 0;
    }
}
