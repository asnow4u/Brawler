using UnityEngine;

[CreateAssetMenu(fileName = "Climb", menuName = "ScriptableObjects/SceneObject/Movement/Climb")]
public class ClimbMoveData : BaseMovementData
{
    public AnimationClip IdleAnimation;

    public float ClimbXVelocity;
    public float ClimbUpYVelocity;
    public float ClimbDownYVelocity;

    public float MaxClimbSlideDecceleration;
    public float MinClimbSlideDecceleration;

    public override bool IsValid()
    {
        return Animation != null && 
               IdleAnimation != null &&
               ClimbXVelocity > 0 &&
               ClimbUpYVelocity > 0 &&
               ClimbDownYVelocity > 0 &&
               MaxClimbSlideDecceleration > 0 &&
               MinClimbSlideDecceleration > 0 &&
               MaxClimbSlideDecceleration > MinClimbSlideDecceleration;
    }
}
