using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "AirJump", menuName = "ScriptableObjects/Movement/AirJump")]
    public class AirJumpInputData : JumpInputData
    {
        public int JumpsAvailable;
    }
}
