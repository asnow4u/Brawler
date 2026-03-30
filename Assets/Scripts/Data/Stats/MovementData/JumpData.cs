using UnityEngine;

[CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/SceneObject/Movement/Jump")]
public class JumpData : BaseMovementData
{
    public float MinInitialVelocity;
    public float MaxInitialVelocity;
    public float MinJumpAcceleration;
    public float MaxJumpAcceleration;
}
