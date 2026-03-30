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
}
