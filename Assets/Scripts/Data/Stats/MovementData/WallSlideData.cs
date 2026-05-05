using UnityEngine;

[CreateAssetMenu(fileName = "WallSlide", menuName = "ScriptableObjects/SceneObject/Movement/WallSlide")]
public class WallSlideData : BaseMovementData
{
    public float MaxSlideVelocity;
    public float MinSlideVelocity;
    public float MaxSlideDecceleration;
    public float MinSlideDecceleration;

    public override bool IsValid()
    {
        return Animation != null &&
               MaxSlideVelocity > 0 &&
               MinSlideVelocity > 0 &&
               MaxSlideVelocity >= MinSlideVelocity &&
               MaxSlideDecceleration > 0 &&
               MinSlideDecceleration > 0 &&
               MaxSlideDecceleration >= MinSlideDecceleration;
    }
}
