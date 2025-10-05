
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/Movement/Move")]
    public class MoveInputData : MovementInputData
    {
        [Header("Grounded Movement")]
        public float GroundedXMaxAcceleration;
        public float GroundedXMinAcceleration;

        [Header("Attack Decleration")]
        [Tooltip("How quickly should the sceneObject slowdow when performing an attack while moving")]
        public float GroundedAttackXDecleration;
    }
}
