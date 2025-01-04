using UnityEngine;

namespace Game.SceneObjects.Movement 
{ 
    [CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/Movement/Jump")]
    public class JumpData : MovementData
    {
        public float JumpVelocity;
        public float GravityScaler;
    }
}
