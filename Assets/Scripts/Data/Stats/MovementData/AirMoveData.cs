using UnityEngine;

[CreateAssetMenu(fileName = "AirMove", menuName = "ScriptableObjects/SceneObject/Movement/AirMove")]
public class AirMoveData : BaseMovementData
{
    [Header("Arial Acceleration")]
    public float AerialXAcceleration;

    [Header("Fast Fall")]
    [Tooltip("Descent speed a fast fall snaps to and holds. Must be set above the SceneObject's AerialMaxFallVelocity - at or below it, fast falling would do nothing or slow the object down.")]
    public float FastFallVelocity;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
               AerialXAcceleration > 0 &&
               FastFallVelocity > 0;
    }
}
