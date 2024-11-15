using UnityEngine;

[CreateAssetMenu(fileName = "WallJump", menuName = "ScriptableObject/Movement/WallJump")]
public class WallJumpData : MovementData
{
    public float JumpVelocity;
    public float JumpAngle;
}
