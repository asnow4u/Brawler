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

        public float GetInitialVelocity(float ratio)
        {
            return Mathf.Lerp(MaxInitialVelocity, MinInitialVelocity, Mathf.Clamp01(ratio));
        }

        public float GetJumpAcceleration(float ratio)
        {
            return Mathf.Lerp(MaxJumpAcceleration, MinJumpAcceleration, Mathf.Clamp01(ratio));
        } 
    }
}
