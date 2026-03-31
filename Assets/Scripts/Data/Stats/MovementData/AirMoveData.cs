using UnityEngine;

[CreateAssetMenu(fileName = "AirMove", menuName = "ScriptableObjects/SceneObject/Movement/AirMove")]
public class AirMoveData : BaseMovementData
{
    [Header("Arial Acceleration")]
    public float AerialXMaxAcceleration;
    public float AerialXMinAcceleration;

    public float AerialYMaxAcceleration;
    public float AerialYMinAcceleration;

    public override bool IsValid()
    {
        return Animation != null &&
               AerialXMaxAcceleration > 0 &&
               AerialXMinAcceleration > 0 &&
               AerialXMaxAcceleration > AerialXMinAcceleration &&
               AerialYMaxAcceleration > 0 &&
               AerialYMinAcceleration > 0 &&
               AerialYMaxAcceleration > AerialYMinAcceleration;

    }
}
