using UnityEngine;

[CreateAssetMenu(fileName = "AirMove", menuName = "ScriptableObjects/SceneObject/Movement/AirMove")]
public class AirMoveData : BaseMovementData
{
    [Header("Arial Acceleration")]
    public float AerialXAcceleration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               AerialXAcceleration > 0;
    }
}
