using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "AirJump", menuName = "ScriptableObjects/Movement/AirJump")]
    public class AirJumpData : MovementData
    {
        public int JumpsAvailable;
        public float AirJumpVelocity;
    }
}
