using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/Movement/AirMove")]
    public class AirMoveData : MovementData
    {
        [Header("Velocity Limit")]
        public float AerialMaxXVelocity;
        public float AerialMaxYVelocity;

        [Header("Arial Acceleration")]
        public float AerialXAcceleration;
        public float AerialYAcceleration;

        [Header("Arial Decceleration")]
        public float AerialXDeceleration;
        public float AerialYDeceleration;
    }
}
