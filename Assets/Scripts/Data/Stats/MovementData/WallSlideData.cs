using UnityEngine;

[CreateAssetMenu(fileName = "WallSlide", menuName = "ScriptableObjects/SceneObject/Movement/WallSlide")]
public class WallSlideData : BaseMovementData
{
    public float SlideVelocity;
    public float SlideDecceleration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               SlideVelocity > 0 &&
               SlideDecceleration > 0;
    }
}
