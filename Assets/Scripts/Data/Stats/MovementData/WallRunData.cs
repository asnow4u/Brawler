using UnityEngine;

[CreateAssetMenu(fileName = "WallRun", menuName = "ScriptableObjects/SceneObject/Movement/WallRun")]
public class WallRunData : JumpData
{
    [Tooltip("Minimum speed toward the wall required to start a wall run.")]
    public float MinTriggerSpeed;

    [Tooltip("Launch velocity at MinTriggerSpeed. JumpVelocity is the launch velocity at max grounded speed.")]
    public float MinJumpVelocity;

    public override bool IsValid()
    {
        return base.IsValid() &&
               MinTriggerSpeed > 0 &&
               MinJumpVelocity > 0 &&
               MinJumpVelocity <= JumpVelocity;
    }
}
