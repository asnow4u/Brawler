using UnityEngine;

[CreateAssetMenu(fileName = "AirMove", menuName = "ScriptableObjects/SceneObject/Movement/AirMove")]
public class AirMoveData : BaseMovementData
{
    [Header("Arial Acceleration")]
    public float AerialXMaxAcceleration;
    public float AerialXMinAcceleration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               AerialXMaxAcceleration > 0 &&
               AerialXMinAcceleration > 0 &&
               AerialXMaxAcceleration >= AerialXMinAcceleration;             
    }
}
