using UnityEngine;

namespace Game.SceneObjects.Movement 
{ 
    [CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/Movement/Jump")]
    public class JumpInputData : MovementInputData
    {
        public float MinInitialVelocity;
        public float MaxInitialVelocity;
        public float MinJumpAcceleration;
        public float MaxJumpAcceleration;
    }
}
