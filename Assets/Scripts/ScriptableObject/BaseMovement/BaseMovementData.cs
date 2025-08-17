using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "BaseMovement", menuName = "ScriptableObjects/Movement/BaseMovement")]

    public class BaseMovementData : ScriptableObject
    {
        public float GroundedMaxVelocity;
        public float GroundedDecceleration;

        public float AerialMaxXVelocity;
        public float AerialMaxYVelocity;
        public float AerialDecceleration;

        public float GravityScaler;
    }
}
