using UnityEngine;

[CreateAssetMenu(fileName = "WallLean", menuName = "ScriptableObjects/SceneObject/Movement/WallLean")]
public class WallLeanData : BaseMovementData
{
    public override bool IsValid()
    {
        return AnimationData.Animation != null;
    }
}
