using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "ClimbData", menuName = "ScriptableObjects/Movement/Climb")]
    public class ClimbData : MovementData
    {
        public float ClimbXVelocity;
        public float ClimbUpYVelocity;
        public float ClimbDownYVelocity;
    }
}
