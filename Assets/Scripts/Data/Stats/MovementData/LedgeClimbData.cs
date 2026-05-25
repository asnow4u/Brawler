using UnityEngine;

[CreateAssetMenu(fileName = "LedgeClimb", menuName = "ScriptableObjects/SceneObject/Movement/LedgeClimb")]
public class LedgeClimbData : BaseMovementData
{
    public override bool IsValid()
    {
        return AnimationData.Animation != null;
    }
}

