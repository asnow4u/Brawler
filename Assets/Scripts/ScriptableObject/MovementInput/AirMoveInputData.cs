using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/Movement/AirMove")]
    public class AirMoveInputData : MovementInputData
    {
        [Header("Arial Acceleration")]
        public float AerialXAcceleration;
        public float AerialYAcceleration;
    }
}
