using UnityEngine;

[CreateAssetMenu(fileName = "WallJump", menuName = "ScriptableObjects/Movement/WallJump")]
public class WallJumpData : MovementData
{
    public float JumpVelocity;
    public float JumpAngle;
}
