using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "SceneObjectBase", menuName = "ScriptableObjects/Base/SceneObject")]

    public class SceneObjectData : ScriptableObject
    {
        public float MaxMass;
        public float MinMass;

        public float GroundedMaxVelocityMax;
        public float GroundedMaxVelocityMin;

        public float AerialMaxXVelocityMax;
        public float AerialMaxXVelocityMin;
        public float AerialMaxYVelocityMax;
        public float AerialMaxYVelocityMin;

        public float GroundedDecceleration;
        public float AerialDecceleration;

        public float GravityScaler;
        
        public bool IsValid()
        {
            // SceneObject must have mass
            if (MaxMass <= 0 || MinMass <= 0 || MinMass > MaxMass)
                return false;

            // SceneObject must have be able to move (NOTE: This can be when getting hit)
            if (GroundedMaxVelocityMin <= 0 || GroundedMaxVelocityMax <= 0 || GroundedMaxVelocityMin > GroundedMaxVelocityMax )
                return false;

            // SceneObject must be able to move in the air (NOTE: This can be when getting hit)
            if (AerialMaxXVelocityMin <= 0 || AerialMaxXVelocityMax <= 0 || AerialMaxXVelocityMin > AerialMaxXVelocityMax ||
                AerialMaxYVelocityMin <= 0 || AerialMaxYVelocityMax <= 0 || AerialMaxYVelocityMin > AerialMaxYVelocityMax)
                return false;

            // SceneObject must be able to stop moving
            if (GroundedDecceleration <= 0 || AerialDecceleration <=0)
                return false;

            return true;
        }
    }


}
