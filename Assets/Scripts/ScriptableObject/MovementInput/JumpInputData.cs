using UnityEngine;

namespace Game.SceneObjects.Movement 
{ 
    [CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/Movement/Jump")]
    public class JumpInputData : MovementInputData
    {
        public float MinJumpVelocity;
        public float MaxJumpVelocity;
    }
}
