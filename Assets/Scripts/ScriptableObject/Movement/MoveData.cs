
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/Movement/Move")]
    public class MoveData : MovementData
    {
        [Header("Velocity Limit")]
        public float GroundedMaxXVelocity;

        [Header("Grounded Movement")]
        public float GroundedXAcceleration;
        public float GroundedXDeceleration;

        [Tooltip("How quickly should the sceneObject slowdow when performing an attack while moving")]
        public float GroundedAttackXDecleration;
    }
}
