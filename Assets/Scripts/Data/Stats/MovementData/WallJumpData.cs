using UnityEngine;

[CreateAssetMenu(fileName = "WallJump", menuName = "ScriptableObjects/SceneObject/Movement/WallJump")]
public class WallJumpData : JumpData
{    
    [Tooltip("The angle of the jump direction." +
        "Assume wall normal is 0 degrees")]
    public float JumpAngle; 
}
