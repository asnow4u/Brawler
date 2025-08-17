using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "Climb", menuName = "ScriptableObjects/Movement/Climb")]
    public class ClimbInputData : MovementInputData
    {
        public float ClimbXVelocity;
        public float ClimbUpYVelocity;
        public float ClimbDownYVelocity;
    }
}
