using UnityEngine;

[CreateAssetMenu(fileName = "AirMove", menuName = "ScriptableObjects/SceneObject/Movement/AirMove")]
public class AirMoveData : BaseMovementData
{
    [Header("Arial Acceleration")]
    public float AerialXMaxAcceleration;
    public float AerialXMinAcceleration;

    public float AerialYMaxAcceleration;
    public float AerialYMinAcceleration;
}
